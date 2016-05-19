namespace Castle.NHibIntegration
{
	using System;

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class EnsureSessionClosed : Attribute
	{
	}
}