using System;
using System.Collections.Generic;
using System.Linq;

namespace Servya
{
	internal class Route
	{
		public string Raw { get; private set; }
		public RouteComponent[] Components { get; private set; }

		public Route(string raw)
		{
			Raw = raw;
			Components = GetComponents(raw).Select(c => new RouteComponent(c)).ToArray();
		}

		public bool Validate(string[] urlComponents, out IDictionary<string, string> args)
		{
			args = null;

			if (urlComponents.Length != Components.Length)
				return false;

			for (var i = 0; i < Components.Length; i++)
			{
				var component = Components[i];
				var token = urlComponents[i];

				string output;
				if (!component.Validate(token, out output))
					return false;

				if (component.Included)
				{
					if (args == null)
						args = new Dictionary<string, string>();

					args.Add(component.Raw, output);
				}
			}

			return true;
		}

		public static string[] GetComponents(string path)
		{
			return path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}

	public enum RouteComponentType
	{
		Literal,
		Replace
	}

	public class RouteComponent
	{
		public string Raw { get; private set; }
		public bool Included { get; private set; }
		public RouteComponentType Type { get; private set; }

		public RouteComponent(string component)
		{
			Raw = component;

			if (component.StartsWith("{") && component.EndsWith("}"))
			{
				Raw = component.Replace("{", "").Replace("}", "");
				Included = true;
				Type = RouteComponentType.Replace;
			}
		}

		public bool Validate(string input, out string output)
		{
			switch (Type)
			{
			case RouteComponentType.Literal:
				if (input == Raw)
				{
					output = Raw;
					return true;
				}

				output = null;
				return false;

			case RouteComponentType.Replace:
				output = input;
				return true;

			default:
				throw new NotSupportedException("RouteComponentType is not supported");
			}
		}
	}
}

