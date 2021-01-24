// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Gma.DataStructures.StringSearch
{
	using Ecng.Collections;

	public class ConcurrentTrieNode<TValue> : TrieNodeBase<TValue>
    {
        private readonly ConcurrentDictionary<char, ConcurrentTrieNode<TValue>> m_Children;
        private readonly ConcurrentQueue<TValue> m_Values;

        public ConcurrentTrieNode()
        {
            m_Children = new ConcurrentDictionary<char, ConcurrentTrieNode<TValue>>();
            m_Values = new ConcurrentQueue<TValue>();
        }


        protected override int KeyLength => 1;

		protected override IEnumerable<TValue> Values()
        {
            return m_Values;
        }

        protected override IEnumerable<TrieNodeBase<TValue>> Children()
        {
            return m_Children.Values;
        }

        protected override void AddValue(TValue value)
        {
            m_Values.Enqueue(value);
        }

        protected override TrieNodeBase<TValue> GetOrCreateChild(char key)
        {
            return m_Children.GetOrAdd(key, new ConcurrentTrieNode<TValue>());
        }

        protected override TrieNodeBase<TValue> GetChildOrNull(string query, int position)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            ConcurrentTrieNode<TValue> childNode;
            return
                m_Children.TryGetValue(query[position], out childNode)
                    ? childNode
                    : null;
        }

		public void Remove(TValue value)
		{
			RemoveRange(new[] { value });
		}

		public void RemoveRange(IEnumerable<TValue> values)
		{
			var temp = new HashSet<TValue>(m_Values);
			temp.RemoveRange(values);

			while (!m_Values.IsEmpty)
			{
				TValue v;

				if (!m_Values.TryDequeue(out v))
					break;
			}

			foreach (var item in temp)
				m_Values.Enqueue(item);

			var emptyNodes = new List<char>();

			foreach (var pair in m_Children)
			{
				var node = pair.Value;

				node.RemoveRange(values);

				if (node.m_Values.IsEmpty)
					emptyNodes.Add(pair.Key);
			}

			foreach (var emptyNode in emptyNodes)
			{
				ConcurrentTrieNode<TValue> node;
				m_Children.TryRemove(emptyNode, out node);
			}
		}

		public void Clear()
		{
			m_Children.Clear();

			while (!m_Values.IsEmpty)
			{
				TValue value;

				if (!m_Values.TryDequeue(out value))
					break;
			}
		}
    }
}