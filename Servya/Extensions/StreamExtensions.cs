using System.IO;
using System.Threading.Tasks;

namespace Servya
{
	public static class StreamExtensions
	{
		public const int DefaultBufferSize = 1024;

		public static async Task WriteLineAsync(this TextWriter writer, string format, params object[] args)
		{
			await writer.WriteLineAsync(string.Format(format, args));
		}

		public static async Task<string> ReadAllAsync(this Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				return await reader.ReadToEndAsync();
			}
		}
	}
}
