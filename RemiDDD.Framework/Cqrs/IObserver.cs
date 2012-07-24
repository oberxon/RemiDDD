namespace RemiDDD.Framework.Cqrs
{
	public interface IObserver<T> where T : IEvent
	{
		void ReactTo(T @event);
	}
}