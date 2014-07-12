using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Servya.SystemNetImpl;

namespace Servya
{
	public class AsyncHttpListener
	{
		private readonly HttpListener m_listener;
		private readonly IHttpProcessor m_processor;
		private readonly bool m_keepAlive;
		private readonly ManualResetEventSlim m_resetEvent;
		private readonly CategoryLogger m_logger;

		public AsyncHttpListener(IHttpProcessor processor, int port = 80, int securePort = 433, bool keepAlive = true, IPAddress bindTo = null)
		{
			m_listener = new HttpListener { IgnoreWriteExceptions = true };
			m_processor = processor;
			m_keepAlive = keepAlive;
			m_resetEvent = new ManualResetEventSlim(true);
			m_logger = new CategoryLogger(this);

			var bind = bindTo != null && bindTo != IPAddress.Any ? bindTo.ToString() : "*";

			if (port != 0)
				m_listener.Prefixes.Add(string.Format("http://{0}:{1}/", bind, port));

			if (securePort != 0)
			{
				CertAssert(securePort);
				m_listener.Prefixes.Add(string.Format("https://{0}:{1}/", bind, securePort));
			}
		}

		public void Start(Func<SynchronizationContext> syncContext, int concurrentAccepts, int maxDelayMS = 0)
		{
			m_logger.Info("Listening on {0} with {1} concurrent accepts", m_listener.Prefixes.First(), concurrentAccepts);
			m_listener.Start();

			for (var i = 0; i < concurrentAccepts; i++)
			{
				var context = syncContext();
				var state = new EventLoopState(maxDelayMS);
				Task.Run(() => MainLoop(context, state));

				if (maxDelayMS > 0 && context is EventLoopContext)
					Task.Run(() => CalcDelay(context, state));
			}
		}

		public void Stop()
		{
			Pause();
			m_listener.Close();
			m_processor.Dispose();
		}

		public void Pause()
		{
			m_resetEvent.Reset();
		}

		public void Restart()
		{
			m_resetEvent.Set();
		}

		private void CertAssert(int port)
		{
			var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var path = Path.Combine(appdata, ".mono", "httplistener");
			var cer = port + ".cer";
			var pvk = port + ".pvk";

			if (!File.Exists(Path.Combine(path, cer)) || !File.Exists(Path.Combine(path, pvk)))
				throw new FileNotFoundException(string.Format("Expected {0} and {1} at {2} for HTTPS", cer, pvk, path));
		}

		private async Task MainLoop(SynchronizationContext syncContext, EventLoopState state)
		{
			while (true)
			{
				try
				{
					HttpListenerContext context;
					try
					{
						context = await m_listener.GetContextAsync();
					}
					catch (ObjectDisposedException)
					{
						return;
					}
					catch (HttpListenerException ex)
					{
						// IO error due to our request, means we're probably exiting
						if (ex.NativeErrorCode == 995)
							continue;

						throw;
					}

					m_resetEvent.Wait();

					syncContext.Post(async unused =>
					{
						var wrapper = new Context(context);
						wrapper.Response.KeepAlive = m_keepAlive && wrapper.Request.KeepAlive;

						await m_processor.Process(wrapper, state.Delay > state.MaxDelay);
					}, null);
				}
				catch (Exception ex)
				{
					m_logger.Error("Error in accept loop: " + ex);
				}
			}
		}

		private void CalcDelay(SynchronizationContext context, EventLoopState state)
		{
			context.Post(async unused =>
			{
				var timer = Stopwatch.StartNew();

				while (true)
				{
					// Yield back to the event loop
					await Task.Yield();

					// Find out how long it took for the event loop to handle our request
					var elapsed = (int)timer.ElapsedMilliseconds;

					state.Delay = elapsed;

					await Task.Delay(Math.Max(0, 500 - elapsed));
					timer.Restart();
				}
			}, null);
		}

		private class EventLoopState
		{
			public int MaxDelay { get; private set; }

			// Don't need thread safety, just observability via volatile
			private volatile int m_delay;
			public int Delay { get { return m_delay; } set { m_delay = value; } }

			public EventLoopState(int maxDelayMS)
			{
				MaxDelay = Math.Max(0, maxDelayMS);
			}
		}
	}
}
