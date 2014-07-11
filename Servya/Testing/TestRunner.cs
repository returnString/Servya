using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servya.Testing
{
	public class TestRunner
	{
		private readonly DependencyResolver m_resolver;
		private readonly CategoryLogger m_logger;

		public TestRunner(DependencyResolver resolver)
		{
			m_resolver = resolver;
			m_logger = new CategoryLogger(this);
		}

		public async Task Run()
		{
			var categories = new Dictionary<TestResult, List<Test>>();
			foreach (var resultType in Reflection.GetEnumValues<TestResult>())
				categories.Add(resultType, new List<Test>());

			foreach (var type in Reflection.GetAllTypes())
			{
				var methods = type.GetMethods();

				PreTestAttribute preTestAttr;
				var preTestMethod = methods.FirstOrDefault(m => m.TryGetAttribute(out preTestAttr));

				object instance = null;

				foreach (var method in methods)
				{
					TestAttribute attr;
					if (!method.TryGetAttribute(out attr))
						continue;

					if (instance == null)
						instance = m_resolver.Create(type);

					var test = new Test(method, instance, preTestMethod);

					var result = await test.Execute();

					switch (result)
					{
						case TestResult.Failed:
							m_logger.Error("[{0}] {1}", test.Name, test.Error);
							break;

						default:
							m_logger.Info("[{0}] {1}", test.Name, result);
							break;
					}

					categories[result].Add(test);
				}
			}

			var ok = categories[TestResult.Ok];
			var failed = categories[TestResult.Failed];

			m_logger.Info("{0} tests succeeded", ok.Count);
			m_logger.Info("{0} tests failed", failed.Count);
		}
	}
}
