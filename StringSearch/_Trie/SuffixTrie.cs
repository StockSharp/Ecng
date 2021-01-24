// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php
using System.Collections.Generic;
using System.Linq;

namespace Gma.DataStructures.StringSearch
{
	using System;

	public class SuffixTrie<T> : ITrie<T>
    {
        private readonly Trie<T> m_InnerTrie;
        private readonly int m_MinSuffixLength;

        public SuffixTrie(int minSuffixLength)
            : this(new Trie<T>(), minSuffixLength)
        {
        }

        private SuffixTrie(Trie<T> innerTrie, int minSuffixLength)
        {
			if (minSuffixLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(minSuffixLength));

            m_InnerTrie = innerTrie;
            m_MinSuffixLength = minSuffixLength;
        }

        public IEnumerable<T> Retrieve(string query)
        {
            return
                m_InnerTrie
                    .Retrieve(query)
                    .Distinct();
        }

        public void Add(string key, T value)
        {
			for (int i = key.Length - m_MinSuffixLength; i >= 0; i--)
			{
				m_InnerTrie.Add(key, i, value);
			}
		}

	    public void Remove(T value)
	    {
			m_InnerTrie.Remove(value);
	    }

	    public void RemoveRange(IEnumerable<T> values)
	    {
			m_InnerTrie.RemoveRange(values);
		}

	    public void Clear()
	    {
			m_InnerTrie.Clear();
	    }

		private static IEnumerable<string> GetAllSuffixes(int minSuffixLength, string word)
		{
			for (int i = word.Length - minSuffixLength; i >= 0; i--)
			{
				var partition = new StringPartition(word, i);
				yield return partition.ToString();
			}
		}
	}
}