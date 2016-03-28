namespace Castle.Services.Transaction
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Linq;
	using System.Transactions;

	/// <summary>
	/// 	Specifies a method as transactional. When adding this interface to a method you can use an inversion of control container
	///		to intercept method calls to that method and perform the method transactionally. In the 'recommended' implementation,
	///		you can use Windsor and the AutoTx Facility for this. Just write <code>(:IWindsorContainer).AddFacility&lt;AutoTxFacility&gt;();</code>
	///		when you are registering your components.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class TransactionAttribute : Attribute
	{
		public TransactionAttribute() : this(TransactionScopeOption.Required, System.Transactions.IsolationLevel.ReadCommitted)
		{
		}

		public TransactionAttribute(TransactionScopeOption mode) : this(mode, System.Transactions.IsolationLevel.ReadCommitted)
		{
		}

		public TransactionAttribute(TransactionScopeOption mode, IsolationLevel isolationLevel)
		{
			Timeout = TimeSpan.Zero;
			Mode = mode;
			IsolationLevel = isolationLevel;
//			_CustomContext = new Dictionary<string, object>();
		}

		public IsolationLevel IsolationLevel { get; set; }
		public TransactionScopeOption Mode { get; set; }

//		public DependentCloneOption DependentOption { [Pure] get; set; }

		/// <summary>
		/// 	Gets or sets the transaction timeout. The timeout is often better
		/// 	implemented in the database, so this value is by default <see cref = "TimeSpan.MaxValue" />.
		/// </summary>
		public TimeSpan Timeout { [Pure] get; set; }

		public Castle.Services.Transaction.TransactionOptions ToOptions()
		{
			return new Castle.Services.Transaction.TransactionOptions
			{
				IsolationLevel = this.IsolationLevel,
				Timeout = this.Timeout, 
				Mode = this.Mode
			};
		}
	}
}
