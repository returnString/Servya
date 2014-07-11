using System;

namespace Servya.Testing
{
	[AttributeUsage(AttributeTargets.Method)]
	public class TestAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class PreTestAttribute : Attribute
	{
	}
}
