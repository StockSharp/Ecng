namespace Ecng.Tests.Collections;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PriorityQueueTests
{
	[TestMethod]
	public void SameOrder()
	{
		var pq = new Ecng.Collections.PriorityQueue<long, int>((p1, p2) => (p1 - p2).Abs());
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 1);
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 2);
		pq.Enqueue(2, 30);
		pq.Enqueue(3, 40);
		pq.Enqueue(1, 3);
		pq.Enqueue(0, 0);
		pq.Count.AssertEqual(8);

		pq.Dequeue();
		pq.Count.AssertEqual(7);

		var emptyCnt = 2;

		for (var i = 0; i < emptyCnt; i++)
			pq.Dequeue();

		var left = 7 - emptyCnt;
		pq.Count.AssertEqual(left);

		var prev = 0;

		while (pq.Count > 0)
		{
			var curr = pq.Dequeue().element;
			(curr > prev).AssertTrue();
			prev = curr;
			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Random()
	{
		var pq = new Ecng.Collections.PriorityQueue<long, int>((p1, p2) => (p1 - p2).Abs());

		var count = 100000;

		var prev = 0;

		for (var i = 0; i < count; i++)
			pq.Enqueue(RandomGen.GetInt(0, 100), ++prev);

		pq.Count.AssertEqual(count);

		var prevPriority = 0L;
		var left = count;

		while (pq.Count > 0)
		{
			var (priority, _) = pq.Dequeue();

			(prevPriority <= priority).AssertTrue();
			prevPriority = priority;

			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Seq()
	{
		var pq = new Ecng.Collections.PriorityQueue<long, int>((p1, p2) => (p1 - p2).Abs());

		var count = 100000;

		var prev = 0;

		for (var i = 0; i < count; i++)
			pq.Enqueue(i, ++prev);

		pq.Count.AssertEqual(count);

		var prevPriority = 0L;
		var left = count;

		while (pq.Count > 0)
		{
			var (priority, _) = pq.Dequeue();

			(prevPriority <= priority).AssertTrue();
			prevPriority = priority;

			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Enumeration()
	{
		var pq = new Ecng.Collections.PriorityQueue<long, int>((p1, p2) => (p1 - p2).Abs());
		pq.Enqueue(1, 1);
		pq.Enqueue(2, 2);

		var counter = 0;

		foreach (var item in pq)
			counter++;

		counter.AssertEqual(2);

		pq.Enqueue(1, 1);
		pq.Enqueue(2, 2);

		counter = 0;

		foreach (var item in pq)
			counter++;

		counter.AssertEqual(4);
	}
}
