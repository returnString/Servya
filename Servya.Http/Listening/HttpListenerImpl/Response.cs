using System.IO;
using System.Net;

namespace Servya.SystemNetImpl
{
	internal class Response : IHttpResponse
	{
		public Stream Stream { get; private set; }

		private readonly HttpListenerResponse m_resp;

		public Response(HttpListenerResponse response)
		{
			m_resp = response;
			Status = (HttpStatusCode)response.StatusCode;
			Stream = response.OutputStream;
		}

		public void Dispose()
		{
			m_resp.Close();
		}


		public bool Chunked
		{
			get
			{
				return m_resp.SendChunked;
			}
			set
			{
				m_resp.SendChunked = value;
			}
		}

		public long ContentLength
		{
			get
			{
				return m_resp.ContentLength64;
			}
			set
			{
				m_resp.ContentLength64 = value;
			}
		}

		public string ContentType
		{
			get
			{
				return m_resp.ContentType;
			}
			set
			{
				m_resp.ContentType = value;
			}
		}

		public HttpStatusCode Status
		{
			get
			{
				return (HttpStatusCode)m_resp.StatusCode;
			}
			set
			{
				m_resp.StatusCode = (int)value;
			}
		}

		public bool KeepAlive
		{
			get
			{
				return m_resp.KeepAlive;
			}
			set
			{
				m_resp.KeepAlive = value;
			}
		}
	}
}
