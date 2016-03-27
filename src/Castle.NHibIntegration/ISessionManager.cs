namespace Castle.NHibIntegration
{
	using System;
	using NHibernate;

	/// <summary>
	/// Provides a bridge to NHibernate allowing the implementation
	/// to cache created session (through an invocation) and 
	/// enlist it on transaction if one is detected on the thread.
	/// </summary>
	public interface ISessionManager
	{
		/// <summary>
		/// The flushmode the created session gets
		/// </summary>
		FlushMode DefaultFlushMode { get; set; }

		/// <summary>
		/// Returns a valid opened and connected ISession instance.
		/// </summary>
		/// <returns></returns>
		ISession OpenSession();

		/// <summary>
		/// Returns a valid opened and connected ISession instance
		/// for the given connection alias.
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		ISession OpenSession(String alias);

		/// <summary>
		/// Returns a valid opened and connected IStatelessSession instance.
		/// </summary>
		/// <returns></returns>
//		IStatelessSession OpenStatelessSession();

		/// <summary>
		/// Returns a valid opened and connected IStatelessSession instance
		/// for the given connection alias.
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
//		IStatelessSession OpenStatelessSession(String alias);
	}
}