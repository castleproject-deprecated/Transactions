namespace Castle.Services.Transaction
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Remoting.Messaging;

	public class TransactionCallContext : Dictionary<string, object>
	{
		public const string Key = "castle.tx.callctx";

		public TransactionCallContext()
		{
			Id = Guid.NewGuid();
		}

		public Guid Id { get; private set; }

		public static TransactionCallContext Get()
		{
			return CallContext.LogicalGetData(Key) as TransactionCallContext;
		}

		internal static TransactionCallContext TryInstall()
		{
			var context = Get();

			if (context == null)
			{
				context = new TransactionCallContext();
				CallContext.LogicalSetData(Key, context);
			}

			return context;
		}
	}
}