namespace Castle.Services.Transaction2.Facility
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Transaction;

	public sealed class TransactionalClassMetaInfo
	{
		private readonly Dictionary<string, TransactionOptions> _method2TransactionOpts;

		public TransactionalClassMetaInfo(IList<Tuple<MethodInfo, TransactionOptions>> methods)
		{
			_method2TransactionOpts = new Dictionary<string, TransactionOptions>(StringComparer.Ordinal);

			foreach (var tuple in methods)
			{
				_method2TransactionOpts[tuple.Item1.Name] = tuple.Item2;
			}
		}

		/// <summary>
		/// 	Gets the maybe transaction options for the method info, target. If the target
		/// 	has not been associated with a tranaction, the maybe is none.
		/// </summary>
		/// <param name = "target">Method to find the options for.</param>
		/// <returns>A non-null maybe <see cref = "ITransactionOptions" />.</returns>
		public TransactionOptions AsTransactional(MethodInfo target)
		{
			TransactionOptions att;
			_method2TransactionOpts.TryGetValue(target.Name, out att);
			return att;
		}
	}
}