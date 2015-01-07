using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Servya
{
	public class AutoRouter
	{
		private readonly Router m_processor;
		private readonly Dictionary<Type, PropertyInfo> m_taskResultLookup;
		private readonly Parser m_argParser;
		private readonly DependencyResolver m_resolver;
		private readonly CategoryLogger m_logger;
		private readonly bool m_debug;

		private static Type[] ExcludedFromCache = { typeof(void), typeof(Task) };

		public AutoRouter(Router processor, Parser argParser, DependencyResolver resolver, bool debug)
		{
			m_logger = new CategoryLogger(this);
			m_processor = processor;
			m_taskResultLookup = new Dictionary<Type, PropertyInfo>();
			m_argParser = argParser;
			m_resolver = resolver;
			m_debug = debug;

			CacheResultProperty(typeof(Task<object>));
		}

		public void AddStaticPage(string location, string content)
		{
			var cache = new ConcurrentDictionary<string, byte[]>();

			m_processor.AddRoute(HttpVerb.Get, location, async (context, args) =>
			{
				var encoding = context.Request.Encoding;
				var data = cache.GetOrAdd(encoding.EncodingName, k => encoding.GetBytes(content));
				var response = context.Response;
				response.ContentLength = data.Length;
				await response.Stream.WriteAsync(data, 0, data.Length);
			});
		}

		public void CreateWebInterface(WebInterfaceConfig config = null, string location = "/")
		{
			var page = WebInterface.Create(config ?? WebInterfaceConfig.Default);
			AddStaticPage(location, page);
		}

		public void Discover()
		{
			m_logger.Info("Performing automatic service discovery");

			foreach (var type in Reflection.GetAllTypes())
			{
				ServiceAttribute attr;
				if (!type.TryGetAttribute(out attr))
					continue;

				var instance = m_resolver.Create(type);
				m_resolver.Add(type, instance);
				var name = ServiceAttribute.GetName(type, attr);
				Register(instance, name);
			}
		}

		private void Register(object service, string serviceName)
		{
			m_logger.Info("Registering new reflected service: {0}", serviceName);

			foreach (var method in service.GetType().GetMethods())
			{
				foreach (var routeAttr in method.GetCustomAttributes<RouteAttribute>())
					RegisterRoute(service, serviceName, method, routeAttr);
			}
		}

		private void RegisterRoute(object service, string serviceName, MethodInfo method, RouteAttribute routeAttr)
		{
			foreach (var attr in routeAttr.Create())
			{
				var returnType = method.ReturnType;
				var cache = !ExcludedFromCache.Contains(returnType);

				if (cache && !returnType.IsSubclassOf(typeof(Task)))
					returnType = typeof(Task<>).MakeGenericType(returnType);

				if (string.IsNullOrEmpty(attr.Path))
					attr.Path = method.Name;

				var path = string.Format("/{0}/{1}", serviceName, attr.Path);
				path = attr.ModifyPath(path).ToLower();

				var route = CreateRoute(attr, method, service);
				m_processor.AddRoute(attr.Verb, path, route);

				if (cache && !m_taskResultLookup.ContainsKey(returnType))
				{
					CacheResultProperty(returnType);
					m_logger.Debug("Cached task result property for type: {0}", returnType.GetGenericArguments()[0].GetFriendlyName());
				}
			}
		}

		private void CacheResultProperty(Type returnType)
		{
			m_taskResultLookup.Add(returnType, returnType.GetProperty("Result"));
		}

		private RouteAction CreateRoute(RouteAttribute routeAttr, MethodInfo method, object service)
		{
			var methodParams = method.GetParameters();

			IQueryValidator argResolver = null;
			if (routeAttr.QueryValidatorType != null)
				argResolver = (IQueryValidator)m_resolver.Create(routeAttr.QueryValidatorType);

			return async (context, routeArgs) =>
			{
				var response = context.Response;
				var request = context.Request;

				response.Chunked = routeAttr.EnableChunking;
				routeAttr.ModifyHttpContext(context);

				object taskResult;

				try
				{
					var result = await GetResult(context, routeArgs, service, method, methodParams, argResolver);

					var routeError = result as RouteError;
					if (routeError != null)
						taskResult = routeAttr.HandleError(routeError);
					else
						taskResult = routeAttr.Transform(result);
				}
				catch (Exception ex)
				{
					if (ex is TargetInvocationException)
						ex = ex.InnerException;

					m_logger.Error(ex);
					response.Status = HttpStatusCode.InternalServerError;

					var message = m_debug ? ex.ToString() : "Internal server error";

					var error = new RouteError(RouteErrorDomain.Invocation, message);
					taskResult = routeAttr.HandleError(error);
				}

				if (taskResult != null)
				{
					var data = request.Encoding.GetBytes(taskResult.ToString() + "\r\n");

					if (!response.Chunked)
						response.ContentLength = data.Length;

					await response.Stream.WriteAsync(data, 0, data.Length);
				}
			};
		}

		private async Task<object> GetResult(IHttpContext context, IDictionary<string, string> routeArgs,
			object service, MethodInfo method, ParameterInfo[] methodParams, IQueryValidator argResolver)
		{
			var urlArgs = new Dictionary<string, string>();

			var paramSource = await GetParamSource(context.Request);

			foreach (var param in paramSource.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var tokens = param.Split('=');

				if (tokens.Length != 2)
					return Error(context, "Invalid query string");

				urlArgs.Add(tokens[0].ToLower(), Uri.UnescapeDataString(tokens[1]));
			}

			var methodArgs = new object[methodParams.Length];

			if (argResolver != null)
			{
				var error = await argResolver.Validate(context, urlArgs);
				if (error != null)
					return error;
			}

			for (var i = 0; i < methodArgs.Length; i++)
			{
				var param = methodParams[i];
				var argResult = TryGetArg(context, param, urlArgs, routeArgs, argResolver);

				if (argResult is RouteError)
					return argResult;

				methodArgs[i] = argResult;
			}

			var result = method.Invoke(service, methodArgs);

			var task = result as Task;
			if (task == null)
				return result;

			await task;
			PropertyInfo getter;
			if (m_taskResultLookup.TryGetValue(task.GetType(), out getter))
				return getter.GetValue(task);
			else
				return null;
		}

		private object TryGetArg(IHttpContext context, ParameterInfo param, IDictionary<string, string> urlArgs,
			IDictionary<string, string> routeArgs, IQueryValidator argResolver)
		{
			var type = param.ParameterType;

			if (type == typeof(IHttpContext))
				return context;

			var paramName = param.Name.ToLower();

			string argValue;
			var gotValue = urlArgs.TryGetValue(paramName, out argValue)
				|| (routeArgs != null && routeArgs.TryGetValue(paramName, out argValue));

			if (!gotValue)
			{
				object defaultValue;
				if (param.TryGetDefaultValue(out defaultValue))
					return defaultValue;

				if (Nullable.GetUnderlyingType(param.ParameterType) != null)
					return null;

				return Error(context, "Missing param '{0}'", paramName);
			}

			if (string.IsNullOrEmpty(argValue))
				return Error(context, "Empty param '{0}'", paramName);

			object parsedArg;
			if (!m_argParser.TryParse(type, argValue, out parsedArg))
				return Error(context, "Param '{0}' is invalid", paramName);

			return parsedArg;
		}

		private static async Task<string> GetParamSource(IHttpRequest request)
		{
			switch (request.Verb)
			{
			case HttpVerb.Post:
				return await request.Stream.ReadAllAsync();

			default:
				return request.Url.Query;
			}
		}

		private static RouteError Error(IHttpContext context, string message, params object[] args)
		{
			context.Response.Status = HttpStatusCode.BadRequest;
			return new RouteError(RouteErrorDomain.Request, string.Format(message, args));
		}
	}
}

