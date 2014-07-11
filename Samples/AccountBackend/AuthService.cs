using Servya;
using System.Security.Cryptography;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Text;

namespace AccountBackend
{
	public enum RegistrationStatus
	{
		Ok,
		UnknownError,
		NameTaken,
	}

	public enum AuthStatus
	{
		Ok,
		Failed
	}

	[Service]
	public class AuthService
	{
		private readonly IDatabase m_db;

		public AuthService(IDatabase db)
		{
			m_db = db;
		}

		[Route]
		public async Task<RegistrationStatus> Register(string name, string password)
		{
			var key = UserKey(name);

			var transaction = m_db.CreateTransaction();
			var nameNotTaken = transaction.AddCondition(Condition.KeyNotExists(key));
			transaction.HashSetAsync(key, "password", PasswordHash(password)).Forget();

			if (await transaction.ExecuteAsync())
				return RegistrationStatus.Ok;

			if (!nameNotTaken.WasSatisfied)
				return RegistrationStatus.NameTaken;

			return RegistrationStatus.UnknownError;
		}

		[Route]
		public async Task<AuthStatus> Login(string name, string password)
		{
			var key = UserKey(name);

			var storedPassword = await m_db.HashGetAsync(key, "password");

			if (storedPassword == PasswordHash(password))
				return AuthStatus.Ok;

			return AuthStatus.Failed;
		}

		private byte[] PasswordHash(string password)
		{
			using (var alg = new SHA512Managed())
			{
				var bytes = Encoding.UTF8.GetBytes(password);
				return alg.ComputeHash(bytes);
			}
		}

		private string UserKey(string name)
		{
			return "user:" + name;
		}
	}
}

