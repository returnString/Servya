using System;
using System.Linq.Expressions;

namespace Servya.Testing
{
	public class AssertionFailedException : Exception
	{
		public AssertionFailedException(string message, params object[] args)
			: base(string.Format(message, args))
		{
		}

		public AssertionFailedException(Exception inner, string message, params object[] args)
			: base(string.Format(message, args), inner)
		{
		}
	}

	public static class Assert
	{
		public static void Equal<T>(T x, T y)
		{
			if (!x.Equals(y))
				throw new AssertionFailedException("Expected {0}, got {1}", y, x);
		}

		public static T Throws<T>(Action action) where T : Exception
		{
			Exception wrongEx = null;

			try
			{
				action();
			}
			catch (Exception ex)
			{
				var cast = ex as T;

				if (cast != null)
					return cast;
				else
					wrongEx = ex;
			}

			if (wrongEx == null)
				throw new AssertionFailedException("Expected {0} to be thrown", typeof(T).Name);
			else
				throw new AssertionFailedException(wrongEx, "Expected {0} to be thrown, got {1}", typeof(T).Name, wrongEx.GetType().Name);
		}
	}
}
