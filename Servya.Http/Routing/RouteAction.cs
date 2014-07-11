using System.Collections.Generic;
using System.Threading.Tasks;

namespace Servya
{
	public delegate Task RouteAction(IHttpContext context, IDictionary<string, string> args);
}
