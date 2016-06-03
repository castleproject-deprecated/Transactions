namespace Castle.NHibIntegration.Tests.Comps
{
	using System;

	public class SvcAutoWithoutTransactions
	{
		private readonly ISessionManager _sessionManager;

		public SvcAutoWithoutTransactions(ISessionManager sessionManager)
		{
			_sessionManager = sessionManager;
		}

		[AutoCloseSession]
		public virtual void ExplicitSessionManagement()
		{
			using(var sess = _sessionManager.OpenSession())
			{
				Child1();

				var isOpen = sess.IsOpen;
			}
		}

		[AutoCloseSession]
		public virtual void NoExplicitSessionManagement()
		{
			var sess = _sessionManager.OpenSession();
			{
				Child1();

				var isOpen = sess.IsOpen;
			}
		}

		[AutoCloseSession]
		public virtual void NoExplicitSessionManagement_MultCall()
		{
			var sess = _sessionManager.OpenSession();
			{
				ChildWithAutoClose();
				ChildWithAutoClose();
				ChildWithAutoClose();

				var isOpen = sess.IsOpen;
			}
		}

		[AutoCloseSession]
		public virtual void ChildWithAutoClose()
		{
			var sess = _sessionManager.OpenSession();

			sess.Save(new TestTable { Id = Guid.NewGuid(), Counter = 1 });

			sess.Flush();
		}

		private void Child1()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child2();

				var isOpen = sess.IsOpen;
			}
		}

		private void Child2()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				var isOpen = sess.IsOpen;

				sess.Save(new TestTable { Id = Guid.NewGuid(), Counter = 1 });

				sess.Flush();
			}
		}
	}
}