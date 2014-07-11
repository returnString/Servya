using System.Collections.Generic;
using System.Threading.Tasks;

namespace Servya
{
	public interface IQueryValidator
	{
		Task<RouteError> Validate(IHttpContext context, IDictionary<string, string> queryArgs);
	}
}
