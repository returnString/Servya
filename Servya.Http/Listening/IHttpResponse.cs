using System;
using System.IO;
using System.Net;

namespace Servya
{
	public interface IHttpResponse : IDisposable
	{
		HttpStatusCode Status { get; set; }
		Stream Stream { get; }
		bool Chunked { get; set; }
		long ContentLength { get; set; }
		string ContentType { get; set; }
		bool KeepAlive { get; set; }
	}
}
