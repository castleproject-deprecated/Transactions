using System;
using System.Collections.Generic;
using Castle.Transactions;
using NLog;

namespace Castle.Facilities.AutoTx.Tests.TestClasses
{
	/**
	 * Simplyfied version of ThreadedService from Castle.Facilities.NHibernate.Tests
	 */
	public class ThreadedService
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly List<Tuple<int, double>> _calculationsIds = new List<Tuple<int, double>>();

		public List<Tuple<int, double>> CalculationsIds
		{
			get { return _calculationsIds; }
		}

		[Transaction]
		public virtual void MainThreadedEntry()
		{
			logger.Debug("put some cores ({0}) to work!", Environment.ProcessorCount);

			for (var i = 0; i < Environment.ProcessorCount; i++)
				CalculatePi(i);
		}

		[Transaction(Fork = true)]
		protected virtual void CalculatePi(int i)
		{
			lock (_calculationsIds)
				_calculationsIds.Add(new Tuple<int, double>(i, 2*CalculatePiInner(1)));
		}

		protected double CalculatePiInner(int i)
		{
			if (i == 5000)
				return 1;

			return 1 + i/(2.0*i + 1)*CalculatePiInner(i + 1);
		}
	}
}