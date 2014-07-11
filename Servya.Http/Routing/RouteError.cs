using System;

namespace Servya
{
	public enum RouteErrorDomain
	{
		Request = -1,
		Invocation = -2
	}

	public class RouteError
	{
		public int Code { get; private set; }
		public string Message { get; private set; }
		public bool Internal { get; private set; }

		internal RouteError(RouteErrorDomain code, string message)
			: this((Enum)code, message)
		{
			Internal = true;
		}

		public RouteError(Enum code, string message)
		{
			Code = Convert.ToInt32(code);
			Message = message;
		}

		public override string ToString()
		{
			return string.Format("(Domain: {0}) {1}", Code, Message);
		}
	}
}
