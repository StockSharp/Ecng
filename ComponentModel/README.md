# Ecng.ComponentModel

MVVM infrastructure, property change notifications, validation attributes, and UI component helpers.

## Property Change Notifications

### NotifiableObject

Base class implementing `INotifyPropertyChanged` and `INotifyPropertyChanging`.

```csharp
using Ecng.ComponentModel;

public class Person : NotifiableObject
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;

            NotifyChanging();  // Fires PropertyChanging
            _name = value;
            NotifyChanged();   // Fires PropertyChanged
        }
    }
}

// Subscribe to changes
var person = new Person();
person.PropertyChanged += (s, e) =>
{
    Console.WriteLine($"{e.PropertyName} changed");
};
```

### ViewModelBase

Extended base class for ViewModels with dispatcher support.

```csharp
public class MainViewModel : ViewModelBase
{
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyChanged();
        }
    }
}
```

### DispatcherNotifiableObject

Notifiable object that marshals property changes to the UI thread.

```csharp
public class ThreadSafeModel : DispatcherNotifiableObject
{
    // PropertyChanged events automatically dispatched to UI thread
}
```

## Observable Collections

### ObservableCollectionEx

Extended `ObservableCollection<T>` with additional features.

```csharp
var collection = new ObservableCollectionEx<Item>();

// Add range (fires single notification)
collection.AddRange(items);

// Remove range
collection.RemoveRange(itemsToRemove);

// Reset with new items
collection.Reset(newItems);
```

### DispatcherObservableCollection

Thread-safe observable collection with UI dispatcher support.

```csharp
var collection = new DispatcherObservableCollection<Item>();

// Safe to modify from any thread
Task.Run(() =>
{
    collection.Add(new Item()); // Automatically dispatched
});
```

### ConvertibleObservableCollection

Observable collection that transforms source items.

```csharp
var models = new ObservableCollection<PersonModel>();
var viewModels = new ConvertibleObservableCollection<PersonModel, PersonViewModel>(
    models,
    model => new PersonViewModel(model));

// Adding to models automatically adds to viewModels
models.Add(new PersonModel());
```

## Range Types

### Range<T>

Generic range with min/max bounds.

```csharp
var range = new Range<int>(1, 100);

// Check containment
bool contains = range.Contains(50);    // true
bool outside = range.Contains(150);    // false

// Range properties
int min = range.Min;        // 1
int max = range.Max;        // 100
int length = range.Length;  // 99

// Range operations
var other = new Range<int>(50, 150);
var intersection = range.Intersect(other);  // Range(50, 100)
var subRange = range.SubRange(20, 80);      // Range(20, 80)
```

### NumericRange

Specialized numeric range with additional operations.

```csharp
var range = new NumericRange<decimal>(0, 100);

// Percentage calculations, clamping, etc.
```

## Validation Attributes

### Numeric Validation

```csharp
public class Settings
{
    [IntValidation(Min = 1, Max = 100)]
    public int Count { get; set; }

    [DoubleValidation(Min = 0.0, Max = 1.0)]
    public double Factor { get; set; }

    [DecimalValidation(Min = 0)]
    public decimal Price { get; set; }

    [LongValidation(Min = 0, Max = 1000000)]
    public long Volume { get; set; }
}
```

### TimeSpan Validation

```csharp
[TimeSpanValidation(Min = "00:00:01", Max = "24:00:00")]
public TimeSpan Timeout { get; set; }
```

### Price Validation

```csharp
[PriceValidation(Min = 0)]
public Price StockPrice { get; set; }
```

## Price Type

Financial price with type information.

```csharp
var price = new Price(100.50m, PriceTypes.Limit);

// Price types
PriceTypes.Limit     // Limit price
PriceTypes.Market    // Market price
PriceTypes.Percent   // Percentage
```

## Attributes

### Step Attribute

Define increment step for numeric properties.

```csharp
[Step(0.01)]
public decimal Price { get; set; }

[Step(1)]
public int Quantity { get; set; }
```

### Icon Attributes

```csharp
[Icon("path/to/icon.png")]
public class MyCommand { }

[VectorIcon("fas fa-home")]  // Font Awesome style
public class HomeCommand { }
```

### Doc Attribute

Link to documentation.

```csharp
[Doc("https://docs.example.com/my-feature")]
public class MyFeature { }
```

### BasicSetting Attribute

Mark property as a basic (commonly used) setting.

```csharp
[BasicSetting]
public string ApiKey { get; set; }
```

## Server Credentials

Manage server connection credentials securely.

```csharp
var credentials = new ServerCredentials
{
    Email = "user@example.com",
    Password = "secret".Secure(),  // SecureString
    Token = "api-token".Secure()
};

// Check if configured
bool isConfigured = credentials.IsConfigured;
```

## Process Singleton

Ensure only one instance of an application runs.

```csharp
using var singleton = new ProcessSingleton("MyApp");

if (!singleton.TryAcquire())
{
    Console.WriteLine("Another instance is already running");
    return;
}

// Application continues...
```

## Dispatcher

Abstract UI thread dispatcher.

```csharp
public interface IDispatcher
{
    void Invoke(Action action);
    Task InvokeAsync(Action action);
    void BeginInvoke(Action action);
}

// Check if on UI thread
if (!dispatcher.CheckAccess())
{
    dispatcher.Invoke(() => UpdateUI());
}
```

## Channel Executor

Process items from a channel with controlled concurrency.

```csharp
var executor = new ChannelExecutor<WorkItem>(
    processItem: async item =>
    {
        await ProcessAsync(item);
    },
    maxConcurrency: 4);

// Add work
executor.Enqueue(new WorkItem());

// Graceful shutdown
await executor.StopAsync();
```

## Statistics

Track simple statistics.

```csharp
var stat = new Stat("ProcessedItems");

stat.Increment();
stat.Add(5);

Console.WriteLine($"Total: {stat.Value}");
```

## Periodic Action Planner

Schedule recurring actions.

```csharp
var planner = new PeriodicActionPlanner();

planner.Schedule(
    TimeSpan.FromMinutes(5),
    async () => await RefreshDataAsync());

// Stop scheduling
planner.Cancel();
```

## NuGet

```
Install-Package Ecng.ComponentModel
```
