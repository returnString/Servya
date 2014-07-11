﻿using System;

namespace AccountBackend
{
	public class Response
	{
		public Status Status { get; private set; }
		public string Info { get; private set; }

		public Response(Status status)
		{
			Status = status;
			Info = status.ToString();
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
			return new Response<T>(payload, Status.Ok);
		}

		public static implicit operator Response<T>(Status status)
		{
			return new Response<T>(default(T), status);
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

