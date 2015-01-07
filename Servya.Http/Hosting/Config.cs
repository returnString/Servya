
namespace Servya
{
	public class Config
	{
		public bool Debug { get; set; }
		public HttpConfig Http { get; set; }

		public static Config DevDefault
		{
			get
			{
				return new Config { Http = HttpConfig.DevDefault };
			}
		}
	}
}

