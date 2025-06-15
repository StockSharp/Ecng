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

## Project Overview

Below is a brief description of the projects included in this repository. Each
library is distributed as a separate NuGet package with the `Ecng` prefix.

- **Ecng.Common** – basic helpers and utility classes used throughout the
  framework.
- **Ecng.Collections** – additional collection types and algorithms.
- **Ecng.ComponentModel** – components implementing property change
  notifications and other design-time helpers.
- **Ecng.Configuration** – configuration infrastructure for reading and storing
  settings.
- **Ecng.Data** – common abstractions for data access layers.
- **Ecng.Data.Linq2db** – providers and helpers for [linq2db](https://github.com/linq2db/linq2db).
- **Ecng.Drawing** – lightweight primitives for UI-related drawing tasks.
- **Ecng.IO** – file system helpers and compression utilities.
- **Ecng.Interop** – cross‑platform interop helpers and unmanaged memory tools.
- **Ecng.Interop.Windows** – Windows‑specific interop functionality.
- **Ecng.Linq** – extensions for working with LINQ expressions and queries.
- **Ecng.Localization** – simple localization engine and resource helpers.
- **Ecng.Logging** – flexible logging framework with multiple listeners.
- **Ecng.MathLight** – small set of math and statistics utilities.
- **Ecng.Net** – core networking helpers including REST utilities.
- **Ecng.Net.Clients** – base classes for building HTTP clients.
- **Ecng.Net.SocketIO** – client implementation of the Socket.IO protocol.
- **Ecng.Nuget** – helpers for interacting with NuGet feeds.
- **Ecng.Reflection** – reflection extensions and dynamic type utilities.
- **Ecng.Security** – cryptography helpers and authorization primitives.
- **Ecng.Serialization** – serialization services for JSON and binary formats.
- **Ecng.Server.Utils** – utilities for hosting background services.
- **Ecng.UnitTesting** – additional assertions for unit tests.
- **Ecng.Backup** – abstractions for implementing backup services.
- **Ecng.Backup.AWS** – backup providers for Amazon S3 and Glacier.
- **Ecng.Backup.Azure** – backup provider for Azure storage.
- **Ecng.Backup.Mega** – provider for backups to MEGA cloud.
- **Ecng.Backup.Yandex** – provider for Yandex Disk based backups.
- **Ecng.Compilation** – dynamic compilation helpers and abstractions.
- **Ecng.Compilation.FSharp** – F# compiler integration.
- **Ecng.Compilation.Python** – utilities for running Python scripts.
- **Ecng.Compilation.Roslyn** – C# compiler services via Roslyn.
- **Ecng.Tests** – project containing unit tests for the libraries.

## Contribution

Contributions are welcome! If you have improvements or bug fixes, please feel free to fork the repository, make your changes, and submit a pull request.


