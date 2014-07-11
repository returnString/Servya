using System;
using System.Threading.Tasks;

namespace Servya
{
	public interface IHttpProcessor : IDisposable
	{
		Task Process(IHttpContext context, bool busy);
	}
}
