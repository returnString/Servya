using System.Collections.Generic;
using System.Threading.Tasks;
using Servya;
using StackExchange.Redis;

namespace AccountBackend
{
	public class TokenValidator : IQueryValidator
	{
		private readonly IDatabase m_db;

		private static readonly RouteError TokenMissing = new RouteError(Status.TokenMissing);
		private static readonly RouteError TokenInvalid = new RouteError(Status.TokenExpired);

		public TokenValidator(IDatabase db)
		{
			m_db = db;
		}

		public async Task<RouteError> Validate(IHttpContext context, IDictionary<string, string> queryArgs)
		{
			string token;
			if (!queryArgs.TryGetValue("token", out token))
				return TokenMissing;

			var user = await m_db.StringGetAsync("token:" + token);
			if (user.IsNullOrEmpty)
				return TokenInvalid;

			queryArgs["user"] = user;
			return null;
		}
	}
}

