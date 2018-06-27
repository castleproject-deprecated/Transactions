# Castle Transactions

A project for transaction management on .NET Standard 2.0

## Releases

See the [releases](https://github.com/castleproject/Castle.Transactions/releases).

## License

Castle Transactions is &copy; 2004-2018 Castle Project. It is free software, and may be redistributed under the terms of the [Apache 2.0](http://opensource.org/licenses/Apache-2.0) license.

## Building

```
build.cmd
```

## Quick Start

You have a few major options. The first option is to install the Windsor integration:

`install-package Castle.Facilities.AutoTx`,

 - -> Castle.Transactions
 - -> Castle.Windsor

another option is that you only care about the transactions API as a stand-alone:

`install-package Castle.Transactions` -> Castle.Core

another option is that you care about the transactions API + transactional NTFS:

`install-package Castle.Transactions.IO`

 - -> Castle.Transactions
 - -> Castle.IO
 - -> Castle.Core

### Castle Transactions

The original project that manages transactions.

#### Main Features

 * Regular Transactions (+`System.Transactions` interop) - allows you to create transactions with a nice API
 * Dependent Transactions - allows you to fork dependent transactions automatically by declarative programming: `[Transaction(Fork=true)]`
 * Transaction Logging - A trace listener in namespace `Castle.Transactions.Logging`, named `TraceListener`.
 * Retry policies for transactions

#### Main Interfaces

 - `ITransactionManager`:
   - *default implementation is `TransactionManager`*
   - keeps tabs on what transaction is currently active
   - coordinates parallel dependent transactions
   - keep the light weight transaction manager (LTM) happy on the CLR

### Castle Transactions IO

A project for adding a transactional file system to the mix!

#### Main Features

 * Provides an `Castle.IO.IFileSystem` implementation that adds transactionality to common operations.
 