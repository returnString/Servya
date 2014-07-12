using System.IO;

namespace Servya
{
	public static class FileIO
	{
		private const int DefaultBufferSize = 1024;

		public static FileStream Open(string filename, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read,
			FileShare share = FileShare.Read, int bufferSize = DefaultBufferSize)
		{
			return new FileStream(filename, mode, access, share, bufferSize, useAsync: true);
		}

		public static FileStream OpenRead(string filename, int bufferSize = DefaultBufferSize)
		{
			return Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
		}

		public static FileStream OpenWrite(string filename, bool overwrite = false, int bufferSize = DefaultBufferSize)
		{
			return Open(filename, overwrite ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
		}
	}
}
