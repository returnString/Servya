using System;

namespace Servya
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class DependencyAttribute : Attribute
	{
		public bool Singleton { get; set; }
	}
}
