
namespace Servya
{
	public class HostConfig
	{
		public bool Debug { get; set; }
		public HttpConfig Http { get; set; }

		public static HostConfig DevDefault
		{
			get
			{
				return new HostConfig
				{
					Debug = true,
					Http = HttpConfig.DevDefault
				};
			}
		}
	}
}

