namespace Castle.NHibIntegration.AutoClosing
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public interface INhMetaInfoStore
	{
		AutoCloseClassMetaInfo GetMetaFromType(Type implementation);

		IList<MethodInfo> SanityCheck(Type implementation);
	}

	public class NhMetaInfoStore : INhMetaInfoStore
	{
		private readonly ConcurrentDictionary<Type, AutoCloseClassMetaInfo> _type2Meta;

		public NhMetaInfoStore()
		{
			_type2Meta = new ConcurrentDictionary<Type, AutoCloseClassMetaInfo>(); 
		}

		public IList<MethodInfo> SanityCheck(Type implementation)
		{
			var problematicMethods =
				implementation
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(method => method.IsDefined(typeof(AutoCloseSessionAttribute), true) && !method.IsVirtual)
					.ToList();

			return problematicMethods;
		}

		public AutoCloseClassMetaInfo GetMetaFromType(Type implementation)
		{
			return _type2Meta.GetOrAdd(implementation, (t) =>
			{
				var autoClosedEnabledMethods =
					implementation.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(method => method.IsDefined(typeof(AutoCloseSessionAttribute), true))
						.ToList();

				if (autoClosedEnabledMethods.Any())
				{
					return new AutoCloseClassMetaInfo(autoClosedEnabledMethods);
				}

				return null;
			});
		}
	}
}