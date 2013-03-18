using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using RemiDDD.Framework.Cqrs;

namespace RemidDDD.Framework.Test
{
	[TestFixture]
	public class CommandProcessorTest
	{
		[Test]
		public void RouteToCommandHandlerWhenPresent()
		{

			var commandProcessor = new MessageProcessor();
			var fakeCommand = new FakeCommand();
			commandProcessor.Initialize(new[]
			                            	{
			                            		typeof(FakeCommandHandler), 
												fakeCommand.GetType()
			                            	});
			commandProcessor.Execute(fakeCommand);
			Assert.IsTrue(fakeCommand.Executed);
		}
		[Test]
		public void RaiseErrorWhenNoHandler()
		{

			var commandProcessor = new MessageProcessor();
			var fakeCommand = new FakeCommand();
			var otherFakeCommand = new OtherFakeCommand();
			Assert.Throws<InvalidOperationException>(() => commandProcessor.Initialize(new[] { typeof(FakeCommandHandler), fakeCommand.GetType(), otherFakeCommand.GetType() }));

		}
		[Test]
		public void RaiseErrorWhenMoreThanOneHandler()
		{
			var commandProcessor = new MessageProcessor();
			var fakeCommand = new FakeCommand();
			Assert.Throws<InvalidOperationException>(() => commandProcessor.Initialize(new[] { typeof(FakeCommandHandler), typeof(OtherFakeCommandHandler), fakeCommand.GetType() }));
		}
		[Test]
		public void ObserveEventInMultipleObserver()
		{
			var commandProcessor = new MessageProcessor();
			commandProcessor.Initialize(new[] { typeof(FakeEvent), typeof(FakeEventObserver), typeof(OtherFakeEventObserver) });
			var fakeEvent = new FakeEvent();
			commandProcessor.Raise(fakeEvent);
			Assert.AreEqual(2,fakeEvent.Handled);
		}
		[Test]
		public void DontDoNothinIfNoObserver()
		{
			var commandProcessor = new MessageProcessor();
			commandProcessor.Initialize(new[] { typeof(FakeEvent) });
			var fakeEvent = new FakeEvent();
			commandProcessor.Raise(fakeEvent);
			Assert.AreEqual(0, fakeEvent.Handled);
		}

		[Test]
		public void OnTheFlyActionCalled()
		{
			var commandProcessor = new MessageProcessor();
			commandProcessor.Initialize(new[] { typeof(FakeEvent), typeof(FakeCommand), typeof(FakeCommandHandlerRaisingEvent) });
			FakeCommand command = new FakeCommand(){TestId = 14};
			int testId = 0;
			commandProcessor.Observe<FakeEvent>(command, e => testId = e.TestId);
			commandProcessor.Execute(command);
			Assert.AreEqual(command.TestId,testId);
		}
	}
	public class FakeEvent : IEvent
	{
		public int Handled { get; set; }

		public int TestId { get; set; }
	}
	public class FakeEventObserver : RemiDDD.Framework.Cqrs.IObserver<FakeEvent>
	{


		public void ReactTo(FakeEvent @event)
		{
			@event.Handled++;
		}
	}
	public class OtherFakeEventObserver : RemiDDD.Framework.Cqrs.IObserver<FakeEvent>
	{


		public void ReactTo(FakeEvent @event)
		{
			@event.Handled++;
		}
	}
	public class FakeCommandHandler : ICommandHandler<FakeCommand>
	{
		
		public FakeCommandHandler()
		{

		}

		public void Execute(FakeCommand command)
		{
			command.Execute();
		}
	}
	public class FakeCommandHandlerRaisingEvent : ICommandHandler<FakeCommand>
	{
		private readonly MessageProcessor _messageProcessor;

		public FakeCommandHandlerRaisingEvent(MessageProcessor messageProcessor)
		{
			_messageProcessor = messageProcessor;
		}

		public void Execute(FakeCommand command)
		{
			_messageProcessor.Raise(
				new FakeEvent()
			        {
			            TestId = command.TestId
			        },
			command);
			command.Execute();
		}
	}
	public class OtherFakeCommandHandler : ICommandHandler<FakeCommand>
	{

		public OtherFakeCommandHandler()
		{

		}

		public void Execute(FakeCommand command)
		{
			command.Execute();
		}
	}

	public class OtherFakeCommand : ICommand
	{

	}
	public class FakeCommand : ICommand
	{
		public void Execute()
		{
			Executed = true;
		}

		public int TestId { get; set; }

		public bool Executed { get; set; }
	}
}
