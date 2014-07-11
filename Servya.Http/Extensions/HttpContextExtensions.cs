using System.IO;

namespace Servya
{
	public static class HttpContextExtensions
	{
		public static StreamWriter GetWriter(this IHttpContext context)
		{
			return new StreamWriter(context.Response.Stream, context.Request.Encoding, StreamExtensions.DefaultBufferSize, true);
		}
	}
}
