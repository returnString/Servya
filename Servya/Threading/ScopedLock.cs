using System;
using System.Threading;

namespace Servya
{
	public struct ReadLock : IDisposable
	{
		private readonly ReaderWriterLockSlim m_lock;

		public ReadLock(ReaderWriterLockSlim l)
		{
			m_lock = l;
			m_lock.EnterReadLock();
		}

		public void Dispose()
		{
			m_lock.ExitReadLock();
		}
	}

	public struct WriteLock : IDisposable
	{
		private readonly ReaderWriterLockSlim m_lock;

		public WriteLock(ReaderWriterLockSlim l)
		{
			m_lock = l;
			m_lock.EnterWriteLock();
		}

		public void Dispose()
		{
			m_lock.ExitWriteLock();
		}
	}
}
