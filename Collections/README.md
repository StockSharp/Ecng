# Ecng.Collections

Thread-safe collections, specialized data structures, and collection utilities for high-performance scenarios.

## Thread-Safe Collections

All synchronized collections provide thread-safe operations with internal locking.

### SynchronizedDictionary

Thread-safe wrapper around `Dictionary<TKey, TValue>`.

```csharp
var dict = new SynchronizedDictionary<int, string>();

// Thread-safe operations
dict[1] = "one";
dict.Add(2, "two");

if (dict.TryGetValue(1, out var value))
    Console.WriteLine(value); // "one"

// Manual locking for compound operations
using (dict.EnterScope())
{
    if (!dict.ContainsKey(3))
        dict.Add(3, "three");
}
```

### SynchronizedList

Thread-safe list with notification support.

```csharp
var list = new SynchronizedList<string>();

list.Add("item1");
list.AddRange(new[] { "item2", "item3" });

// Safe iteration with scope
using (list.EnterScope())
{
    foreach (var item in list)
        Console.WriteLine(item);
}
```

### CachedSynchronizedList

Thread-safe list that caches its array representation for fast enumeration.

```csharp
var list = new CachedSynchronizedList<int>();

list.Add(1);
list.Add(2);
list.Add(3);

// Cache is computed once and reused until list changes
int[] cached = list.Cache;
foreach (var item in cached)
    Console.WriteLine(item);

// After modification, cache is invalidated
list.Add(4);
int[] newCache = list.Cache; // Recomputed
```

### CachedSynchronizedDictionary

Thread-safe dictionary with cached key/value arrays.

```csharp
var dict = new CachedSynchronizedDictionary<string, int>();

dict["a"] = 1;
dict["b"] = 2;

// Cached arrays for fast iteration
string[] keys = dict.CachedKeys;
int[] values = dict.CachedValues;
KeyValuePair<string, int>[] pairs = dict.CachedPairs;
```

### SynchronizedSet

Thread-safe `HashSet<T>` wrapper.

```csharp
var set = new SynchronizedSet<int>();

set.Add(1);
set.Add(2);
bool added = set.Add(1); // false, already exists

bool contains = set.Contains(1); // true
```

## Specialized Collections

### PairSet - Bidirectional Dictionary

Allows lookup by both key and value.

```csharp
var pairs = new PairSet<int, string>();

pairs.Add(1, "one");
pairs.Add(2, "two");

// Forward lookup (key -> value)
string value = pairs.GetValue(1); // "one"

// Reverse lookup (value -> key)
int key = pairs.GetKey("two"); // 2

// Check existence
bool hasKey = pairs.ContainsKey(1);
bool hasValue = pairs.ContainsValue("one");

// Remove by value
pairs.RemoveByValue("one");
```

### SynchronizedPairSet

Thread-safe version of `PairSet`.

```csharp
var pairs = new SynchronizedPairSet<Guid, string>();

var id = Guid.NewGuid();
pairs.Add(id, "session-1");

// Thread-safe bidirectional lookup
if (pairs.TryGetValue(id, out var session))
    Console.WriteLine(session);

if (pairs.TryGetKey("session-1", out var foundId))
    Console.WriteLine(foundId);
```

### CircularBuffer

Fixed-size buffer that overwrites oldest elements when full.

```csharp
var buffer = new CircularBuffer<int>(capacity: 3);

buffer.PushBack(1);
buffer.PushBack(2);
buffer.PushBack(3);
// Buffer: [1, 2, 3]

buffer.PushBack(4);
// Buffer: [2, 3, 4] - oldest (1) was overwritten

int front = buffer.Front(); // 2
int back = buffer.Back();   // 4
int second = buffer[1];     // 3

// Remove from ends
buffer.PopFront(); // Removes 2
buffer.PopBack();  // Removes 4

// Convert to array
int[] arr = buffer.ToArray();

// Resize buffer
buffer.Capacity = 5; // Grow
buffer.Capacity = 2; // Shrink (keeps newest elements)
```

### CircularBufferEx

Extended circular buffer with additional operations.

```csharp
var buffer = new CircularBufferEx<decimal>(100);

// Add elements
buffer.PushBack(1.5m);
buffer.PushBack(2.5m);

// Sum, min, max operations
decimal sum = buffer.Sum;
decimal min = buffer.Min;
decimal max = buffer.Max;
```

### NumericCircularBufferEx

Circular buffer optimized for numeric calculations with running statistics.

```csharp
var buffer = new NumericCircularBufferEx<double>(100);

for (int i = 0; i < 100; i++)
    buffer.PushBack(Math.Sin(i));

// Efficient statistics (computed incrementally)
double sum = buffer.Sum;
double min = buffer.Min;
double max = buffer.Max;
```

## Queue and Stack Extensions

### SynchronizedQueue

Thread-safe queue.

```csharp
var queue = new SynchronizedQueue<Message>();

queue.Enqueue(new Message("hello"));

if (queue.TryDequeue(out var msg))
    Process(msg);
```

### SynchronizedStack

Thread-safe stack.

```csharp
var stack = new SynchronizedStack<int>();

stack.Push(1);
stack.Push(2);

if (stack.TryPop(out var value))
    Console.WriteLine(value); // 2
```

### QueueEx / StackEx

Extended queue and stack with additional peek operations.

```csharp
var queue = new QueueEx<int>();
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);

int first = queue.PeekFirst();  // 1 (front)
int last = queue.PeekLast();    // 3 (back)
```

## Ordered Channels

### BaseOrderedChannel

Base class for ordered message processing with channels.

```csharp
// Used for ordered async message processing
// See ChannelExtensions for usage patterns
```

## Bit Array Operations

### BitArrayWriter / BitArrayReader

Efficient bit-level I/O for binary data.

```csharp
// Writing bits
var writer = new BitArrayWriter();
writer.Write(true);           // 1 bit
writer.Write(42, 8);          // 8 bits
writer.WriteInt(1000);        // Variable-length int

byte[] data = writer.ToArray();

// Reading bits
var reader = new BitArrayReader(data);
bool flag = reader.Read();     // 1 bit
int value = reader.Read(8);    // 8 bits
int number = reader.ReadInt(); // Variable-length int
```

## Collection Interfaces

### INotifyList

List that raises events on modifications.

```csharp
public interface INotifyList<T> : IList<T>
{
    event Action<T> Adding;
    event Action<T> Added;
    event Action<T> Removing;
    event Action<T> Removed;
    event Action Clearing;
    event Action Cleared;
    event Action Changed;
}
```

### ISynchronizedCollection

Interface for thread-safe collections.

```csharp
public interface ISynchronizedCollection<T> : ICollection<T>
{
    SyncObject SyncRoot { get; }
    LockScope EnterScope();
}
```

## Extension Methods

```csharp
using Ecng.Collections;

// Check if collection is empty
IEnumerable<int> items = GetItems();
if (items.IsEmpty())
    return;

// Safe first/last
var first = items.FirstOr(defaultValue: -1);
var last = items.LastOr(defaultValue: -1);

// Batch processing
foreach (var batch in items.Batch(100))
    ProcessBatch(batch);

// Index lookup
int index = items.IndexOf(x => x > 10);
```

## NuGet

```
Install-Package Ecng.Collections
```
