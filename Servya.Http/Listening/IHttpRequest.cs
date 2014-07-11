using System;
using System.IO;
using System.Net;
using System.Text;

namespace Servya
{
	public interface IHttpRequest
	{
		Uri Url { get; }
		HttpVerb Verb { get; }
		string RawVerb { get; }
		IPEndPoint Client { get; }
		Stream Stream { get; }
		Encoding Encoding { get; }
		bool KeepAlive { get; }
	}
}
