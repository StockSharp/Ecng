# ECNG System Framework

The ECNG System Framework is a comprehensive collection of system classes designed for the development of connectors under the StockSharp platform. It provides a robust foundation for extending the functionality and performance of applications integrated with StockSharp, offering a wide range of utilities and enhancements for .NET standard types and beyond.

## Main Components

- **Ecng.Common**: Enhancements and extensions for standard .NET types, facilitating more efficient and streamlined development processes.
- **Ecng.Collections**: A set of additional collections that offer more functionalities and flexibility compared to the standard .NET collections.
- **Ecng.Serialization**: Custom serializers to JSON format, allowing for efficient serialization and deserialization of objects, crucial for network communication and data storage.
- **Ecng.Net**: Extensions for working with REST and WebSocket protocols, enabling seamless integration with web services and real-time data feeds.

## Purpose

This framework aims to simplify the complexities involved in developing connectors for the StockSharp platform by providing a rich set of utilities and classes. Whether it's enhancing basic .NET types, offering advanced collection structures, enabling custom serialization options, or facilitating network communications, the ECNG System Framework covers a wide spectrum of development needs.

## Getting Started

To get started with the ECNG System Framework, clone the repository from [GitHub](https://github.com/stocksharp/ecng) and explore the documentation for each component to understand its functionalities and how it can be integrated into your StockSharp platform projects.

## NuGet Packages

All projects within the ECNG System Framework are available as separate NuGet packages, which can be easily found with the prefix `Ecng`. For a detailed search of these packages, visit [NuGet.org](https://www.nuget.org/packages/?q=ecng).

## Contribution

Contributions are welcome! If you have improvements or bug fixes, please feel free to fork the repository, make your changes, and submit a pull request.

## Git Hooks

The repository contains a pre-commit hook that runs the unit tests. To enable it, configure Git to use the hooks in the `.githooks` directory:

```bash
git config core.hooksPath .githooks
```

After running the command above once, Git will execute the unit tests before each commit is created.
