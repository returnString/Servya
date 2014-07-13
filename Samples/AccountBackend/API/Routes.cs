using Servya;
using Newtonsoft.Json;

namespace AccountBackend
{
	public class UnprotectedRouteAttribute : RouteAttribute
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
			return Transform(new Response(error.Code, error.Message));
		}
	}

	public class TokenRouteAttribute : UnprotectedRouteAttribute
	{
		public TokenRouteAttribute()
		{
			QueryValidatorType = typeof(TokenValidator);
		}
	}
}

