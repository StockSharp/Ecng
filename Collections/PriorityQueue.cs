namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Collections/src/System/Collections/Generic/PriorityQueue.cs
// modifications:
// 1) generic arg reorder
// 2) implemented ICollection<(TPriority, TElement)>
// 3) implemented IQueue<(TPriority, TElement)>

/// <summary>
///  Represents a min priority queue.
/// </summary>
/// <typeparam name="TElement">Specifies the type of elements in the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the type of priority associated with enqueued elements.</typeparam>
/// <remarks>
///  Implements an array-backed quaternary min-heap. Each element is enqueued with an associated priority
///  that determines the dequeue order: elements with the lowest priority get dequeued first.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public class PriorityQueue<TPriority, TElement> : ICollection<(TPriority, TElement)>, IQueue<(TPriority priority, TElement element)>
{
	/// <summary>
	/// Represents an implicit heap-ordered complete d-ary tree, stored as an array.
	/// </summary>
	private (TPriority Priority, TElement Element)[] _nodes;

	/// <summary>
	/// Custom comparer used to order the heap.
	/// </summary>
	private readonly IComparer<TPriority> _comparer;

	/// <summary>
	/// Lazily-initialized collection used to expose the contents of the queue.
	/// </summary>
	private UnorderedItemsCollection _unorderedItems;

	/// <summary>
	/// The number of nodes in the heap.
	/// </summary>
	private int _size;

	/// <summary>
	/// Version updated on mutation to help validate enumerators operate on a consistent state.
	/// </summary>
	private int _version;

	/// <summary>
	/// Specifies the arity of the d-ary heap, which here is quaternary.
	/// It is assumed that this value is a power of 2.
	/// </summary>
	private const int _arity = 4;

	/// <summary>
	/// The binary logarithm of <see cref="_arity" />.
	/// </summary>
	private const int _log2Arity = 2;

#if DEBUG
	static PriorityQueue()
	{
		Debug.Assert(_log2Arity > 0 && Math.Pow(2, _log2Arity) == _arity);
	}
#endif

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class.
	/// </summary>
	public PriorityQueue()
	{
		_nodes = Array.Empty<(TPriority, TElement)>();
		_comparer = InitializeComparer(null);
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
	///  with the specified initial capacity.
	/// </summary>
	/// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	///  The specified <paramref name="initialCapacity"/> was negative.
	/// </exception>
	public PriorityQueue(int initialCapacity)
		: this(initialCapacity, comparer: null)
	{
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
	///  with the specified custom priority comparer.
	/// </summary>
	/// <param name="comparer">
	///  Custom comparer dictating the ordering of elements.
	///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
	/// </param>
	public PriorityQueue(IComparer<TPriority> comparer)
	{
		_nodes = Array.Empty<(TPriority, TElement)>();
		_comparer = InitializeComparer(comparer);
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
	///  with the specified initial capacity and custom priority comparer.
	/// </summary>
	/// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
	/// <param name="comparer">
	///  Custom comparer dictating the ordering of elements.
	///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">
	///  The specified <paramref name="initialCapacity"/> was negative.
	/// </exception>
	public PriorityQueue(int initialCapacity, IComparer<TPriority> comparer)
	{
		if (initialCapacity < 0)
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));

		_nodes = new (TPriority, TElement)[initialCapacity];
		_comparer = InitializeComparer(comparer);
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
	///  that is populated with the specified elements and priorities.
	/// </summary>
	/// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
	/// <exception cref="ArgumentNullException">
	///  The specified <paramref name="items"/> argument was <see langword="null"/>.
	/// </exception>
	/// <remarks>
	///  Constructs the heap using a heapify operation,
	///  which is generally faster than enqueuing individual elements sequentially.
	/// </remarks>
	public PriorityQueue(IEnumerable<(TPriority Priority, TElement Element)> items)
		: this(items, comparer: null)
	{
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="PriorityQueue{TPriority, TElement}"/> class
	///  that is populated with the specified elements and priorities,
	///  and with the specified custom priority comparer.
	/// </summary>
	/// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
	/// <param name="comparer">
	///  Custom comparer dictating the ordering of elements.
	///  Uses <see cref="Comparer{T}.Default" /> if the argument is <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">
	///  The specified <paramref name="items"/> argument was <see langword="null"/>.
	/// </exception>
	/// <remarks>
	///  Constructs the heap using a heapify operation,
	///  which is generally faster than enqueuing individual elements sequentially.
	/// </remarks>
	public PriorityQueue(IEnumerable<(TPriority Priority, TElement Element)> items, IComparer<TPriority> comparer)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		_nodes = items.ToArray();
		_size = _nodes.Length;
		_comparer = InitializeComparer(comparer);

		if (_size > 1)
		{
			Heapify();
		}
	}

	/// <summary>
	///  Gets the number of elements contained in the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public int Count => _size;

	/// <summary>
	///  Gets the priority comparer used by the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public IComparer<TPriority> Comparer => _comparer ?? Comparer<TPriority>.Default;

	/// <summary>
	///  Gets a collection that enumerates the elements of the queue in an unordered manner.
	/// </summary>
	/// <remarks>
	///  The enumeration does not order items by priority, since that would require N * log(N) time and N space.
	///  Items are instead enumerated following the internal array heap layout.
	/// </remarks>
	public UnorderedItemsCollection UnorderedItems => _unorderedItems ??= new UnorderedItemsCollection(this);

	/// <summary>
	///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	public void Enqueue(TPriority priority, TElement element)
	{
		// Virtually add the node at the end of the underlying array.
		// Note that the node being enqueued does not need to be physically placed
		// there at this point, as such an assignment would be redundant.

		int currentSize = _size;
		_version++;

		if (_nodes.Length == currentSize)
		{
			Grow(currentSize + 1);
		}

		_size = currentSize + 1;

		if (_comparer == null)
		{
			MoveUpDefaultComparer((priority, element), currentSize);
		}
		else
		{
			MoveUpCustomComparer((priority, element), currentSize);
		}
	}

	/// <summary>
	///  Returns the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/> without removing it.
	/// </summary>
	/// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{TPriority, TElement}"/> is empty.</exception>
	/// <returns>The minimal element of the <see cref="PriorityQueue{TPriority, TElement}"/>.</returns>
	public (TPriority priority, TElement element) Peek()
	{
		if (_size == 0)
		{
			throw new InvalidOperationException("Empty queue.");
		}

		return _nodes[0];
	}

	/// <summary>
	///  Removes and returns the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">The queue is empty.</exception>
	/// <returns>The minimal element of the <see cref="PriorityQueue{TPriority, TElement}"/>.</returns>
	public (TPriority priority, TElement element) Dequeue()
	{
		if (_size == 0)
		{
			throw new InvalidOperationException("Empty queue.");
		}

		var node = _nodes[0];
		RemoveRootNode();
		return node;
	}

	/// <summary>
	///  Removes the minimal element and then immediately adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	/// <exception cref="InvalidOperationException">The queue is empty.</exception>
	/// <returns>The minimal element removed before performing the enqueue operation.</returns>
	/// <remarks>
	///  Implements an extract-then-insert heap operation that is generally more efficient
	///  than sequencing Dequeue and Enqueue operations: in the worst case scenario only one
	///  shift-down operation is required.
	/// </remarks>
	public TElement DequeueEnqueue(TPriority priority, TElement element)
	{
		if (_size == 0)
		{
			throw new InvalidOperationException("Empty queue.");
		}

		(TPriority Priority, TElement Element) = _nodes[0];

		if (_comparer == null)
		{
			if (Comparer<TPriority>.Default.Compare(priority, Priority) > 0)
			{
				MoveDownDefaultComparer((priority, element), 0);
			}
			else
			{
				_nodes[0] = (priority, element);
			}
		}
		else
		{
			if (_comparer.Compare(priority, Priority) > 0)
			{
				MoveDownCustomComparer((priority, element), 0);
			}
			else
			{
				_nodes[0] = (priority, element);
			}
		}

		_version++;
		return Element;
	}

	/// <summary>
	///  Removes the minimal element from the <see cref="PriorityQueue{TPriority, TElement}"/>,
	///  and copies it to the <paramref name="element"/> parameter,
	///  and its associated priority to the <paramref name="priority"/> parameter.
	/// </summary>
	/// <param name="element">The removed element.</param>
	/// <param name="priority">The priority associated with the removed element.</param>
	/// <returns>
	///  <see langword="true"/> if the element is successfully removed;
	///  <see langword="false"/> if the <see cref="PriorityQueue{TPriority, TElement}"/> is empty.
	/// </returns>
	public bool TryDequeue(out TElement element, out TPriority priority)
	{
		if (_size != 0)
		{
			(priority, element) = _nodes[0];
			RemoveRootNode();
			return true;
		}

		element = default;
		priority = default;
		return false;
	}

	/// <summary>
	///  Returns a value that indicates whether there is a minimal element in the <see cref="PriorityQueue{TPriority, TElement}"/>,
	///  and if one is present, copies it to the <paramref name="element"/> parameter,
	///  and its associated priority to the <paramref name="priority"/> parameter.
	///  The element is not removed from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="element">The minimal element in the queue.</param>
	/// <param name="priority">The priority associated with the minimal element.</param>
	/// <returns>
	///  <see langword="true"/> if there is a minimal element;
	///  <see langword="false"/> if the <see cref="PriorityQueue{TPriority, TElement}"/> is empty.
	/// </returns>
	public bool TryPeek(out TElement element, out TPriority priority)
	{
		if (_size != 0)
		{
			(priority, element) = _nodes[0];
			return true;
		}

		element = default;
		priority = default;
		return false;
	}

	/// <summary>
	///  Adds the specified element with associated priority to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	///  and immediately removes the minimal element, returning the result.
	/// </summary>
	/// <param name="priority">The priority with which to associate the new element.</param>
	/// <param name="element">The element to add to the <see cref="PriorityQueue{TPriority, TElement}"/>.</param>
	/// <returns>The minimal element removed after the enqueue operation.</returns>
	/// <remarks>
	///  Implements an insert-then-extract heap operation that is generally more efficient
	///  than sequencing Enqueue and Dequeue operations: in the worst case scenario only one
	///  shift-down operation is required.
	/// </remarks>
	public TElement EnqueueDequeue(TPriority priority, TElement element)
	{
		if (_size != 0)
		{
			(TPriority Priority, TElement Element) = _nodes[0];

			if (_comparer == null)
			{
				if (Comparer<TPriority>.Default.Compare(priority, Priority) > 0)
				{
					MoveDownDefaultComparer((priority, element), 0);
					_version++;
					return Element;
				}
			}
			else
			{
				if (_comparer.Compare(priority, Priority) > 0)
				{
					MoveDownCustomComparer((priority, element), 0);
					_version++;
					return Element;
				}
			}
		}

		return element;
	}

	/// <summary>
	///  Enqueues a sequence of element/priority pairs to the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	/// <param name="items">The pairs of elements and priorities to add to the queue.</param>
	/// <exception cref="ArgumentNullException">
	///  The specified <paramref name="items"/> argument was <see langword="null"/>.
	/// </exception>
	public void EnqueueRange(IEnumerable<(TPriority Priority, TElement Element)> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		int count = 0;
		var collection = items as ICollection<(TPriority Priority, TElement Element)>;
		if (collection is not null && (count = collection.Count) > _nodes.Length - _size)
		{
			Grow(checked(_size + count));
		}

		if (_size == 0)
		{
			// build using Heapify() if the queue is empty.

			if (collection is not null)
			{
				collection.CopyTo(_nodes, 0);
				_size = count;
			}
			else
			{
				int i = 0;
				(TPriority, TElement)[] nodes = _nodes;
				foreach ((TPriority priority, TElement element) in items)
				{
					if (nodes.Length == i)
					{
						Grow(i + 1);
						nodes = _nodes;
					}

					nodes[i++] = (priority, element);
				}

				_size = i;
			}

			_version++;

			if (_size > 1)
			{
				Heapify();
			}
		}
		else
		{
			foreach ((TPriority priority, TElement element) in items)
			{
				Enqueue(priority, element);
			}
		}
	}

	/// <summary>
	///  Enqueues a sequence of elements pairs to the <see cref="PriorityQueue{TPriority, TElement}"/>,
	///  all associated with the specified priority.
	/// </summary>
	/// <param name="elements">The elements to add to the queue.</param>
	/// <param name="priority">The priority to associate with the new elements.</param>
	/// <exception cref="ArgumentNullException">
	///  The specified <paramref name="elements"/> argument was <see langword="null"/>.
	/// </exception>
	public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
	{
		if (elements is null)
			throw new ArgumentNullException(nameof(elements));

		int count;
		if (elements is ICollection<TElement> collection &&
			(count = collection.Count) > _nodes.Length - _size)
		{
			Grow(checked(_size + count));
		}

		if (_size == 0)
		{
			// build using Heapify() if the queue is empty.

			int i = 0;
			(TPriority, TElement)[] nodes = _nodes;
			foreach (TElement element in elements)
			{
				if (nodes.Length == i)
				{
					Grow(i + 1);
					nodes = _nodes;
				}

				nodes[i++] = (priority, element);
			}

			_size = i;
			_version++;

			if (i > 1)
			{
				Heapify();
			}
		}
		else
		{
			foreach (TElement element in elements)
			{
				Enqueue(priority, element);
			}
		}
	}

	/// <summary>
	///  Removes all items from the <see cref="PriorityQueue{TPriority, TElement}"/>.
	/// </summary>
	public void Clear()
	{
		// Clear the elements so that the gc can reclaim the references
		Array.Clear(_nodes, 0, _size);
		_size = 0;
		_version++;
	}

	/// <summary>
	///  Ensures that the <see cref="PriorityQueue{TPriority, TElement}"/> can hold up to
	///  <paramref name="capacity"/> items without further expansion of its backing storage.
	/// </summary>
	/// <param name="capacity">The minimum capacity to be used.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	///  The specified <paramref name="capacity"/> is negative.
	/// </exception>
	/// <returns>The current capacity of the <see cref="PriorityQueue{TPriority, TElement}"/>.</returns>
	public int EnsureCapacity(int capacity)
	{
		if (capacity < 0)
			throw new ArgumentOutOfRangeException(nameof(capacity));

		if (_nodes.Length < capacity)
		{
			Grow(capacity);
			_version++;
		}

		return _nodes.Length;
	}

	/// <summary>
	///  Sets the capacity to the actual number of items in the <see cref="PriorityQueue{TPriority, TElement}"/>,
	///  if that is less than 90 percent of current capacity.
	/// </summary>
	/// <remarks>
	///  This method can be used to minimize a collection's memory overhead
	///  if no new elements will be added to the collection.
	/// </remarks>
	public void TrimExcess()
	{
		int threshold = (int)(_nodes.Length * 0.9);
		if (_size < threshold)
		{
			Array.Resize(ref _nodes, _size);
			_version++;
		}
	}

	/// <summary>
	/// Grows the priority queue to match the specified min capacity.
	/// </summary>
	private void Grow(int minCapacity)
	{
		Debug.Assert(_nodes.Length < minCapacity);

		const int GrowFactor = 2;
		const int MinimumGrow = 4;

		int newcapacity = GrowFactor * _nodes.Length;

		// Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
		// Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
		if ((uint)newcapacity > 0X7FFFFFC7) newcapacity = 0X7FFFFFC7;

		// Ensure minimum growth is respected.
		newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);

		// If the computed capacity is still less than specified, set to the original argument.
		// Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
		if (newcapacity < minCapacity) newcapacity = minCapacity;

		Array.Resize(ref _nodes, newcapacity);
	}

	/// <summary>
	/// Removes the node from the root of the heap
	/// </summary>
	private void RemoveRootNode()
	{
		int lastNodeIndex = --_size;
		_version++;

		if (lastNodeIndex > 0)
		{
			(TPriority Priority, TElement Element) lastNode = _nodes[lastNodeIndex];
			if (_comparer == null)
			{
				MoveDownDefaultComparer(lastNode, 0);
			}
			else
			{
				MoveDownCustomComparer(lastNode, 0);
			}
		}

		_nodes[lastNodeIndex] = default;
	}

	/// <summary>
	/// Gets the index of an element's parent.
	/// </summary>
	private static int GetParentIndex(int index) => (index - 1) >> _log2Arity;

	/// <summary>
	/// Gets the index of the first child of an element.
	/// </summary>
	private static int GetFirstChildIndex(int index) => (index << _log2Arity) + 1;

	/// <summary>
	/// Converts an unordered list into a heap.
	/// </summary>
	private void Heapify()
	{
		// Leaves of the tree are in fact 1-element heaps, for which there
		// is no need to correct them. The heap property needs to be restored
		// only for higher nodes, starting from the first node that has children.
		// It is the parent of the very last element in the array.

		(TPriority Priority, TElement Element)[] nodes = _nodes;
		int lastParentWithChildren = GetParentIndex(_size - 1);

		if (_comparer == null)
		{
			for (int index = lastParentWithChildren; index >= 0; --index)
			{
				MoveDownDefaultComparer(nodes[index], index);
			}
		}
		else
		{
			for (int index = lastParentWithChildren; index >= 0; --index)
			{
				MoveDownCustomComparer(nodes[index], index);
			}
		}
	}

	/// <summary>
	/// Moves a node up in the tree to restore heap order.
	/// </summary>
	private void MoveUpDefaultComparer((TPriority Priority, TElement Element) node, int nodeIndex)
	{
		// Instead of swapping items all the way to the root, we will perform
		// a similar optimization as in the insertion sort.

		Debug.Assert(_comparer is null);
		Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

		(TPriority Priority, TElement Element)[] nodes = _nodes;

		while (nodeIndex > 0)
		{
			int parentIndex = GetParentIndex(nodeIndex);
			(TPriority Priority, TElement Element) parent = nodes[parentIndex];

			if (Comparer<TPriority>.Default.Compare(node.Priority, parent.Priority) < 0)
			{
				nodes[nodeIndex] = parent;
				nodeIndex = parentIndex;
			}
			else
			{
				break;
			}
		}

		nodes[nodeIndex] = node;
	}

	/// <summary>
	/// Moves a node up in the tree to restore heap order.
	/// </summary>
	private void MoveUpCustomComparer((TPriority Priority, TElement Element) node, int nodeIndex)
	{
		// Instead of swapping items all the way to the root, we will perform
		// a similar optimization as in the insertion sort.

		Debug.Assert(_comparer is not null);
		Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

		IComparer<TPriority> comparer = _comparer;
		(TPriority Priority, TElement Element)[] nodes = _nodes;

		while (nodeIndex > 0)
		{
			int parentIndex = GetParentIndex(nodeIndex);
			(TPriority Priority, TElement Element) parent = nodes[parentIndex];

			if (comparer.Compare(node.Priority, parent.Priority) < 0)
			{
				nodes[nodeIndex] = parent;
				nodeIndex = parentIndex;
			}
			else
			{
				break;
			}
		}

		nodes[nodeIndex] = node;
	}

	/// <summary>
	/// Moves a node down in the tree to restore heap order.
	/// </summary>
	private void MoveDownDefaultComparer((TPriority Priority, TElement Element) node, int nodeIndex)
	{
		// The node to move down will not actually be swapped every time.
		// Rather, values on the affected path will be moved up, thus leaving a free spot
		// for this value to drop in. Similar optimization as in the insertion sort.

		Debug.Assert(_comparer is null);
		Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

		(TPriority Priority, TElement Element)[] nodes = _nodes;
		int size = _size;

		int i;
		while ((i = GetFirstChildIndex(nodeIndex)) < size)
		{
			// Find the child node with the minimal priority
			(TPriority Priority, TElement Element) minChild = nodes[i];
			int minChildIndex = i;

			int childIndexUpperBound = Math.Min(i + _arity, size);
			while (++i < childIndexUpperBound)
			{
				(TPriority Priority, TElement Element) nextChild = nodes[i];
				if (Comparer<TPriority>.Default.Compare(nextChild.Priority, minChild.Priority) < 0)
				{
					minChild = nextChild;
					minChildIndex = i;
				}
			}

			// Heap property is satisfied; insert node in this location.
			if (Comparer<TPriority>.Default.Compare(node.Priority, minChild.Priority) < 0)
			{
				break;
			}

			// Move the minimal child up by one node and
			// continue recursively from its location.
			nodes[nodeIndex] = minChild;
			nodeIndex = minChildIndex;
		}

		nodes[nodeIndex] = node;
	}

	/// <summary>
	/// Moves a node down in the tree to restore heap order.
	/// </summary>
	private void MoveDownCustomComparer((TPriority Priority, TElement Element) node, int nodeIndex)
	{
		// The node to move down will not actually be swapped every time.
		// Rather, values on the affected path will be moved up, thus leaving a free spot
		// for this value to drop in. Similar optimization as in the insertion sort.

		Debug.Assert(_comparer is not null);
		Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

		IComparer<TPriority> comparer = _comparer;
		(TPriority Priority, TElement Element)[] nodes = _nodes;
		int size = _size;

		int i;
		while ((i = GetFirstChildIndex(nodeIndex)) < size)
		{
			// Find the child node with the minimal priority
			(TPriority Priority, TElement Element) minChild = nodes[i];
			int minChildIndex = i;

			int childIndexUpperBound = Math.Min(i + _arity, size);
			while (++i < childIndexUpperBound)
			{
				(TPriority Priority, TElement Element) nextChild = nodes[i];
				if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
				{
					minChild = nextChild;
					minChildIndex = i;
				}
			}

			// Heap property is satisfied; insert node in this location.
			if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
			{
				break;
			}

			// Move the minimal child up by one node and continue recursively from its location.
			nodes[nodeIndex] = minChild;
			nodeIndex = minChildIndex;
		}

		nodes[nodeIndex] = node;
	}

	/// <summary>
	/// Initializes the custom comparer to be used internally by the heap.
	/// </summary>
	private static IComparer<TPriority> InitializeComparer(IComparer<TPriority> comparer)
	{
		if (typeof(TPriority).IsValueType)
		{
			if (comparer == Comparer<TPriority>.Default)
			{
				// if the user manually specifies the default comparer,
				// revert to using the optimized path.
				return null;
			}

			return comparer;
		}
		else
		{
			// Currently the JIT doesn't optimize direct Comparer<T>.Default.Compare
			// calls for reference types, so we want to cache the comparer instance instead.
			// TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
			return comparer ?? Comparer<TPriority>.Default;
		}
	}

	/// <summary>
	///  Enumerates the contents of a <see cref="PriorityQueue{TPriority, TElement}"/>, without any ordering guarantees.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public sealed class UnorderedItemsCollection : IReadOnlyCollection<(TPriority Priority, TElement Element)>, ICollection
	{
		internal readonly PriorityQueue<TPriority, TElement> _queue;

		internal UnorderedItemsCollection(PriorityQueue<TPriority, TElement> queue) => _queue = queue;

		public int Count => _queue._size;
		object ICollection.SyncRoot => this;
		bool ICollection.IsSynchronized => false;

		void ICollection.CopyTo(Array array, int index)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			if (array.Rank != 1)
			{
				throw new ArgumentException("Multi dimensions.", nameof(array));
			}

			if (array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException("Non zero lower bound.", nameof(array));
			}

			if (index < 0 || index > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index invalid.");
			}

			if (array.Length - index < _queue._size)
			{
				throw new ArgumentException("Index offset.");
			}

			try
			{
				Array.Copy(_queue._nodes, 0, array, index, _queue._size);
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException("Incompatible array type", nameof(array));
			}
		}

		/// <summary>
		///  Enumerates the element and priority pairs of a <see cref="PriorityQueue{TPriority, TElement}"/>,
		///  without any ordering guarantees.
		/// </summary>
		public struct Enumerator : IEnumerator<(TPriority Priority, TElement Element)>
		{
			private readonly PriorityQueue<TPriority, TElement> _queue;
			private readonly int _version;
			private int _index;
			private (TPriority, TElement) _current;

			internal Enumerator(PriorityQueue<TPriority, TElement> queue)
			{
				_queue = queue;
				_index = 0;
				_version = queue._version;
				_current = default;
			}

			/// <summary>
			/// Releases all resources used by the <see cref="Enumerator"/>.
			/// </summary>
			public void Dispose() { }

			/// <summary>
			/// Advances the enumerator to the next element of the <see cref="UnorderedItems"/>.
			/// </summary>
			/// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.</returns>
			public bool MoveNext()
			{
				PriorityQueue<TPriority, TElement> localQueue = _queue;

				if (_version == localQueue._version && ((uint)_index < (uint)localQueue._size))
				{
					_current = localQueue._nodes[_index];
					_index++;
					return true;
				}

				return MoveNextRare();
			}

			private bool MoveNextRare()
			{
				if (_version != _queue._version)
				{
					throw new InvalidOperationException("Version mistmach.");
				}

				_index = _queue._size + 1;
				_current = default;
				return false;
			}

			/// <summary>
			/// Gets the element at the current position of the enumerator.
			/// </summary>
			public (TPriority Priority, TElement Element) Current => _current;
			object IEnumerator.Current => _current;

			void IEnumerator.Reset()
			{
				if (_version != _queue._version)
				{
					throw new InvalidOperationException("Version mistmach.");
				}

				_index = 0;
				_current = default;
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the <see cref="UnorderedItems"/>.
		/// </summary>
		/// <returns>An <see cref="Enumerator"/> for the <see cref="UnorderedItems"/>.</returns>
		public Enumerator GetEnumerator() => new(_queue);

		IEnumerator<(TPriority Priority, TElement Element)> IEnumerable<(TPriority Priority, TElement Element)>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	bool ICollection<(TPriority, TElement)>.IsReadOnly => false;
	void ICollection<(TPriority, TElement)>.Add((TPriority, TElement) item) => Enqueue(item.Item1, item.Item2);

	bool ICollection<(TPriority, TElement)>.Contains((TPriority, TElement) item) => throw new NotImplementedException();
	void ICollection<(TPriority, TElement)>.CopyTo((TPriority, TElement)[] array, int arrayIndex) => throw new NotImplementedException();
	bool ICollection<(TPriority, TElement)>.Remove((TPriority, TElement) item) => throw new NotImplementedException();
	IEnumerator<(TPriority, TElement)> IEnumerable<(TPriority, TElement)>.GetEnumerator() => UnorderedItems.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<(TPriority, TElement)>)this).GetEnumerator();

	void IQueue<(TPriority priority, TElement element)>.Enqueue((TPriority, TElement) item) => Enqueue(item.Item1, item.Item2);
}