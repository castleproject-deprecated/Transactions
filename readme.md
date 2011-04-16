Documentation on [Wiki!](https://github.com/haf/Castle.Services.Transaction/wiki)

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


## Getting in Touch

If you have any questions, please send me an e-mail: [henrik@haf.se](mailto:henrik@haf.se) or ask at [Castle Project Users - Google Groups](http://groups.google.com/group/castle-project-users). As long as the projects are at RC-status I prefer if you e-mail me; it'll be a faster turn-around time and I get the e-mails straight to my inbox.

Cheers!

Henrik Feldt