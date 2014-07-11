using System;

namespace Servya
{
	public abstract class DependencyException : Exception
	{
		public DependencyException(string format, params object[] args)
            : base(string.Format(format, args))
		{
		}

		public DependencyException(Exception inner, string format, params object[] args)
			: base(string.Format(format, args), inner)
		{
		}
	}

	public class DependencyNotFoundException : DependencyException
	{
		public DependencyNotFoundException(Type type)
			: base("Type {0} could not be resolved", type)
		{
		}
	}

	public class DependencyCreationFailedException : DependencyException
	{
		public DependencyCreationFailedException(Type type)
			: base("Type {0} could not be created, no constructors were appropriate", type)
		{
		}

		public DependencyCreationFailedException(Type type, Exception inner)
			: base(inner, "Type {0} could not be created, {1} was thrown", type, inner.GetType())
		{
		}
	}
}
