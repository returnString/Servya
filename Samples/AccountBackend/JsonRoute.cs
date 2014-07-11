using Servya;
using Newtonsoft.Json;

namespace AccountBackend
{
	public class JsonRouteAttribute : RouteAttribute
	{
		private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
		};

		public override object Transform(object response)
		{
			return JsonConvert.SerializeObject(response, JsonSettings);
		}

		public override object HandleError(RouteError error)
		{
			return Transform(new Response<RouteError>(error, Status.UnknownError));
		}
	}
}

