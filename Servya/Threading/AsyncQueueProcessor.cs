using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Servya
{
	public class AsyncQueueProcessor
	{
		private class State
		{
			public QueueProcessor Processor;
			public long Current;
		}

		private State[] m_states;
		private readonly string m_name;
		private readonly ReaderWriterLockSlim m_lock;

		public AsyncQueueProcessor(string name)
		{
			m_name = name;
			m_lock = new ReaderWriterLockSlim();
		}

		public void Configure(int newSize)
		{
			if (newSize == 0)
				throw new ArgumentOutOfRangeException("number", "Can't create an AsyncQueueProcessor with zero threads");

			var currentSize = m_states != null ? m_states.Length : 0;

			if (newSize == currentSize)
				return;

			using (new WriteLock(m_lock))
			{
				// Stop any processors outside of the new range
				for (var i = newSize; i < currentSize; i++)
				{
					m_states[i].Processor.Stop();
				}

				Array.Resize(ref m_states, newSize);

				// Spin up processors to meet the new capacity, if higher
				for (var i = currentSize; i < newSize; i++)
				{
					var queue = new QueueProcessor(string.Format("{0} {1}", m_name, i));
					m_states[i] = new State { Processor = queue };
				}
			}
		}

		public Task Execute(Action action)
		{
			return Execute<object>(() => { action(); return null; });
		}

		public Task<T> Execute<T>(Func<T> func)
		{
			var source = new TaskCompletionSource<T>();

			State state;
			using (new ReadLock(m_lock))
				state = m_states.Aggregate((m, n) => m.Current < n.Current ? m : n);

			Interlocked.Increment(ref state.Current);

			state.Processor.Add(() =>
			{
				T result;

				try
				{
					result = func();
				}
				catch (Exception ex)
				{
					source.SetException(ex);
					return;
				}

				source.SetResult(result);
				Interlocked.Decrement(ref state.Current);
			});

			return source.Task;
		}
	}
}

