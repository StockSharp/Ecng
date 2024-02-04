using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecng.Tests.Collections;

[TestClass]
public class PriorityQueueTests
{
	[TestMethod]
	public void SameOrder()
	{
		var pq = new Ecng.Collections.PriorityQueue<long, int>();
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 1);
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 2);

		var emptyCnt = 2;

		for (var i = 0; i < emptyCnt; i++)
			pq.Dequeue();

		var prev = 0;

		while (pq.Count > 0)
		{
			var curr = pq.Dequeue().element;
			(curr > prev).AssertTrue();
			prev = curr;
		}
	}
}
