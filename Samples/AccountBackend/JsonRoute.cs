using Servya;
using Newtonsoft.Json;

namespace AccountBackend
{
	public class JsonRouteAttribute : RouteAttribute
	{
		public override object Transform(object response)
		{
			return JsonConvert.SerializeObject(response);
		}

		public override object HandleError(RouteError error)
		{
			return Transform(new Response<RouteError> { Payload = error, Status = Status.UnknownError });
		}
	}
}

