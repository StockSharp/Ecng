# Ecng.Common

A set of helpers and extensions that simplify everyday .NET tasks.  The library contains convenient string utilities, a fast CSV parser, and a flexible converter able to transform many data types.

## Purpose

Provide reusable helpers for core scenarios so that applications can stay concise and readable.

## Key Features

- Rich string manipulation helpers
- High performance CSV parsing
- Flexible conversion between numerous types

## String helpers

`StringHelper` adds small but handy methods:

```csharp
"text".IsEmpty();                // bool check
"{0} + {1} = {2}".Put(1, 2, 3);  // simple formatting
"a\nb".SplitByLineSeps();         // split by any newline
```

## CSV parser

`FastCsvReader` reads CSV data without allocations:

```csharp
var csv = "Id;Name\n1;Foo\n2;Bar";
var reader = new FastCsvReader(csv, StringHelper.N);

while (reader.NextLine())
{
    var id = reader.ReadInt();
    var name = reader.ReadString();
    Console.WriteLine($"{id}: {name}");
}
```

## Type converter

`Converter` easily casts objects across a wide range of types:

```csharp
int number = "42".To<int>();        // string -> int
byte[] data = number.To<byte[]>();   // int -> byte[]
DateTime date = 1700L.To<DateTime>();
```

Additional converters can be registered through `Converter.AddTypedConverter`.
