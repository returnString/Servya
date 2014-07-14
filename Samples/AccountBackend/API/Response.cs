using System;

namespace AccountBackend
{
	public class Response
	{
		public int Code { get; private set; }
		public string Info { get; private set; }

		public Response(int code, string info)
		{
			Code = code;
			Info = info;
		}

		public Response(Status status)
			: this((int)status, status.ToString())
		{
		}

		public static implicit operator Response(Status status)
		{
			return new Response(status);
		}
	}

	public class Response<T> : Response
	{
		public T Payload { get; set; }

		public Response(T payload, Status status)
			: base(status)
		{
			Payload = payload;
		}

		public static implicit operator Response<T>(T payload)
		{
			return new Response<T>(payload, (int)Status.Ok);
		}

		public static implicit operator Response<T>(Status status)
		{
			return new Response<T>(default(T), status);
		}
	}

	public enum Status
	{
		Ok,
		InternalError,
		NameTaken,
		InvalidCredentials,
		TokenExpired,
		TokenMissing,
		NotFound
	}
}

