namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Threading;
	using System.Threading.Tasks.Dataflow;
	using NUnit.Framework;


	[TestFixture, Explicit]
	public class AsynLocalExpTestCase
	{
		private ManualResetEvent _startCoordinatorEvent;
		private ActionBlock<string> _actionBlock;
		private AsyncLocal<string> _asyncLocal;

		[SetUp]
		public void Init()
		{
			_startCoordinatorEvent = new ManualResetEvent(false);
			_asyncLocal = new AsyncLocal<string>();
			_actionBlock = new ActionBlock<string>((Action<string>) OnAction1, new ExecutionDataflowBlockOptions());
		}

		[TearDown]
		public void End()
		{
			_startCoordinatorEvent.Dispose();
		}

		// [Test]
		public void EnsureAsyncLocalIsNotShared()
		{
			for (int i = 0; i < 4; i++)
			{
				if (i == 0)
					new Thread(StartNoWait) { IsBackground = true }.Start(i);
				else
					new Thread(StartWithWait) { IsBackground = true }.Start(i);
			}

			_startCoordinatorEvent.Set();

			Thread.Sleep(1000);
		}

		private void StartPostToActionBlock(object arg)
		{
			_asyncLocal.Value = _asyncLocal.Value ?? "root";

			_actionBlock.Post(arg.ToString());

			Thread.Sleep(1000);

			Console.WriteLine("back val " + arg + " = " + _asyncLocal.Value);
		}

		[Test]
		public void EnsureAsyncLocalIsNotPropagatedToActionBlock()
		{
			for (int i = 0; i < 4; i++)
			{
				new Thread(StartPostToActionBlock) { IsBackground = true }.Start(i);
			}

			Thread.Sleep(2000);

			Console.WriteLine("back val _ " + _asyncLocal.Value);
		}

		private void StartNoWait(object arg)
		{
			_asyncLocal.Value = arg.ToString();

			Console.WriteLine(arg + " _ " + _asyncLocal.Value);
		}

		private void StartWithWait(object arg)
		{
			_startCoordinatorEvent.WaitOne();

			Console.WriteLine(arg + " _ " + _asyncLocal.Value);
		}

		private void OnAction1(string message)
		{
			Console.WriteLine(message + " _ " + _asyncLocal.Value);

			_asyncLocal.Value = message;
		}
	}
}