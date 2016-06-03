namespace Castle.NHibIntegration.Tests
{
	using System;
	using System.Configuration;
	using System.Data.SqlClient;
	using Comps;
	using FluentAssertions;
	using MicroKernel.Registration;
	using NHibernate;
	using NUnit.Framework;
	using Oracle.ManagedDataAccess.Client;
	using Services.Transaction.Facility;
	using Windsor;

	public class BaseTest
	{
		protected WindsorContainer _container;
		protected ISessionStore _sessionStore;
		protected ISessionFactory _sessionFactoryOracle, _sessionFactoryMsSql;

		[SetUp]
		public void Init()
		{
			_container = new WindsorContainer();
			_container.AddFacility<AutoTx2Facility>();
			_container.AddFacility(new NhFacility(new MultipleFactoriesConfigBuilder()));

			_container.Register(
				Component.For<SvcWithTransactions>(),
				Component.For<SvcWithoutTransactions>(),
				Component.For<SvcAutoWithoutTransactions>(),
				Component.For<SvcAutoWithTransactions>()
			);

			_sessionStore = _container.Resolve<ISessionStore>();

			_sessionFactoryOracle = _container.Resolve<ISessionFactory>("sessfactory.oracle");
			_sessionFactoryMsSql = _container.Resolve<ISessionFactory>("sessfactory.mssql");

			var tran = System.Transactions.Transaction.Current;
			tran.Should().BeNull();

			var connStr = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
			using (var conn = new OracleConnection(connStr))
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "DELETE FROM TESTTABLE";
					cmd.ExecuteNonQuery();
				}
			}

			connStr = ConfigurationManager.ConnectionStrings["sqlserver"].ConnectionString;
			using (var conn = new SqlConnection(connStr))
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "DELETE FROM TESTTABLE2";
					cmd.ExecuteNonQuery();
				}
			}
		}

		protected int CountTestTableOracle()
		{
			var connStr = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
			using (var conn = new OracleConnection(connStr))
			{
				conn.Open();

				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "SELECT COUNT(*) FROM TESTTABLE";
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
			}
		}

		protected int CountTestTableMsSql()
		{
			var connStr = ConfigurationManager.ConnectionStrings["sqlserver"].ConnectionString;
			using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
			{
				conn.Open();

				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = "SELECT COUNT(*) FROM TESTTABLE2";
					return Convert.ToInt32(cmd.ExecuteScalar());
				}
			}
		}
	}
}