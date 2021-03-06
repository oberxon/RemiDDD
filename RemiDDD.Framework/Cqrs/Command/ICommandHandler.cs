﻿namespace RemiDDD.Framework.Cqrs
{
	public interface ICommandHandler<T> where T : ICommand
	{
		void Execute(T command);
	}
}