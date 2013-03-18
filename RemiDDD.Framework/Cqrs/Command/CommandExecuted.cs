using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemiDDD.Framework.Cqrs.Command
{
	public class CommandExecuted : IEvent
	{
		public ICommand Command { get; set; }
	}
}
