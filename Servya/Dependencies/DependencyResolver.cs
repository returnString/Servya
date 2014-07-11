using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Servya
{
	public class DependencyResolver
	{
		private class Implementation
		{
			public Type Type { get; private set; }
			public object Instance { get; set; }

			public Implementation(Type type)
			{
				Type = type;
			}
		}

		private readonly Dictionary<Type, Implementation> m_map;
		private readonly List<Assembly> m_assemblies;
		private readonly CategoryLogger m_logger;

		public DependencyResolver()
		{
			m_map = new Dictionary<Type, Implementation>();
			m_assemblies = new List<Assembly>();
			m_logger = new CategoryLogger(this);

			AddAssembly(Assembly.GetEntryAssembly());
		}

		public void AddAssembly(Assembly assembly)
		{
			m_logger.Info("Considering '{0}' for dependency resolution", assembly.FullName);
			m_assemblies.Add(assembly);
		}

		public void Add(Type type, Type instanceType)
		{
			m_logger.Debug("Mapping {0} to {1}", type, instanceType);
			m_map[type] = new Implementation(instanceType);
		}

		public void Add(Type type, object instance)
		{
			m_logger.Debug("Mapping {0} to instance of {1}", type, instance.GetType());
			m_map[type] = new Implementation(instance.GetType()) { Instance = instance };
		}

		public void Add<T>(T instance)
		{
			Add(typeof(T), instance);
		}

		public void Add<T, TImpl>()
		{
			Add(typeof(T), typeof(TImpl));
		}

		public void CreateAll()
		{
			var implementations = m_map.Values;

			foreach (var impl in implementations)
			{
				if (impl.Instance != null)
					continue;

				var singleton = false;

				DependencyAttribute attr;
				if (impl.Type.TryGetAttribute(out attr))
					singleton = attr.Singleton;

				if (singleton)
					impl.Instance = Create(impl.Type);
			}
		}

		public void AddAll()
		{
			var types = m_assemblies.SelectMany(a => a.GetTypes()).ToArray();

			foreach (var type in types)
			{
				if (m_map.ContainsKey(type))
					continue;

				var children = types.Where(t => t != type && type.IsAssignableFrom(t)).ToArray();

				if (children.Length == 1)
					Add(type, children[0]);
			}
		}

		public object Get(Type type)
		{
			object instance;
			if (!TryGet(type, out instance))
				throw new DependencyNotFoundException(type);

			return instance;
		}

		public T Get<T>()
		{
			return (T)Get(typeof(T));
		}

		public IEnumerable<T> GetAll<T>()
		{
			foreach (var type in Reflection.GetChildren<T>())
			{
				object instance;
				if (TryGet(type, out instance))
					yield return (T)instance;
			}
		}

		public object Create(Type type)
		{
			var ctors = type.GetConstructors();

			foreach (var ctor in ctors)
			{
				object newInstance;
				if (TryCtor(ctor, out newInstance))
					return newInstance;
			}

			throw new DependencyCreationFailedException(type);
		}

		public T Create<T>()
		{
			return (T)Create(typeof(T));
		}

		private bool TryCtor(ConstructorInfo ctor, out object newInstance)
		{
			var ctorParams = ctor.GetParameters();
			var ctorArgs = new object[ctorParams.Length];

			for (var i = 0; i < ctorParams.Length; i++)
			{
				var paramType = ctorParams[i].ParameterType;
					
				object instance;
				if (!TryGet(paramType, out instance))
				{
					newInstance = null;
					return false;
				}

				ctorArgs[i] = instance;
			}

			try
			{
				newInstance = ctor.Invoke(ctorArgs);
			}
			catch (TargetInvocationException ex)
			{
				throw new DependencyCreationFailedException(ctor.DeclaringType, ex.InnerException);
			}

			return true;
		}

		public bool TryGet(Type type, out object instance)
		{
			Implementation entry;
			if (m_map.TryGetValue(type, out entry))
			{
				m_logger.Debug("Resolving {0} to {1} instance of {2}",
					type, entry.Instance == null ? "new" : "singleton", entry.Type);

				instance = entry.Instance ?? Create(entry.Type);
				return true;
			}

			instance = null;
			return false;
		}
	}
}
