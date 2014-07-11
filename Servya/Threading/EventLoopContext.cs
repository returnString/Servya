using System;
using System.Threading;

namespace Servya
{
	public class EventLoopContext : SynchronizationContext
	{
		private readonly QueueProcessor m_processor;
		private static int s_id;

		public EventLoopContext()
		{
			var id = Interlocked.Increment(ref s_id);

			m_processor = new QueueProcessor("Event " + id,
				() => SynchronizationContext.SetSynchronizationContext(this));
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			m_processor.Add(() => d(state));
		}
	}
}

