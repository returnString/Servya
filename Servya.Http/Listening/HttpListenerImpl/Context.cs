using System.Net;

namespace Servya.SystemNetImpl
{
	internal class Context : IHttpContext
	{
		public IHttpRequest Request { get; private set; }
		public IHttpResponse Response { get; private set; }

		public Context(HttpListenerContext context)
		{
			Request = new Request(context.Request);
			Response = new Response(context.Response);
		}
	}
}
