# StringSearch - Efficient String Search Data Structures

A collection of high-performance string search data structures including Trie, Patricia Trie, and Ukkonen's Suffix Tree implementations. These data structures are optimized for fast prefix and substring searching operations.

## Features

- Multiple trie implementations for different use cases
- Fast prefix and substring searching
- Generic value associations with string keys
- Memory-efficient Patricia Trie (compressed trie)
- Ukkonen's Suffix Tree for advanced substring searching
- Concurrent trie support for multi-threaded scenarios
- MIT licensed open-source implementation

## Installation

Add a reference to the StringSearch project in your solution.

## Quick Start

### Basic Trie Usage

```csharp
using Gma.DataStructures.StringSearch;

// Create a Patricia Trie (most commonly used)
var trie = new PatriciaTrie<int>();

// Add key-value pairs
trie.Add("apple", 1);
trie.Add("application", 2);
trie.Add("apply", 3);
trie.Add("banana", 4);

// Retrieve values by prefix
var results = trie.Retrieve("app");
// Returns: [1, 2, 3] (values associated with "apple", "application", "apply")

var results2 = trie.Retrieve("ban");
// Returns: [4] (value associated with "banana")
```

## Data Structures

### PatriciaTrie

A space-optimized trie (radix tree) that stores strings efficiently by compressing chains of nodes with single children.

**Best for:**
- Prefix searching
- Autocomplete features
- Memory-efficient storage of strings with common prefixes

```csharp
using Gma.DataStructures.StringSearch;

var trie = new PatriciaTrie<string>();

// Add entries
trie.Add("test", "Test Value");
trie.Add("testing", "Testing Value");
trie.Add("tester", "Tester Value");
trie.Add("tea", "Tea Value");

// Search by prefix
var results = trie.Retrieve("test");
// Returns: ["Test Value", "Testing Value", "Tester Value"]

var results2 = trie.Retrieve("te");
// Returns: ["Test Value", "Testing Value", "Tester Value", "Tea Value"]
```

### PatriciaSuffixTrie

A Patricia Trie that indexes all suffixes of added strings, enabling substring searching.

**Best for:**
- Substring searching
- Finding all occurrences of a pattern within stored strings
- "Contains" queries

```csharp
using Gma.DataStructures.StringSearch;

// Create with minimum query length of 3 characters
var suffixTrie = new PatriciaSuffixTrie<string>(minQueryLength: 3);

// Add strings with associated values
suffixTrie.Add("hello world", "Document 1");
suffixTrie.Add("world peace", "Document 2");
suffixTrie.Add("hello there", "Document 3");

// Search for substring "wor"
var results = suffixTrie.Retrieve("wor");
// Returns: ["Document 1", "Document 2"] (both contain "world")

// Search for substring "hello"
var results2 = suffixTrie.Retrieve("hello");
// Returns: ["Document 1", "Document 3"]
```

### SuffixTrie

A standard trie-based suffix tree implementation.

```csharp
using Gma.DataStructures.StringSearch;

// Create with minimum suffix length of 2
var suffixTrie = new SuffixTrie<string>(minSuffixLength: 2);

suffixTrie.Add("programming", "Item 1");
suffixTrie.Add("grammar", "Item 2");

// Search for "gram"
var results = suffixTrie.Retrieve("gram");
// Returns: ["Item 1", "Item 2"] (both contain "gram")
```

### UkkonenTrie

An implementation of Ukkonen's linear-time suffix tree construction algorithm.

**Best for:**
- Large text indexing
- Efficient substring search in big datasets
- Advanced pattern matching

```csharp
using Gma.DataStructures.StringSearch;

// Create with minimum suffix length
var ukkonen = new UkkonenTrie<string>(minSuffixLength: 3);

ukkonen.Add("banana", "Fruit 1");
ukkonen.Add("bandana", "Clothing 1");

// Search for "ana"
var results = ukkonen.Retrieve("ana");
// Returns: ["Fruit 1", "Clothing 1"]
```

### ConcurrentTrie

Thread-safe trie implementation for concurrent access scenarios.

```csharp
using Gma.DataStructures.StringSearch;
using System.Threading.Tasks;

var concurrentTrie = new ConcurrentTrie<int>();

// Safe for concurrent access
Parallel.For(0, 1000, i =>
{
    concurrentTrie.Add($"key{i}", i);
});

// Retrieve from multiple threads
var results = concurrentTrie.Retrieve("key1");
```

## Common Interface: ITrie<TValue>

All trie implementations implement the `ITrie<TValue>` interface:

```csharp
public interface ITrie<TValue>
{
    void Add(string key, TValue value);
    IEnumerable<TValue> Retrieve(string query);
    void Remove(TValue value);
    void RemoveRange(IEnumerable<TValue> values);
    void Clear();
}
```

## Usage Examples

### Autocomplete System

```csharp
using Gma.DataStructures.StringSearch;

public class AutocompleteSystem
{
    private readonly PatriciaTrie<string> _trie;

    public AutocompleteSystem()
    {
        _trie = new PatriciaTrie<string>();
    }

    public void AddWord(string word)
    {
        _trie.Add(word.ToLower(), word);
    }

    public IEnumerable<string> GetSuggestions(string prefix)
    {
        return _trie.Retrieve(prefix.ToLower()).Distinct();
    }
}

// Usage
var autocomplete = new AutocompleteSystem();
autocomplete.AddWord("Apple");
autocomplete.AddWord("Application");
autocomplete.AddWord("Approach");
autocomplete.AddWord("Banana");

var suggestions = autocomplete.GetSuggestions("app");
// Returns: ["Apple", "Application", "Approach"]
```

### Document Search Engine

```csharp
using Gma.DataStructures.StringSearch;
using System.Linq;

public class DocumentSearchEngine
{
    private readonly PatriciaSuffixTrie<string> _index;

    public DocumentSearchEngine(int minQueryLength = 3)
    {
        _index = new PatriciaSuffixTrie<string>(minQueryLength);
    }

    public void IndexDocument(string documentId, string content)
    {
        // Index the document content
        _index.Add(content.ToLower(), documentId);
    }

    public IEnumerable<string> Search(string query)
    {
        if (query.Length < 3)
            return Enumerable.Empty<string>();

        return _index.Retrieve(query.ToLower()).Distinct();
    }
}

// Usage
var searchEngine = new DocumentSearchEngine();
searchEngine.IndexDocument("doc1", "The quick brown fox jumps over the lazy dog");
searchEngine.IndexDocument("doc2", "A quick movement in the forest");
searchEngine.IndexDocument("doc3", "The lazy cat sleeps");

var results = searchEngine.Search("quick");
// Returns: ["doc1", "doc2"]

var results2 = searchEngine.Search("lazy");
// Returns: ["doc1", "doc3"]
```

### Phone Directory

```csharp
using Gma.DataStructures.StringSearch;

public class Contact
{
    public string Name { get; set; }
    public string Phone { get; set; }

    public override string ToString() => $"{Name}: {Phone}";
}

public class PhoneDirectory
{
    private readonly PatriciaTrie<Contact> _trie;

    public PhoneDirectory()
    {
        _trie = new PatriciaTrie<Contact>();
    }

    public void AddContact(string name, string phone)
    {
        var contact = new Contact { Name = name, Phone = phone };
        _trie.Add(name.ToLower(), contact);
    }

    public IEnumerable<Contact> FindByPrefix(string namePrefix)
    {
        return _trie.Retrieve(namePrefix.ToLower());
    }

    public void RemoveContact(Contact contact)
    {
        _trie.Remove(contact);
    }

    public void Clear()
    {
        _trie.Clear();
    }
}

// Usage
var directory = new PhoneDirectory();
directory.AddContact("Alice Smith", "555-0001");
directory.AddContact("Alice Johnson", "555-0002");
directory.AddContact("Bob Brown", "555-0003");

var contacts = directory.FindByPrefix("alic");
// Returns all contacts starting with "alic"
```

### IP Address Lookup

```csharp
using Gma.DataStructures.StringSearch;

public class IpAddressLookup
{
    private readonly PatriciaTrie<string> _trie;

    public IpAddressLookup()
    {
        _trie = new PatriciaTrie<string>();
    }

    public void AddMapping(string ipPrefix, string location)
    {
        _trie.Add(ipPrefix, location);
    }

    public IEnumerable<string> FindLocations(string ipPrefix)
    {
        return _trie.Retrieve(ipPrefix);
    }
}

// Usage
var ipLookup = new IpAddressLookup();
ipLookup.AddMapping("192.168.1", "Local Network A");
ipLookup.AddMapping("192.168.2", "Local Network B");
ipLookup.AddMapping("10.0.0", "VPN Network");

var locations = ipLookup.FindLocations("192.168");
// Returns: ["Local Network A", "Local Network B"]
```

### Tag Search System

```csharp
using Gma.DataStructures.StringSearch;
using System.Collections.Generic;

public class TaggedItem
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<string> Tags { get; set; }
}

public class TagSearchSystem
{
    private readonly PatriciaTrie<TaggedItem> _tagIndex;

    public TagSearchSystem()
    {
        _tagIndex = new PatriciaTrie<TaggedItem>();
    }

    public void AddItem(TaggedItem item)
    {
        foreach (var tag in item.Tags)
        {
            _tagIndex.Add(tag.ToLower(), item);
        }
    }

    public IEnumerable<TaggedItem> SearchByTag(string tagPrefix)
    {
        return _tagIndex.Retrieve(tagPrefix.ToLower()).Distinct();
    }

    public void RemoveItem(TaggedItem item)
    {
        _tagIndex.Remove(item);
    }
}

// Usage
var tagSystem = new TagSearchSystem();

tagSystem.AddItem(new TaggedItem
{
    Id = "1",
    Title = "Article about C#",
    Tags = new List<string> { "csharp", "programming", "dotnet" }
});

tagSystem.AddItem(new TaggedItem
{
    Id = "2",
    Title = "Article about C++",
    Tags = new List<string> { "cpp", "programming", "native" }
});

var items = tagSystem.SearchByTag("prog");
// Returns both articles (both have "programming" tag)

var csharpItems = tagSystem.SearchByTag("csh");
// Returns only the first article
```

### Code Symbol Search

```csharp
using Gma.DataStructures.StringSearch;

public class Symbol
{
    public string Name { get; set; }
    public string Type { get; set; }  // class, method, property, etc.
    public string FilePath { get; set; }
}

public class CodeSymbolIndex
{
    private readonly PatriciaTrie<Symbol> _symbolTrie;
    private readonly PatriciaSuffixTrie<Symbol> _substringTrie;

    public CodeSymbolIndex()
    {
        _symbolTrie = new PatriciaTrie<Symbol>();
        _substringTrie = new PatriciaSuffixTrie<Symbol>(minQueryLength: 2);
    }

    public void IndexSymbol(Symbol symbol)
    {
        var key = symbol.Name.ToLower();
        _symbolTrie.Add(key, symbol);
        _substringTrie.Add(key, symbol);
    }

    public IEnumerable<Symbol> SearchByPrefix(string prefix)
    {
        return _symbolTrie.Retrieve(prefix.ToLower());
    }

    public IEnumerable<Symbol> SearchBySubstring(string substring)
    {
        return _substringTrie.Retrieve(substring.ToLower());
    }
}

// Usage
var codeIndex = new CodeSymbolIndex();

codeIndex.IndexSymbol(new Symbol
{
    Name = "GetUserById",
    Type = "method",
    FilePath = "UserService.cs"
});

codeIndex.IndexSymbol(new Symbol
{
    Name = "UserRepository",
    Type = "class",
    FilePath = "UserRepository.cs"
});

// Prefix search
var prefixResults = codeIndex.SearchByPrefix("getuser");
// Returns: GetUserById

// Substring search
var substringResults = codeIndex.SearchBySubstring("user");
// Returns: GetUserById, UserRepository
```

## Performance Characteristics

| Data Structure | Add | Search | Space | Best Use Case |
|----------------|-----|--------|-------|---------------|
| PatriciaTrie | O(k) | O(k) | Low | Prefix search, autocomplete |
| PatriciaSuffixTrie | O(k²) | O(k) | Medium | Substring search |
| SuffixTrie | O(k²) | O(k) | High | Suffix-based search |
| UkkonenTrie | O(k) | O(k) | Medium | Large text indexing |
| ConcurrentTrie | O(k) | O(k) | Medium | Multi-threaded scenarios |

*where k is the length of the string*

## Choosing the Right Data Structure

### Use PatriciaTrie when:
- You need prefix-based searching (autocomplete, type-ahead)
- Memory efficiency is important
- Your strings share common prefixes
- You only need to find strings that START with a query

### Use PatriciaSuffixTrie when:
- You need substring searching (find strings CONTAINING a query)
- You want to find all occurrences of a pattern
- Memory usage is acceptable
- Query strings have a minimum length

### Use UkkonenTrie when:
- You're indexing large amounts of text
- You need the most efficient suffix tree construction
- Substring search performance is critical
- You have complex pattern matching requirements

### Use ConcurrentTrie when:
- Multiple threads need to access the trie simultaneously
- Thread safety is required
- You're building a shared index in a multi-threaded application

## Important Notes

1. **Case Sensitivity**: Most examples use `.ToLower()` for case-insensitive search. The trie itself is case-sensitive.

2. **Minimum Query Length**: Suffix tries accept a minimum query/suffix length to reduce memory usage and improve performance.

3. **Duplicate Values**: `Retrieve()` may return duplicate values if the same value was added with different keys. Use `.Distinct()` if needed.

4. **Remove Operations**: `Remove()` removes all occurrences of a value, regardless of which keys it was added with.

5. **Thread Safety**: Only `ConcurrentTrie` is thread-safe. Other implementations require external synchronization for concurrent access.

## Target Frameworks

- .NET Standard 2.0
- .NET 6.0
- .NET 10.0

## License

This code is distributed under MIT license.
Copyright (c) 2013 George Mamaladze
See license.txt or http://opensource.org/licenses/mit-license.php
