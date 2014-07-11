using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Servya.Testing
{
	public enum TestResult
	{
		Pending,
		Ok,
		Failed,
		Ignored
	}

	public class Test
	{
		public string Name { get; private set; }
		public Exception Error { get; private set; }

		private MethodInfo m_method;
		private object m_instance;
		private MethodInfo m_preTestMethod;

		public Test(MethodInfo method, object instance, MethodInfo preTest)
		{
			m_method = method;
			m_instance = instance;
			m_preTestMethod = preTest;
			Name = string.Format("{0}: {1}", method.DeclaringType.Name, Regex.Replace(method.Name, "(\\B[A-Z])", " $1").ToLower());
		}

		public async Task<TestResult> Execute()
		{
			try
			{
				if (m_preTestMethod != null)
					await Invoke(m_preTestMethod);

				await Invoke(m_method);
				return TestResult.Ok;
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
					ex = ex.InnerException;

				Error = ex;
				return TestResult.Failed;
			}
		}

		private async Task Invoke(MethodInfo method)
		{
			var result = method.Invoke(m_instance, null);

			var task = result as Task;
			if (task != null)
				await task;
		}
	}
}
