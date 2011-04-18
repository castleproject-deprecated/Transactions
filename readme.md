Documentation on [Wiki!](https://github.com/haf/Castle.Services.Transaction/wiki)

# Overview Transactions

Castle Transactions 3.0 enables the .Net coder with:

 * Transactional NTFS (TxF - File Transactions) with the KTM/Kernel Transaction Manager.
 * Integration with System.Transactions
 * 'Nice'/easy creation of CommittableTransaction/DependentTransaction that exposes more features than TransactionScope.
 * Retry policies for transactions

The .IO namespace of Castle Transactions enables:

 * A better tested Path util than what's in the .Net Framework.
 * A MapPath-implementation (as seen in ASP.Net).
 * Full directory/file name length support. No more "PathTooLongException" or borked install or build scripts.

# Roadmap

Castle.Transactions 3.1 will enable:

 * Transactional Registry - Managed API (TxR - Registry Transactions)
 * Full support for all transacted file methods in the Windows kernel:
   * CopyFileTransacted
   * CreateDirectoryTransacted
   * CreateFileTransacted
   * CreateHardLinkTransacted
   * CreateSymbolicLinkTransacted
   * DeleteFileTransacted
   * FindFirstFileNameTransactedW
   * FindFirstFileTransacted
   * FindFirstStreamTransactedW
   * GetCompressedFileSizeTransacted
   * GetFileAttributesTransacted
   * GetFullPathNameTransacted
   * GetLongPathNameTransacted
   * MoveFileTransacted
   * RemoveDirectoryTransacted
   * SetFileAttributesTransacted

***
 
## Castle AutoTx 3.0 enables the .Net coder with:

 * Easily applying transactions through inversion of control.
 * Easily applying Retry-Policies to transactional methods
 * Compensations when transactions abort.

## Getting in Touch

If you have any questions, please send me an e-mail: [henrik@haf.se](mailto:henrik@haf.se) or ask at [Castle Project Users - Google Groups](http://groups.google.com/group/castle-project-users). As long as the projects are at RC-status I prefer if you e-mail me; it'll be a faster turn-around time and I get the e-mails straight to my inbox.

Cheers!

Henrik Feldt / The Castle Project