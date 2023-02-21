namespace Ecng.Collections;

public interface IQueue<T>
{
	T Dequeue();
	T Peek();
	void Enqueue(T item);
}