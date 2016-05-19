namespace Castle.NHibIntegration
{
	using System;

	/// <summary>
	/// Adding this to a virtual method ensures any session opened will be closed at the end. 
	/// It does not distinguish from sessions opened before entering the method though, 
	/// so use it it in the bottom of the stack only.
	/// 
	/// Also, if the session is owned by a transaction, it will not be closed. Which is good. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class AutoCloseSessionAttribute : Attribute
	{
	}
}