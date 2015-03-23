using Castle.Transactions;

namespace Castle.Facilities.AutoTx.Tests.TestClasses
{
    public class InheritedMyService:MyService
    {
        public InheritedMyService(ITransactionManager manager) : base(manager)
        {
        }
    }
}