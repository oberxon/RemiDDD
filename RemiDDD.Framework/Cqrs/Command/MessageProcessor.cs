using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Modules;
using Ninject.Extensions.Conventions;
using Ninject.Parameters;
using Ninject.Planning.Bindings;
using RemiDDD.Framework.Cqrs.Command;

namespace RemiDDD.Framework.Cqrs
{
	/// <summary>
	/// This class will handle the binding between the commands and their handlers and between the events and their observers
	/// </summary>
	public class MessageProcessor
	{

		private IKernel _kernel;
		private readonly Dictionary<ICommand, Dictionary<Type, List<Action<IEvent>>>> _OnTheFlyObserver;

		public MessageProcessor(IKernel kernel)
		{
			_kernel = kernel;
			_OnTheFlyObserver = new Dictionary<ICommand, Dictionary<Type, List<Action<IEvent>>>>();
		}
		/// <summary>
		/// Will bind the command with their handlers and the event with their observers for all the types in these assembly
		/// WARNING : you have to have 1 and only 1 handler for each command (not 0, not 2, 1 !)
		/// </summary>
		/// <param name="anAssembly">The assemblies containing the types</param>
		public void Initialize(IEnumerable<Assembly> anAssembly)
		{
			var assemblyTypes = anAssembly
				.SelectMany(a => a.GetTypes());
			Initialize(assemblyTypes);
		}
		/// <summary>
		/// Will bind the command with their handlers and the event with their observers for all the types in this list
		/// WARNING : you have to have 1 and only 1 handler for each command (not 0, not 2, 1 !)
		/// </summary>
		/// <param name="assemblyTypes">All the types you wan't to process</param>
		public void Initialize(IEnumerable<Type> assemblyTypes)
		{
			InitObservers(assemblyTypes);
			InitCommandHandlers(assemblyTypes);
		}


		private void InitObservers(IEnumerable<Type> assemblyTypes)
		{
			var eventType = typeof(IEvent);
			var observerType = typeof(IObserver<>);
			var eventTypes = assemblyTypes
				.Where(eventType.IsAssignableFrom)
				.ToList();
			foreach (var anEventType in eventTypes)
			{
				var fullyTypedObserver = observerType.MakeGenericType(anEventType);
				var observerTypes = assemblyTypes.Where(fullyTypedObserver.IsAssignableFrom).ToList();
				foreach (var anObserverType in observerTypes)
				{
					_kernel.Bind(fullyTypedObserver).To(anObserverType);
				}
			}
		}

		private void InitCommandHandlers(IEnumerable<Type> assemblyTypes)
		{
			var commandType = typeof(ICommand);
			var commandHandlerType = typeof(ICommandHandler<>);

			var commandTypes = assemblyTypes
				.Where(commandType.IsAssignableFrom)
				.Where(t => t != commandType)
				.ToList();
			foreach (var aCommandType in commandTypes)
			{
				var fullyTypesHandler = commandHandlerType.MakeGenericType(aCommandType);
				var commandHandler = assemblyTypes.Where(fullyTypesHandler.IsAssignableFrom).ToList();
				if (commandHandler.Count() == 0)
					throw new InvalidOperationException(string.Format("Command type not handled : {0}", aCommandType));
				if (commandHandler.Count() > 1)
					throw new InvalidOperationException(string.Format("Too much handler for {0}", aCommandType));
				_kernel.Bind(fullyTypesHandler).To(commandHandler.First());
			}

		}

		/// <summary>
		/// Will call the ICommandHandler for this type of command
		/// </summary>
		/// <typeparam name="T">Type Of command</typeparam>
		/// <param name="command">Instance of command</param>
		public void Execute<T>(T command) where T : ICommand
		{
			_kernel
				.Get<ICommandHandler<T>>()
				.Execute(command);
			StopObserving(command);
			Raise(new CommandExecuted(){Command = command},command);
		}

		/// <summary>
		/// When the command raise an event of type IEvent it'll call action with the instance as parameter, you can add more than one action 
		/// for a same event and a same source.
		/// The event will stop being observed after the command is process (at the end of Execute)
		/// </summary>
		/// <typeparam name="TEvent">Type of event you want to observe</typeparam>
		/// <param name="source">The command that'll raise the event</param>
		/// <param name="action">The action that'll handle the event</param>
		public void Observe<TEvent>(ICommand source, Action<TEvent> action) where TEvent : IEvent
		{
			//this might be doable with ninject but I can't figure a way out, and this is not that much ugly
			Dictionary<Type, List<Action<IEvent>>> eventsForCommand;
			if (!_OnTheFlyObserver.TryGetValue(source, out eventsForCommand))
			{
				eventsForCommand = new Dictionary<Type, List<Action<IEvent>>>();
				_OnTheFlyObserver.Add(source, eventsForCommand);
			}
			List<Action<IEvent>> actionsForEvent;
			Type eventType = typeof(TEvent);
			if (!eventsForCommand.TryGetValue(eventType, out actionsForEvent))
			{
				actionsForEvent = new List<Action<IEvent>>();
				eventsForCommand.Add(eventType, actionsForEvent);
			}
			actionsForEvent.Add((e) => action((TEvent)e));
		}
		private void StopObserving(ICommand source)
		{
			if (_OnTheFlyObserver.ContainsKey(source))
				_OnTheFlyObserver.Remove(source);
		}
		/// <summary>
		/// Will call all the observers observing this type of event
		/// </summary>
		/// <typeparam name="T">Type of event</typeparam>
		/// <param name="event">Instance of event</param>
		/// <param name="source">Source command</param>
		public void Raise<T>(T @event, ICommand source) where T : IEvent
		{

			var observers = _kernel.GetAll<IObserver<T>>();
			foreach (var observer in observers)
			{
				observer.ReactTo(@event);
			}
			if (source == null)
				return;
			Dictionary<Type, List<Action<IEvent>>> commandEvents;
			List<Action<IEvent>> actionsForEvent;
			if (_OnTheFlyObserver.TryGetValue(source, out commandEvents) && commandEvents.TryGetValue(typeof(T), out actionsForEvent))
			{
				foreach (var action in actionsForEvent)
				{
					action(@event);
				}
			}
			if(!(@event is EventRaised))
			{
				Raise(
					new EventRaised()
					      {
						      Event = @event
					      },
					source);
			}
		}
		/// <summary>
		/// Will call all the observers observing this type of event
		/// </summary>
		/// <typeparam name="T">Type of event</typeparam>
		/// <param name="event">Instance of event</param>
		public void Raise<T>(T @event) where T : IEvent
		{
			Raise<T>(@event, null);
		}
	}
}