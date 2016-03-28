namespace Castle.Services.Transaction.Facility
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public interface ITransactionMetaInfoStore
	{
		TransactionalClassMetaInfo GetMetaFromType(Type implementation);

		IList<MethodInfo> SanityCheck(Type implementation);
	}

	public class TransactionClassMetaInfoStore : ITransactionMetaInfoStore
	{
		private readonly ConcurrentDictionary<Type, TransactionalClassMetaInfo> _type2Meta; 

		public TransactionClassMetaInfoStore()
		{
			_type2Meta = new ConcurrentDictionary<Type, TransactionalClassMetaInfo>(); 
		}

		public IList<MethodInfo> SanityCheck(Type implementation)
		{
			// return methods marked with TransactionAttribute and non virtual

			var problematicMethods =
				implementation
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(method => method.IsDefined(typeof(TransactionAttribute), true) && !method.IsVirtual)
					.ToList();

			return problematicMethods;
		}

		public TransactionalClassMetaInfo GetMetaFromType(Type implementation)
		{
			return _type2Meta.GetOrAdd(implementation, (t) =>
			{
				var transactionalMarkedMethods =
				implementation.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(method => method.IsDefined(typeof(TransactionAttribute), true))
					.Select(method => Tuple.Create(method, method.GetCustomAttribute<TransactionAttribute>(true).ToOptions()))
					.ToList();

				if (transactionalMarkedMethods.Any())
					return new TransactionalClassMetaInfo(transactionalMarkedMethods);

				return null;
			});
		}
	}
}