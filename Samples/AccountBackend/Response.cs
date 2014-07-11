using System;

namespace AccountBackend
{
	public class Response
	{
		public Status Status { get; set; }

		public static implicit operator Response(Status status)
		{
			return new Response { Status = status };
		}
	}

	public class Response<T> : Response
	{
		public T Payload { get; set; }

		public static implicit operator Response<T>(T payload)
		{
			return new Response<T> { Payload = payload };
		}

		public static implicit operator Response<T>(Status status)
		{
			return new Response<T> { Status = status };
		}
	}

	public enum Status
	{
		Ok,
		UnknownError,
		NameTaken,
		InvalidCredentials,
		TokenExpired
	}
}

