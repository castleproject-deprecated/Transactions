namespace Castle.NHibIntegration.Tests
{
	using System;


	public class TestTable
	{
		public virtual Guid Id { get; set; }
		public virtual int? Counter { get; set; }
	}

	public class TestTable2
	{
		public virtual Guid Id { get; set; }
		public virtual int? Counter { get; set; }
	}

	public class TestTableMap : FluentNHibernate.Mapping.ClassMap<TestTable>
	{
		public TestTableMap()
		{
			Table("TestTable");

			this.Not.LazyLoad();

			Id(a => a.Id).GeneratedBy.Assigned();

			Map(a => a.Counter);
		}
	}

	public class TestTable2Map : FluentNHibernate.Mapping.ClassMap<TestTable2>
	{
		public TestTable2Map()
		{
			Table("TestTable2");

			this.Not.LazyLoad();

			Id(a => a.Id).GeneratedBy.Assigned();

			Map(a => a.Counter);
		}
	}
}