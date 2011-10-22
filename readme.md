Documentation on [Wiki!](https://github.com/haf/Castle.Services.Transaction/wiki)

*Work in progress!*
**Version 3.0 beta 4**

## Castle Transactions

A project for transaction management on .Net and mono.

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