namespace Castle.Services.Transaction
{
	public enum TransactionState
	{
		/// <summary>
		/// 	Initial state before c'tor run
		/// </summary>
		Default,

		/// <summary>
		/// 	When begin has been called and has returned.
		/// </summary>
		Active,

		/// <summary>
		/// 	When the transaction is in doubt. This occurs if e.g. the durable resource
		///		fails after Prepare but before the ACK for Commit has reached the application on
		///		which this transaction framework is running.
		/// </summary>
		InDoubt,

		/// <summary>
		/// 	When commit has been called and has returned successfully.
		/// </summary>
		CommittedOrCompleted,

		/// <summary>
		/// 	When first begin and then rollback has been called, or
		/// 	a resource failed.
		/// </summary>
		Aborted,

		/// <summary>
		/// 	When the dispose method has run.
		/// </summary>
		Disposed
	}
}