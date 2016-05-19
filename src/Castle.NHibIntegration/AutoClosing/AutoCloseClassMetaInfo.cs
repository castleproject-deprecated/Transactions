namespace Castle.NHibIntegration.AutoClosing
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	public class AutoCloseClassMetaInfo
	{
		private readonly HashSet<string> _autoCloseEnabledMethodNames;

		public AutoCloseClassMetaInfo(IList<MethodInfo> methods)
		{
			_autoCloseEnabledMethodNames = new HashSet<string>(StringComparer.Ordinal);

			foreach (var method in methods)
			{
				_autoCloseEnabledMethodNames.Add(method.Name);
			}
		}

		public bool HasAutoCloseEnabled(MethodInfo target)
		{
			return _autoCloseEnabledMethodNames.Contains(target.Name);
		}
	}
}