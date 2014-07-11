using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Servya
{
	public delegate bool ParseDelegate(Type type, string value, out object result);

	public class Parser
	{
		private readonly ConcurrentDictionary<Type, ParseDelegate> m_parsers;
		private readonly MethodInfo m_enumParse;

		public Parser()
		{
			m_enumParse = typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => m.Name == "TryParse" && m.GetParameters().Length == 3)
				.First();

			m_parsers = new ConcurrentDictionary<Type, ParseDelegate>();
		}

		public bool TryParse(Type type, string value, out object result)
		{
			var underlying = Nullable.GetUnderlyingType(type);
			if (underlying != null)
				type = underlying;

			if (type == typeof(string))
			{
				result = value;
				return true;
			}

			var del = m_parsers.GetOrAdd(type, Create);

			if (del == null)
			{
				result = null;
				return false;
			}

			return del(type, value, out result);
		}

		private ParseDelegate Create(Type type)
		{
			if (type.IsEnum)
			{
				return CreateEnum(type);
			}
			else if (type.IsArray)
			{
				return CreateArray(type);
			}

			var methods = from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
						  where m.Name == "TryParse"
						  where m.GetParameters().Length == 2
						  select m;

			var method = methods.FirstOrDefault();
			if (method == null)
				return null;

			ParseDelegate parser = (Type paramType, string value, out object result) =>
			{
				var args = new[] { value, paramType.GetDefaultInstance() };
				var methodResult = method.Invoke(null, args);
				result = args[1];
				return (bool)methodResult;
			};

			return parser;
		}

		private ParseDelegate CreateEnum(Type type)
		{
			var enumMethod = m_enumParse.MakeGenericMethod(type);

			return (Type enumType, string value, out object result) =>
			{
				int numeric;
				if (int.TryParse(value, out numeric) && !Enum.IsDefined(enumType, numeric))
				{
					result = null;
					return false;
				}

				var args = new object[] { value, true, Enum.ToObject(type, 0) };
				var success = (bool)enumMethod.Invoke(null, args);
				result = args[2];
				return success;
			};
		}

		private ParseDelegate CreateArray(Type type)
		{
			var elemType = type.GetElementType();
			var elemParser = m_parsers.GetOrAdd(elemType, Create);

			return (Type arrayType, string value, out object result) =>
			{
				var split = Regex.Matches(value, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToArray();
				var array = Array.CreateInstance(elemType, split.Length);

				for (var i = 0; i < array.Length; i++)
				{
					object temp;
					if (!elemParser(type, split[i], out temp))
					{
						result = null;
						return false;
					}

					array.SetValue(temp, i);
				}

				result = array;
				return true;
			};
		}
	}
}
