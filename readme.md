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
