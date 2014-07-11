using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Servya
{
	public class Router : IHttpProcessor
	{
		private class Entry : IComparable<Entry>
		{
			public Route Route { get; set; }
			public IDictionary<string, RouteAction> VerbLookup { get; set; }

			public int CompareTo(Entry other)
			{
				return Count(this).CompareTo(Count(other));
			}

			private static int Count(Entry entry)
			{
				return entry.Route.Components.TakeWhile(c => c.Type == RouteComponentType.Literal).Count();
			}

			public override string ToString()
			{
				return string.Format("{0}: {1}", Route.Raw, string.Join(", ", VerbLookup.Keys));
			}
		}

		private readonly List<Entry> m_lookup;
		private readonly IDictionary<HttpStatusCode, RouteAction> m_errorHandlers;
		private readonly QueueProcessor m_accessQueue;
		private readonly CategoryLogger m_logger;
		private readonly FileLogger m_accessLog;

		public Router()
		{
			m_logger = new CategoryLogger(this);
			m_lookup = new List<Entry>();
			m_errorHandlers = new Dictionary<HttpStatusCode, RouteAction>();
			m_accessQueue = new QueueProcessor("Access log", priority: ThreadPriority.Lowest);
			m_accessLog = new FileLogger(string.Format("{0}_access", Assembly.GetEntryAssembly().GetName().Name));
		}

		public void ConfigureAccessLog(TimeSpan interval, FileSize maxSize)
		{
			m_accessLog.Init(interval, maxSize);
		}

		public void Dispose()
		{
			m_accessLog.Close();
		}

		public bool AddRoute(HttpVerb verb, string path, RouteAction action)
		{
			return AddRoute(verb.ToString().ToUpperInvariant(), path, action);
		}

		public bool AddRoute(string verb, string path, RouteAction action)
		{
			var entry = m_lookup.FirstOrDefault(e => e.Route.Raw == path);

			if (entry == null)
			{
				entry = new Entry { Route = new Route(path), VerbLookup = new Dictionary<string, RouteAction>() };
				m_lookup.Add(entry);
			}

			if (entry.VerbLookup.ContainsKey(verb))
			{
				m_logger.Error("Failed to add route at {0} ({1}), already exists", path, verb);
				return false;
			}

			entry.VerbLookup.Add(verb, action);

			// Ensure literals are prioritised
			m_lookup.Sort();

			m_logger.Info("Added route at {0} ({1})", path, verb);
			return true;
		}

		public bool SetHandler(HttpStatusCode code, RouteAction handler)
		{
			if (m_errorHandlers.ContainsKey(code))
			{
				m_logger.Error("Failed to add handler for {0}, already exists", code);
				return false;
			}

			m_errorHandlers.Add(code, handler);
			m_logger.Info("Added handler for {0}", code);
			return true;
		}

		public async Task Process(IHttpContext context, bool busy)
		{
			var timer = Stopwatch.StartNew();
			var request = context.Request;
			var response = context.Response;

			m_logger.Info("{0} request from {1} for {2}", request.RawVerb, request.Client, request.Url);

			using (response)
			{
				if (!busy)
				{
					try
					{
						await ProcessImpl(context);
					}
					catch (Exception ex)
					{
						m_logger.Error(ex);
					}
				}
				else
				{
					response.Status = HttpStatusCode.ServiceUnavailable;
					var data = request.Encoding.GetBytes("Service unavailable");
					await response.Stream.WriteAsync(data, 0, data.Length);
				}

				m_logger.Info("{0} request from {1} for {2} finished with status {3} in {4}ms", request.RawVerb, request.Client,
					request.Url, response.Status, timer.ElapsedMilliseconds);

				AccessLog(timer.ElapsedMilliseconds, request, response);
			}
		}

		private async Task ProcessImpl(IHttpContext context)
		{
			var request = context.Request;
			var response = context.Response;
			var localPath = request.Url.LocalPath;

			IDictionary<string, string> args = null;
			RouteAction routeAction;

			var urlComponents = Route.GetComponents(localPath);
			var entry = m_lookup.Find(e => e.Route.Validate(urlComponents, out args));

			if (entry == null)
			{
				m_logger.Error("Couldn't find route for {0}", localPath);
				response.Status = HttpStatusCode.NotFound;
			}
			else if (!entry.VerbLookup.TryGetValue(request.RawVerb, out routeAction))
			{
				m_logger.Error("Method {0} is not valid for {1}", request.RawVerb, localPath);
				response.Status = HttpStatusCode.MethodNotAllowed;
			}
			else
			{
				await routeAction(context, args);
			}

			var status = response.Status;

			if (status != HttpStatusCode.OK)
			{
				RouteAction handler;
				if (m_errorHandlers.TryGetValue(status, out handler))
				{
					await handler(context, args);
				}
			}
		}

		private void AccessLog(long elapsedMs, IHttpRequest request, IHttpResponse response)
		{
			var date = DateTimeOffset.Now;
			var offset = date.Offset;
			var sign = offset < TimeSpan.Zero ? "-" : "+";

			var dateString = string.Format("{0} {1}{2:00}{3:00}", date.ToString("dd/MMM/yyyy:HH:mm:ss"), sign, offset.Hours, offset.Minutes);
			var logString = string.Format("{0} - - [{1}] \"{2} {3}\" {4} {5}", request.Client.Address, dateString,
					request.RawVerb, request.Url.PathAndQuery, (int)response.Status, elapsedMs);

			m_accessQueue.Add(() =>
			{
				m_accessLog.Write(LogLevel.Info, logString);
			});
		}
	}
}
