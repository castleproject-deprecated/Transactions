namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using NUnit.Framework;


	public class MutableThing
	{
		public List<string> Children;

		public MutableThing()
		{
			Children = new List<string>();
		}

		public override string ToString()
		{
			return "MutableThing with " + Children.Count + " children";
		}
	}

	[TestFixture, Explicit]
	public class AsynLocalExpTestCase
	{
		private ManualResetEvent _startCoordinatorEvent;
		private ActionBlock<string> _actionBlock;
		private AsyncLocal<MutableThing> _asyncLocal;

		[SetUp]
		public void Init()
		{
			_startCoordinatorEvent = new ManualResetEvent(false);
			_asyncLocal = new AsyncLocal<MutableThing>();
			_actionBlock = new ActionBlock<string>((Action<string>) OnAction1, new ExecutionDataflowBlockOptions());
		}

		[TearDown]
		public void End()
		{
			_startCoordinatorEvent.Dispose();
		}

		// [Test]
//		public void EnsureAsyncLocalIsNotShared()
//		{
//			for (int i = 0; i < 4; i++)
//			{
//				if (i == 0)
//					new Thread(StartNoWait) { IsBackground = true }.Start(i);
//				else
//					new Thread(StartWithWait) { IsBackground = true }.Start(i);
//			}
//
//			_startCoordinatorEvent.Set();
//
//			Thread.Sleep(1000);
//		}

		[Test]
		public void EnsureAsyncLocalIsNotPropagatedToActionBlock()
		{
			_asyncLocal.Value = new MutableThing();

			_actionBlock.Post("1");
			_actionBlock.Post("2");

			for (int i = 0; i < 4; i++)
			{
				// new Thread(StartPostToActionBlock) { IsBackground = true }.Start(i);
				Task.Factory.StartNew(StartPostToActionBlock, i);
			}

			Thread.Sleep(2000);

			Console.WriteLine("back root _ " + _asyncLocal.Value.Children.Aggregate("", (acc, item) => acc + " " + item));
		}

		private void StartPostToActionBlock(object arg)
		{
			_asyncLocal.Value = new MutableThing();

			_actionBlock.Post("FT " + arg);

			Thread.Sleep(1000);

			Console.WriteLine("back FT val " + arg + " = " + _asyncLocal.Value.Children.Aggregate("", (acc, item) => acc + " " + item));
		}

		private void OnAction1(string message)
		{
			Console.WriteLine("AB " + message + " _ " + _asyncLocal.Value);

			_asyncLocal.Value.Children.Add(message);
		}

//		private void StartNoWait(object arg)
//		{
//			_asyncLocal.Value = arg.ToString();
//
//			Console.WriteLine(arg + " _ " + _asyncLocal.Value);
//		}
//
//		private void StartWithWait(object arg)
//		{
//			_startCoordinatorEvent.WaitOne();
//
//			Console.WriteLine(arg + " _ " + _asyncLocal.Value);
//		}
	}
}