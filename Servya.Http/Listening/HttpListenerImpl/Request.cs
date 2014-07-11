using System;
using System.IO;
using System.Net;
using System.Text;

namespace Servya.SystemNetImpl
{
	internal class Request : IHttpRequest
	{
		public Uri Url { get; private set; }
		public HttpVerb Verb { get; private set; }
		public string RawVerb { get; private set; }
		public IPEndPoint Client { get; private set; }
		public Stream Stream { get; private set; }
		public Encoding Encoding { get; private set; }
		public bool KeepAlive { get; private set; }

		public Request(HttpListenerRequest request)
		{
			Url = request.Url;
			RawVerb = request.HttpMethod;
			Client = request.RemoteEndPoint;
			Stream = request.InputStream;
			Encoding = request.ContentEncoding;
			KeepAlive = request.KeepAlive;

			HttpVerb verb;
			if (!Enum.TryParse(RawVerb, true, out verb))
				verb = HttpVerb.Unknown;

			Verb = verb;
		}
	}
}

