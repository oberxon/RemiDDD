using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Modules;
using Ninject.Extensions.Conventions;

namespace RemiDDD.Framework.Cqrs
{
	public class MessageProcessor
	{


		private CqsModule _module;
		private IKernel _kernel;
		public void Initialize(Assembly anAssembly)
		{
			var assemblyTypes = anAssembly
				.GetTypes();
			Initialize(assemblyTypes);
		}

		public void Initialize(IEnumerable<Type> assemblyTypes)
		{
			var commandType = typeof(ICommand);
			var commandTypes = assemblyTypes
				.Where(commandType.IsAssignableFrom)
				.ToList();
			_module = new CqsModule(assemblyTypes);
			_kernel = new StandardKernel(_module);
			

		}

		public class CqsModule : NinjectModule
		{
			private readonly IEnumerable<Type> _types;

			public CqsModule(IEnumerable<Type> types)
			{
				_types = types;
			}

			public override void Load()
			{
				InitCommandHandlers();
				InitObservers();
			}

			private void InitObservers()
			{
				var eventType = typeof (IEvent);
				var observerType = typeof (IObserver<>);
				var eventTypes = _types
					.Where(eventType.IsAssignableFrom)
					.ToList();
				foreach (var anEventType in eventTypes)
				{
					var fullyTypedObserver = observerType.MakeGenericType(anEventType);
					var observerTypes = _types.Where(fullyTypedObserver.IsAssignableFrom).ToList();
					foreach (var anObserverType in observerTypes)
					{
						Bind(fullyTypedObserver).To(anObserverType);
					}
				}
			}

			private void InitCommandHandlers()
			{
				var commandType = typeof(ICommand);
				var commandHandlerType = typeof(ICommandHandler<>);

				var commandTypes = _types
					.Where(commandType.IsAssignableFrom)
					.ToList();
				foreach (var aCommandType in commandTypes)
				{
					var fullyTypesHandler = commandHandlerType.MakeGenericType(aCommandType);
					var commandHandler = _types.Where(fullyTypesHandler.IsAssignableFrom).ToList();
					if (commandHandler.Count() == 0)
						throw new InvalidOperationException(string.Format("Command type not handled : {0}", commandHandler));
					if(commandHandler.Count() > 1)
						throw new InvalidOperationException(string.Format("Too much handler for {0}", commandHandler));
					Bind(fullyTypesHandler).To(commandHandler.First());
				}
			}
		}



		public void Execute<T>(T command) where T : ICommand
		{
			_kernel.Get<ICommandHandler<T>>().Execute(command);
		}


		public void Raise<T>(T @event) where T : IEvent
		{
			var observers = _kernel.GetAll<IObserver<T>>();
			foreach (var observer in observers)
			{
				observer.ReactTo(@event);
			}
		}
	}
}