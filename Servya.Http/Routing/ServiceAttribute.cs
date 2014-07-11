using System;

namespace Servya
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ServiceAttribute : Attribute
	{
		public string Name { get; set; }

		public static string GetName(Type type, ServiceAttribute attr)
		{
			return attr.Name ?? type.Name.Replace("Service", string.Empty);
		}
	}
}

