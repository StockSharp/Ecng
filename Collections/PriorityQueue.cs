namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Represents a min priority queue.
/// </summary>
/// <typeparam name="TElement">Specifies the type of elements in the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the type of priority associated with enqueued elements.</typeparam>
/// <remarks>
/// Implements an array-backed quaternary min-heap. Each element is enqueued with an associated priority
/// that determines the dequeue order: elements with the lowest priority get dequeued first.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
/// with the specified custom priority comparer.
/// </remarks>
/// <param name="subtractAbs">The function that calculates the absolute difference between two priorities.</param>
/// <param name="comparer">
/// Custom comparer dictating the ordering of elements.
/// Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
/// </param>
[DebuggerDisplay("Count = {Count}")]
public class PriorityQueue<TPriority, TElement>(Func<TPriority, TPriority, TPriority> subtractAbs, IComparer<TPriority> comparer) : ICollection<(TPriority, TElement)>, IQueue<(TPriority priority, TElement element)>
{
	private class Node(TPriority priority) : IEnumerable<TElement>
	{
		private TElement _element;
		private Queue<TElement> _elements;

		public Node(TPriority priority, TElement element)
			: this(priority)
		{
			_element = element;
			HasData = true;
		}

		public Node(TPriority priority, IEnumerable<TElement> elements)
			: this(priority)
		{
			_elements = new(elements);
			HasData = true;
		}

		public bool HasData;
		public readonly TPriority Priority = priority;

		private void CheckData()
		{
			if (!HasData)
				throw new InvalidOperationException("HasData == false");
		}

		public TElement Dequeue()
		{
			CheckData();

			if (_elements is null)
			{
				HasData = false;
				return _element;
			}
			else
			{
				var element = _elements.Dequeue();
				HasData = _elements.Count > 0;
				return element;
			}
		}

		public TElement Peek()
		{
			CheckData();

			return _elements is null ? _element : _elements.Peek();
		}

		public void Enqueue(TElement[] elements)
		{
			if (elements is null)
				throw new ArgumentNullException(nameof(elements));

			if (HasData)
			{
				if (_elements is null)
				{
					_elements = new();
					_elements.Enqueue(_element);
					_element = default;
				}

				foreach (var element in elements)
					_elements.Enqueue(element);
			}
			else
			{
				if (elements.Length == 1)
					_element = elements[0];
				else
					_elements = new(elements);

				HasData = true;
			}
		}

		public void Enqueue(TElement element)
		{
			if (HasData)
			{
				if (_elements is null)
				{
					_elements = new();
					_elements.Enqueue(_element);
					_element = default;
				}

				_elements.Enqueue(element);
			}
			else
			{
				_element = element;
				HasData = true;
			}
		}

		public IEnumerator<TElement> GetEnumerator()
		{
			IEnumerable<TElement> source = HasData ? (_elements is null ? new[] { _element } : _elements) : Array.Empty<TElement>();
			return source.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString()
			=> $"Pr={Priority} Elem={(HasData ? (_elements is null ? _element : _elements.Count) : null)}";
	}

	private readonly LinkedList<Node> _nodes = new();

	/// <summary>
	/// Version updated on mutation to help validate enumerators operate on a consistent state.
	/// </summary>
	private int _version;

	private readonly Func<TPriority, TPriority, TPriority> _subtractAbs = subtractAbs ?? throw new ArgumentNullException(nameof(subtractAbs));

	/// <summary>
	/// Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class.
	/// </summary>
	public PriorityQueue(Func<TPriority, TPriority, TPriority> subtractAbs)
		: this(subtractAbs, Comparer<TPriority>.Default)
	{
	}

	private int _count;

	/// <summary>
	/// Gets the number of elements contained in the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public int Count => _count;

	private readonly IComparer<TPriority> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

	/// <summary>
	/// Gets the priority comparer used by the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public IComparer<TPriority> Comparer => _comparer;

	private void CheckCount()
	{
		if (_count == 0)
		{
			throw new InvalidOperationException("Empty queue.");
		}
	}

	private void EnqueueInternal(TPriority priority, TElement element)
	{
		if (_count == 0)
		{
			_nodes.AddFirst(new Node(priority, element));
		}
		else
		{
			var best = _nodes.First.Value;
			var worst = _nodes.Last.Value;

			var nearestRes = _comparer.Compare(_subtractAbs(best.Priority, priority), _subtractAbs(worst.Priority, priority));

			var walkToBest = true;

			if (nearestRes > 0)
			{
				// closer to worst
			}
			else if (nearestRes < 0)
			{
				// closer to best

				walkToBest = false;
			}
			else if (nearestRes == 0)
			{
				// worst and best are same
			}

			if (walkToBest)
			{
				var res = _comparer.Compare(priority, worst.Priority);

				if (res > 0)
				{
					_nodes.AddLast(new Node(priority, element));
				}
				else if (res == 0)
				{
					worst.Enqueue(element);
				}
				else
				{
					var curr = _nodes.Last.Previous;

					if (curr == null)
					{
						_nodes.AddBefore(_nodes.Last, new Node(priority, element));
					}
					else
					{
						while (true)
						{
							res = _comparer.Compare(priority, curr.Value.Priority);

							if (res > 0)
							{
								_nodes.AddAfter(curr, new Node(priority, element));
								break;
							}
							else if (res == 0)
							{
								curr.Value.Enqueue(element);
								break;
							}
							else
							{
								curr = curr.Previous;

								if (curr is null)
								{
									_nodes.AddFirst(new Node(priority, element));
									break;
								}
							}
						}
					}
				}
			}
			else
			{
				var res = _comparer.Compare(priority, best.Priority);

				if (res < 0)
				{
					_nodes.AddFirst(new Node(priority, element));
				}
				else if (res == 0)
				{
					best.Enqueue(element);
				}
				else
				{
					var curr = _nodes.First.Next;

					if (curr == null)
					{
						_nodes.AddAfter(_nodes.First, new Node(priority, element));
					}
					else
					{
						while (true)
						{
							res = _comparer.Compare(priority, curr.Value.Priority);

							if (res < 0)
							{
								_nodes.AddBefore(curr, new Node(priority, element));
								break;
							}
							else if (res == 0)
							{
								curr.Value.Enqueue(element);
								break;
							}
							else
							{
								curr = curr.Next;

								if (curr is null)
								{
									_nodes.AddLast(new Node(priority, element));
									break;
								}
							}
						}
					}
				}
			}
		}
	}

	private (TPriority priority, TElement element) DequeueInternal()
	{
		var best = _nodes.First.Value;

		var element = best.Dequeue();

		if (!best.HasData)
			_nodes.RemoveFirst();

		return (best.Priority, element);
	}

	/// <summary>
	/// Adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	public void Enqueue(TPriority priority, TElement element)
	{
		// Virtually add the node at the end of the underlying array.
		// Note that the node being enqueued does not need to be physically placed
		// there at this point, as such an assignment would be redundant.

		EnqueueInternal(priority, element);

		_version++;
		_count++;
	}

	/// <summary>
	/// Returns the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/> without removing it.
	/// </summary>
	/// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{TPriority, TElement}"/> is empty.</exception>
	/// <returns>The minimal element of the <see cref="PriorityQueue{TPriority, TElement}"/>.</returns>
	public (TPriority priority, TElement element) Peek()
	{
		CheckCount();

		var best = _nodes.First.Value;
		return (best.Priority, best.Peek());
	}

	/// <summary>
	/// Removes and returns the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">The queue is empty.</exception>
	/// <returns>The minimal element of the <see cref="PriorityQueue{TPriority, TElement}"/>.</returns>
	public (TPriority priority, TElement element) Dequeue()
	{
		CheckCount();

		var t = DequeueInternal();

		_count--;
		_version++;

		return t;
	}

	/// <summary>
	/// Removes the minimal element and then immediately adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	/// <exception cref="InvalidOperationException">The queue is empty.</exception>
	/// <returns>The minimal element removed before performing the enqueue operation.</returns>
	/// <remarks>
	/// Implements an extract-then-insert heap operation that is generally more efficient
	/// than sequencing Dequeue and Enqueue operations: in the worst case scenario only one
	/// shift-down operation is required.
	/// </remarks>
	public TElement DequeueEnqueue(TPriority priority, TElement element)
	{
		CheckCount();

		var best = _nodes.First.Value;
		var retVal = best.Dequeue();

		if (!best.HasData)
			_nodes.RemoveFirst();

		EnqueueInternal(priority, element);

		_version++;
		return retVal;
	}

	/// <summary>
	/// Removes the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// and copies it to the <paramref name="element"/> parameter,
	/// and its associated priority to the <paramref name="priority"/> parameter.
	/// </summary>
	/// <param name="element">The removed element.</param>
	/// <param name="priority">The priority associated with the removed element.</param>
	/// <returns>
	/// <see langword="true"/> if the element is successfully removed;
	/// <see langword="false"/> if the <see cref="PriorityQueue{TPriority, TElement}"/> is empty.
	/// </returns>
	public bool TryDequeue(out TElement element, out TPriority priority)
	{
		if (_count != 0)
		{
			(priority, element) = Dequeue();
			return true;
		}

		element = default;
		priority = default;
		return false;
	}

	/// <summary>
	/// Returns a value that indicates whether there is a minimal element in the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// and if one is present, copies it to the <paramref name="element"/> parameter,
	/// and its associated priority to the <paramref name="priority"/> parameter.
	/// The element is not removed from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="element">The minimal element in the queue.</param>
	/// <param name="priority">The priority associated with the minimal element.</param>
	/// <returns>
	/// <see langword="true"/> if there is a minimal element;
	/// <see langword="false"/> if the <see cref="PriorityQueue{TPriority, TElement}"/> is empty.
	/// </returns>
	public bool TryPeek(out TElement element, out TPriority priority)
	{
		if (_count != 0)
		{
			(priority, element) = Peek();
			return true;
		}

		element = default;
		priority = default;
		return false;
	}

	/// <summary>
	/// Adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// and immediately removes the minimal element, returning the result.
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	/// <returns>The minimal element removed after the enqueue operation.</returns>
	/// <remarks>
	/// Implements an insert-then-extract heap operation that is generally more efficient
	/// than sequencing Enqueue and Dequeue operations: in the worst case scenario only one
	/// shift-down operation is required.
	/// </remarks>
	public TElement EnqueueDequeue(TPriority priority, TElement element)
	{
		if (_count == 0)
			return element;

		EnqueueInternal(priority, element);

		var elem = DequeueInternal().element;

		_version++;

		return elem;
	}

	/// <summary>
	/// Enqueues a sequence of element/priority pairs to the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="items">The pairs of elements and priorities to add to the queue.</param>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="items"/> argument was <see langword="null"/>.
	/// </exception>
	public void EnqueueRange(IEnumerable<(TPriority priority, TElement element)> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		foreach (var g in items.GroupBy(i => i.priority).OrderBy(g => g.Key, _comparer))
			EnqueueRange(g.Key, g.Select(t => t.element));
	}

	/// <summary>
	/// Enqueues a sequence of elements pairs to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// all associated with the specified priority.
	/// </summary>
	/// <param name="elements">The elements to add to the queue.</param>
	/// <param name="priority">The priority to associate with the new elements.</param>
	/// <exception cref="ArgumentNullException">
	/// The specified <paramref name="elements"/> argument was <see langword="null"/>.
	/// </exception>
	public void EnqueueRange(TPriority priority, IEnumerable<TElement> elements)
	{
		var arr = elements.ToArray();

		if (arr.Length == 0)
			return;

		if (_count == 0)
		{
			_nodes.AddFirst(new Node(priority, arr));
		}
		else
		{
			var worst = _nodes.Last.Value;

			var res = _comparer.Compare(priority, worst.Priority);

			if (res > 0)
			{
				_nodes.AddLast(new Node(priority, arr));
			}
			else if (res == 0)
			{
				worst.Enqueue(arr);
			}
			else
			{
				var curr = _nodes.Last.Previous;

				if (curr == null)
				{
					_nodes.AddBefore(_nodes.Last, new Node(priority, arr));
				}
				else
				{
					while (true)
					{
						res = _comparer.Compare(priority, curr.Value.Priority);

						if (res > 0)
						{
							_nodes.AddAfter(curr, new Node(priority, arr));
							break;
						}
						else if (res == 0)
						{
							curr.Value.Enqueue(arr);
							break;
						}
						else
						{
							curr = curr.Previous;

							if (curr is null)
							{
								_nodes.AddFirst(new Node(priority, arr));
								break;
							}
						}
					}
				}
			}
		}

		_version++;
		_count += arr.Length;
	}

	/// <summary>
	/// Removes all items from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public void Clear()
	{
		_nodes.Clear();
		_count = 0;
		_version++;
	}

	private class Enumerator : Disposable, IEnumerator<(TPriority, TElement)>
	{
		private readonly PriorityQueue<TPriority, TElement> _queue;
		private LinkedListNode<Node> _current;
		private IEnumerator<TElement> _currentEnum;
		private readonly int _currVer;

		public Enumerator(PriorityQueue<TPriority, TElement> queue)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_currVer = _queue._version;
		}

		public (TPriority, TElement) Current => (_current.Value.Priority, _currentEnum.Current);
		object IEnumerator.Current => Current;

		bool IEnumerator.MoveNext()
		{
			if (_currVer != _queue._version)
				throw new InvalidOperationException("Collection was modified.");

			if (_currentEnum is null)
			{
				_current = _queue._nodes.First;

				if (_current is null)
					return false;

				_currentEnum = _current.Value.GetEnumerator();
			}

			if (!_currentEnum.MoveNext())
			{
				_current = _current.Next;

				if (_current is null)
					return false;

				_currentEnum.Dispose();
				_currentEnum = _current.Value.GetEnumerator();

				if (!_currentEnum.MoveNext())
					return false;
			}

			return true;
		}

		void IEnumerator.Reset()
		{
			_current = null;
			_currentEnum?.Dispose();
			_currentEnum = null;
		}
	}

	bool ICollection<(TPriority, TElement)>.IsReadOnly => false;
	void ICollection<(TPriority, TElement)>.Add((TPriority, TElement) item) => Enqueue(item.Item1, item.Item2);

	bool ICollection<(TPriority, TElement)>.Contains((TPriority, TElement) item) => throw new NotSupportedException();
	void ICollection<(TPriority, TElement)>.CopyTo((TPriority, TElement)[] array, int arrayIndex)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		if (arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		if (array.Length - arrayIndex < _count)
			throw new ArgumentException("Destination array is not long enough.");

		var index = arrayIndex;

		foreach (var node in _nodes)
		{
			foreach (var element in node)
			{
				array[index++] = (node.Priority, element);
			}
		}
	}
	bool ICollection<(TPriority, TElement)>.Remove((TPriority, TElement) item) => throw new NotSupportedException();
	IEnumerator<(TPriority, TElement)> IEnumerable<(TPriority, TElement)>.GetEnumerator() => new Enumerator(this);
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<(TPriority, TElement)>)this).GetEnumerator();

	void IQueue<(TPriority priority, TElement element)>.Enqueue((TPriority, TElement) item) => Enqueue(item.Item1, item.Item2);
	bool IQueue<(TPriority priority, TElement element)>.TryDequeue(out (TPriority priority, TElement element) item)
	{
		if (TryDequeue(out var element, out var priority))
		{
			item = (priority, element);
			return true;
		}

		item = default;
		return false;
	}
}