# Castle.Services.Transactions & Castle.Facilities.AutoTx

Castle Transactions enables

 * NTFS File Transactions (kernel transactions)
 * In memory (volatile) resource managers and transaction synchronizations.
 * Integration with Lightweight Transaction Managers

The .IO namespace of Castle Transactions enables:

 * A better tested Path util than what's in the .Net Framework.
 * A MapPath (as seen in ASP.Net) implementation.
 
Castle AutoTx enables (current development roadmap):

 * Easy transaction integration through inversion of control.
 * Easy retry-policies for dead connections or deadlocks or livelocks such as a contended lock causing victim database transactions.
 * Compensations when transactions die.

##Before release (3.0):

  * Integrate the file transactions into the nNextTransactions project.
  * Write a few more tests testing error conditions, including the repro on the (mailing list)[http://groups.google.com/group/castle-project-devel/browse_thread/thread/4ccc0fe4c6c12763] by John Surcombe.
  * Try to implement the retry policies
  * Try to implement InDoubt policies??
  * Give a few basic examples in this readme-file.
 
##vNext+1: 
  
  * Include migration-scaffolding-classes firing the same events from v2.1/2.5.
  * Implement async transactions being spawned through calls to ITxManager