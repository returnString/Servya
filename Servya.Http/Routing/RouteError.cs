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

		public RouteError(Enum code)
			: this(code, code.ToString())
		{
		}

		public override string ToString()
		{
			return string.Format("{0} (code {1})", Message, Code);
		}
	}
}
