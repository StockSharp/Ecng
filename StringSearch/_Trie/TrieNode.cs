// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;

namespace Gma.DataStructures.StringSearch
{
	using Ecng.Collections;

	public class TrieNode<TValue> : TrieNodeBase<TValue>
    {
        private readonly Dictionary<char, TrieNode<TValue>> m_Children;
        private readonly Queue<TValue> m_Values;

        protected TrieNode()
        {
            m_Children = new Dictionary<char, TrieNode<TValue>>();
            m_Values = new Queue<TValue>();
        }

        protected override int KeyLength => 1;

		protected override IEnumerable<TrieNodeBase<TValue>> Children()
        {
            return m_Children.Values;
        }

        protected override IEnumerable<TValue> Values()
        {
            return m_Values;
        }

        protected override TrieNodeBase<TValue> GetOrCreateChild(char key)
        {
            TrieNode<TValue> result;
            if (m_Children.Count == 0 || !m_Children.TryGetValue(key, out result))
            {
                result = new TrieNode<TValue>();
                m_Children.Add(key, result);
            }
            return result;
        }

        protected override TrieNodeBase<TValue> GetChildOrNull(string query, int position)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            TrieNode<TValue> childNode;
            return
                m_Children.TryGetValue(query[position], out childNode)
                    ? childNode
                    : null;
        }

        protected override void AddValue(TValue value)
        {
            m_Values.Enqueue(value);
        }

		public void Remove(TValue value)
		{
			RemoveRange(new[] { value });
		}

		public void RemoveRange(IEnumerable<TValue> values)
		{
			var temp = new HashSet<TValue>(m_Values);
			temp.RemoveRange(values);

			m_Values.Clear();

			foreach (var item in temp)
				m_Values.Enqueue(item);

			var emptyNodes = new List<char>();

			foreach (var pair in m_Children)
			{
				var node = pair.Value;

				node.RemoveRange(values);

				if (node.m_Values.Count == 0 && node.m_Children.Count == 0)
					emptyNodes.Add(pair.Key);
			}

			foreach (var emptyNode in emptyNodes)
			{
				m_Children.Remove(emptyNode);
			}
		}

		public void Clear()
		{
			m_Children.Clear();
			m_Values.Clear();
		}
    }
}