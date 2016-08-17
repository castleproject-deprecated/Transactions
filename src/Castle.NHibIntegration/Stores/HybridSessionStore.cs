namespace Castle.NHibIntegration.Internal
{
	using System.Collections.Generic;
	using System.Web;

	public class HybridSessionStore : WebSessionStore
	{
		private readonly AsyncLocalSessionStore localStore = new AsyncLocalSessionStore();

		internal override Dictionary<string, SessionDelegate> GetDictSession()
		{
			return ObtainSessionContext() != null ? base.GetDictSession() : localStore.GetDictSession();
		}

		internal override Dictionary<string, StatelessSessionDelegate> GetDictStatelessSession()
		{
			return ObtainSessionContext() != null ? base.GetDictStatelessSession() : localStore.GetDictStatelessSession();
		}

		internal override HttpContext ObtainSessionContext()
		{
			HttpContext curContext = HttpContext.Current;

			return curContext;
		}
	}
}