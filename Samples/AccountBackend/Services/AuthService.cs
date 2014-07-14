using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Servya;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace AccountBackend
{
	[Service]
	public class AuthService
	{
		private readonly IDatabase m_db;
		private readonly TimeSpan m_tokenTTL;
		private readonly HttpClient m_client;
		private readonly CategoryLogger m_logger;

		public AuthService(IDatabase db)
		{
			m_logger = new CategoryLogger(this);
			m_db = db;
			m_tokenTTL = TimeSpan.FromMinutes(10);
			m_client = new HttpClient { BaseAddress = new Uri("http://freegeoip.net/json/") };
		}

		[UnprotectedRoute(Verb = HttpVerb.Post)]
		public async Task<Response> Register(string name, string password, IHttpContext context)
		{
			var key = Keys.User(name);
			var transaction = m_db.CreateTransaction();
			var nameNotTaken = transaction.AddCondition(Condition.KeyNotExists(key));

			transaction.HashSetAsync(key,
				Fields.Password, PasswordHash(password),
				Fields.Password, DateTime.UtcNow.GetUnixTime(),
				Fields.Country, await GetCountryCode(context.Request.Client.Address)
			).Forget();

			if (await transaction.ExecuteAsync())
				return Status.Ok;

			if (!nameNotTaken.WasSatisfied)
				return Status.NameTaken;

			return Status.InternalError;
		}

		[UnprotectedRoute(Verb = HttpVerb.Post)]
		public async Task<Response<string>> Login(string name, string password)
		{
			var key = Keys.User(name);
			var storedPassword = await m_db.HashGetAsync(key, Fields.Password);

			if (storedPassword != PasswordHash(password))
				return Status.InvalidCredentials;

			var token = Guid.NewGuid().ToString();
			await m_db.StringSetAsync(Keys.Token(token), name, m_tokenTTL);
			return token;
		}

		[UnprotectedRoute]
		public async Task<Response> Verify(string token)
		{
			if (await m_db.KeyExpireAsync(Keys.Token(token), m_tokenTTL))
				return Status.Ok;

			return Status.TokenExpired;
		}

		private byte[] PasswordHash(string password)
		{
			using (var alg = new SHA512Managed())
			{
				var bytes = Encoding.UTF8.GetBytes(password);
				return alg.ComputeHash(bytes);
			}
		}

		private struct GeoLocationResponse
		{
			public string country_code;
		}

		private async Task<string> GetCountryCode(IPAddress address)
		{
			try
			{
				var response = await m_client.GetStringAsync(address.ToString());
				var json = JsonConvert.DeserializeObject<GeoLocationResponse>(response);
				return json.country_code;
			}
			catch (Exception ex)
			{
				m_logger.Error(ex);
				return "RD";
			}
		}
	}
}

