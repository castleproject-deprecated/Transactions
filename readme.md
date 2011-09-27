Documentation on [Wiki!](https://github.com/haf/Castle.Services.Transaction/wiki)

# Getting Started

1. Download code
2. Run `rake -T` to browse tasks.

Either run a `rake`, and you quickly get a release-build, or set up a database `TxTests` and
a table Things as can be seen in the unit tests and then run `rake build_all` to get
both a release and a debug build.

The debug build has code contracts interwoven which will catch you out if you break the API
contracts. The release build is completely free from contracts. As such, I strongly
recommend that you develop towards the debug build.

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

***
 
## Castle AutoTx 3.0 enables the .Net coder with:

 * Easily applying transactions through inversion of control.
 * Easily applying Retry-Policies to transactional methods
 * Compensations when transactions abort.

## Contributing

 * `rake prepare`
 * Open `.sln` file in Visual Studio 2010.
 * Unit/integration test -> code -> fix test -> ...
 * `rake` or `rake test_all`
 * `git add . -A`, then `git commit -m "I improved something"`

## Getting in Touch

If you have any questions, please send me an e-mail: [henrik@haf.se](mailto:henrik@haf.se) or ask at [Castle Project Users - Google Groups](http://groups.google.com/group/castle-project-users). As long as the projects are at RC-status I prefer if you e-mail me; it'll be a faster turn-around time and I get the e-mails straight to my inbox.

Cheers!

Henrik Feldt / The Castle Project