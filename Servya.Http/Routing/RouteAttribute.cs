using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Servya
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RouteAttribute : Attribute
	{
		public HttpVerb Verb { get; set; }
		public string Path { get; set; }
		public bool EnableChunking { get; set; }
		public Type QueryValidatorType { get; set; }

		public RouteAttribute(HttpVerb verb = HttpVerb.Get)
		{
			Verb = verb;
		}

		public virtual IEnumerable<RouteAttribute> Create()
		{
			yield return this;
		}

		public virtual void ModifyHttpContext(IHttpContext context)
		{
		}

		public virtual string ModifyPath(string path)
		{
			return path;
		}

		public virtual object Transform(object response)
		{
			return response;
		}

		public virtual object HandleError(RouteError error)
		{
			return error;
		}

		public virtual T CloneAs<T>()
			where T : RouteAttribute, new()
		{
			return new T
			{
				Verb = Verb,
				Path = Path,
				EnableChunking = EnableChunking,
				QueryValidatorType = QueryValidatorType
			};
		}
	}
}

