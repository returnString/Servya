using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CSharp;

namespace Servya
{
	public static class Reflection
	{
		private static readonly bool m_disableDefaults;
		private static readonly CategoryLogger m_logger;
		private static readonly ConcurrentDictionary<Type, Func<object>> m_defaultInstanceGetters;

		private static void DummyMethod(bool arg = false)
		{
		}

		static Reflection()
		{
			m_defaultInstanceGetters = new ConcurrentDictionary<Type, Func<object>>();
			m_logger = new CategoryLogger("Reflection");

			var method = typeof(Reflection).GetMethod("DummyMethod", BindingFlags.Static | BindingFlags.NonPublic);
			var param = method.GetParameters()[0];

			try
			{
				m_logger.Info("Support for default values enabled", param.HasDefaultValue);
			}
			catch (NotImplementedException)
			{
				m_disableDefaults = true;
			}
		}

		public static T CreateEmpty<T>()
		{
			return (T)FormatterServices.GetUninitializedObject(typeof(T));
		}

		public static bool TryGetAttribute<T>(this MemberInfo member, out T attribute) where T : Attribute
		{
			attribute = member.GetCustomAttribute<T>();
			return attribute != null;
		}

		public static bool HasAttribute<T>(this MemberInfo member) where T : Attribute
		{
			T unused;
			return member.TryGetAttribute<T>(out unused);
		}

		public static IEnumerable<Type> GetAllTypes()
		{
			return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
		}

		public static IEnumerable<Type> GetChildren<T>()
		{
			var parent = typeof(T);
			return GetAllTypes().Where(t => parent.IsAssignableFrom(t) && t != parent);
		}

		public static object GetDefaultInstance(this Type type)
		{
			var func = m_defaultInstanceGetters.GetOrAdd(type, k =>
			{
				var e = Expression.Lambda<Func<object>>(
					Expression.Convert(Expression.Default(type), typeof(object)));

				return e.Compile();
			});

			return func();
		}

		public static bool TryGetDefaultValue(this ParameterInfo param, out object value)
		{
			if (m_disableDefaults || !param.HasDefaultValue)
			{
				value = null;
				return false;
			}

			value = param.DefaultValue;
			return true;
		}

		public static string GetFriendlyName(this Type type)
		{
			var underlying = Nullable.GetUnderlyingType(type);
			if (underlying != null)
				type = underlying;

			if (type.IsGenericType)
			{
				return string.Format("{0}<{1}>", type.Name.Substring(0, type.Name.Length - 2), string.Join(", ", type.GetGenericArguments().Select(t => t.GetFriendlyName())));
			}

			using (var provider = new CSharpCodeProvider())
			{
				var typeRef = new CodeTypeReference(type);
				var friendlyName = provider.GetTypeOutput(typeRef);

				if (friendlyName == type.FullName)
					return type.Name;

				return friendlyName;
			}
		}

		public static TEnum[] GetEnumValues<TEnum>()
		{
			return (TEnum[])Enum.GetValues(typeof(TEnum));
		}
	}
}

