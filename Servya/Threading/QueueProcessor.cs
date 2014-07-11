using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Servya
{
	public class QueueProcessor
	{
		private readonly BlockingCollection<Action> m_queue;
		private readonly Thread m_thread;
		private readonly CategoryLogger m_logger;

		public QueueProcessor(string name, Action preLoopAction = null, ThreadPriority priority = ThreadPriority.Normal)
		{
			m_logger = new CategoryLogger(this);

			m_queue = new BlockingCollection<Action>();
			m_thread = new Thread(() =>
			{
				if (preLoopAction != null)
					preLoopAction();

				while (true)
				{
					Action entry;
					try
					{
						entry = m_queue.Take();
					}
					// Thread should stop once we're out of stuff to process
					catch (InvalidOperationException)
					{
						return;
					}

					try
					{
						entry();
					}
					catch (Exception ex)
					{
						try
						{
							m_logger.Error("Error in processing queue '{0}': {1}", name, ex);
						}
						catch
						{
							Debug.Fail("Pathological case: top-level handler failed");
						}
					}
				}
			}) { IsBackground = true, Name = name, Priority = priority };

			m_thread.Start();
		}

		public void Stop()
		{
			m_queue.CompleteAdding();
		}

		public void Add(Action entry)
		{
			m_queue.Add(entry);
		}
	}
}
