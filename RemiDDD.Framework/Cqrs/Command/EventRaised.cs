using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemiDDD.Framework.Cqrs.Command
{
	public class EventRaised : IEvent
	{
		public IEvent Event { get; set; }
	}
}
