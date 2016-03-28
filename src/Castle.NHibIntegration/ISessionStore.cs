namespace Castle.NHibIntegration
{
	using System;
	using Internal;

	/// <summary>
	/// Provides the contract for implementors who want to 
	/// store valid session so they can be reused in a invocation
	/// chain.
	/// </summary>
	public interface ISessionStore
	{
		/// <summary>
		/// Should return a previously stored session 
		/// for the given alias if available, otherwise null.
		/// </summary>
		SessionDelegate FindCompatibleSession(string alias);

		/// <summary>
		/// Should store the specified session instance 
		/// </summary>
		void Store(string alias, SessionDelegate session, out Action undoAction);

		/// <summary>
		/// Returns <c>true</c> if the current activity
		/// (which is an execution activity context) has no
		/// sessions available
		/// </summary>
		bool IsCurrentActivityEmptyFor(String alias);

//		/// <summary>
//		/// Should return a previously stored stateless session 
//		/// for the given alias if available, otherwise null.
//		/// </summary>
//		/// <param name="alias"></param>
//		/// <returns></returns>
//		StatelessSessionDelegate FindCompatibleStatelessSession(String alias);
//
//		/// <summary>
//		/// Should store the specified stateless session instance.
//		/// </summary>
//		/// <param name="alias"></param>
//		/// <param name="session"></param>
//		void Store(string alias, StatelessSessionDelegate session);
//
//		/// <summary>
//		/// Should remove the stateless session from the store only.
//		/// </summary>
//		/// <param name="session"></param>
//		void Remove(StatelessSessionDelegate session);

		void Dispose();

		int TotalStoredCurrent { get; }
	}
}
