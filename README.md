# ECNG System Framework

[![Build Status](https://github.com/stocksharp/ecng/actions/workflows/dotnet.yml/badge.svg)](https://github.com/stocksharp/ecng/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/stocksharp/ecng/blob/master/LICENSE)

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

- **Ecng.Common** [![NuGet](https://img.shields.io/nuget/v/Ecng.Common.svg)](https://www.nuget.org/packages/Ecng.Common) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Common.svg)](https://www.nuget.org/packages/Ecng.Common) – basic helpers and utility classes used throughout the framework.
- **Ecng.Collections** [![NuGet](https://img.shields.io/nuget/v/Ecng.Collections.svg)](https://www.nuget.org/packages/Ecng.Collections) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Collections.svg)](https://www.nuget.org/packages/Ecng.Collections) – additional collection types and algorithms.
- **Ecng.ComponentModel** [![NuGet](https://img.shields.io/nuget/v/Ecng.ComponentModel.svg)](https://www.nuget.org/packages/Ecng.ComponentModel) [![NuGet](https://img.shields.io/nuget/dt/Ecng.ComponentModel.svg)](https://www.nuget.org/packages/Ecng.ComponentModel) – components implementing property change notifications and other design-time helpers.
- **Ecng.Configuration** [![NuGet](https://img.shields.io/nuget/v/Ecng.Configuration.svg)](https://www.nuget.org/packages/Ecng.Configuration) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Configuration.svg)](https://www.nuget.org/packages/Ecng.Configuration) – configuration infrastructure for reading and storing settings.
- **Ecng.Data** [![NuGet](https://img.shields.io/nuget/v/Ecng.Data.svg)](https://www.nuget.org/packages/Ecng.Data) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Data.svg)](https://www.nuget.org/packages/Ecng.Data) – common abstractions for data access layers.
- **Ecng.Data.Linq2db** [![NuGet](https://img.shields.io/nuget/v/Ecng.Data.Linq2db.svg)](https://www.nuget.org/packages/Ecng.Data.Linq2db) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Data.Linq2db.svg)](https://www.nuget.org/packages/Ecng.Data.Linq2db) – providers and helpers for [linq2db](https://github.com/linq2db/linq2db).
- **Ecng.Drawing** [![NuGet](https://img.shields.io/nuget/v/Ecng.Drawing.svg)](https://www.nuget.org/packages/Ecng.Drawing) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Drawing.svg)](https://www.nuget.org/packages/Ecng.Drawing) – lightweight primitives for UI-related drawing tasks.
- **Ecng.IO** [![NuGet](https://img.shields.io/nuget/v/Ecng.IO.svg)](https://www.nuget.org/packages/Ecng.IO) [![NuGet](https://img.shields.io/nuget/dt/Ecng.IO.svg)](https://www.nuget.org/packages/Ecng.IO) – file system helpers and compression utilities.
- **Ecng.Interop** [![NuGet](https://img.shields.io/nuget/v/Ecng.Interop.svg)](https://www.nuget.org/packages/Ecng.Interop) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Interop.svg)](https://www.nuget.org/packages/Ecng.Interop) – cross‑platform interop helpers and unmanaged memory tools.
- **Ecng.Interop.Windows** [![NuGet](https://img.shields.io/nuget/v/Ecng.Interop.Windows.svg)](https://www.nuget.org/packages/Ecng.Interop.Windows) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Interop.Windows.svg)](https://www.nuget.org/packages/Ecng.Interop.Windows) – Windows‑specific interop functionality.
- **Ecng.Linq** [![NuGet](https://img.shields.io/nuget/v/Ecng.Linq.svg)](https://www.nuget.org/packages/Ecng.Linq) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Linq.svg)](https://www.nuget.org/packages/Ecng.Linq) – extensions for working with LINQ expressions and queries.
- **Ecng.Localization** [![NuGet](https://img.shields.io/nuget/v/Ecng.Localization.svg)](https://www.nuget.org/packages/Ecng.Localization) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Localization.svg)](https://www.nuget.org/packages/Ecng.Localization) – simple localization engine and resource helpers.
- **Ecng.Logging** [![NuGet](https://img.shields.io/nuget/v/Ecng.Logging.svg)](https://www.nuget.org/packages/Ecng.Logging) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Logging.svg)](https://www.nuget.org/packages/Ecng.Logging) – flexible logging framework with multiple listeners.
- **Ecng.MathLight** [![NuGet](https://img.shields.io/nuget/v/Ecng.MathLight.svg)](https://www.nuget.org/packages/Ecng.MathLight) [![NuGet](https://img.shields.io/nuget/dt/Ecng.MathLight.svg)](https://www.nuget.org/packages/Ecng.MathLight) – small set of math and statistics utilities.
- **Ecng.Net** [![NuGet](https://img.shields.io/nuget/v/Ecng.Net.svg)](https://www.nuget.org/packages/Ecng.Net) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Net.svg)](https://www.nuget.org/packages/Ecng.Net) – core networking helpers including REST utilities.
- **Ecng.Net.Clients** [![NuGet](https://img.shields.io/nuget/v/Ecng.Net.Clients.svg)](https://www.nuget.org/packages/Ecng.Net.Clients) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Net.Clients.svg)](https://www.nuget.org/packages/Ecng.Net.Clients) – base classes for building HTTP clients.
- **Ecng.Net.SocketIO** [![NuGet](https://img.shields.io/nuget/v/Ecng.Net.SocketIO.svg)](https://www.nuget.org/packages/Ecng.Net.SocketIO) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Net.SocketIO.svg)](https://www.nuget.org/packages/Ecng.Net.SocketIO) – client implementation of the Socket.IO protocol.
- **Ecng.Nuget** [![NuGet](https://img.shields.io/nuget/v/Ecng.Nuget.svg)](https://www.nuget.org/packages/Ecng.Nuget) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Nuget.svg)](https://www.nuget.org/packages/Ecng.Nuget) – helpers for interacting with NuGet feeds.
- **Ecng.Reflection** [![NuGet](https://img.shields.io/nuget/v/Ecng.Reflection.svg)](https://www.nuget.org/packages/Ecng.Reflection) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Reflection.svg)](https://www.nuget.org/packages/Ecng.Reflection) – reflection extensions and dynamic type utilities.
- **Ecng.Security** [![NuGet](https://img.shields.io/nuget/v/Ecng.Security.svg)](https://www.nuget.org/packages/Ecng.Security) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Security.svg)](https://www.nuget.org/packages/Ecng.Security) – cryptography helpers and authorization primitives.
- **Ecng.Serialization** [![NuGet](https://img.shields.io/nuget/v/Ecng.Serialization.svg)](https://www.nuget.org/packages/Ecng.Serialization) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Serialization.svg)](https://www.nuget.org/packages/Ecng.Serialization) – serialization services for JSON and binary formats.
- **Ecng.Server.Utils** [![NuGet](https://img.shields.io/nuget/v/Ecng.Server.Utils.svg)](https://www.nuget.org/packages/Ecng.Server.Utils) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Server.Utils.svg)](https://www.nuget.org/packages/Ecng.Server.Utils) – utilities for hosting background services.
- **Ecng.UnitTesting** [![NuGet](https://img.shields.io/nuget/v/Ecng.UnitTesting.svg)](https://www.nuget.org/packages/Ecng.UnitTesting) [![NuGet](https://img.shields.io/nuget/dt/Ecng.UnitTesting.svg)](https://www.nuget.org/packages/Ecng.UnitTesting) – additional assertions for unit tests.
- **Ecng.Backup** [![NuGet](https://img.shields.io/nuget/v/Ecng.Backup.svg)](https://www.nuget.org/packages/Ecng.Backup) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Backup.svg)](https://www.nuget.org/packages/Ecng.Backup) – abstractions for implementing backup services.
- **Ecng.Backup.AWS** [![NuGet](https://img.shields.io/nuget/v/Ecng.Backup.AWS.svg)](https://www.nuget.org/packages/Ecng.Backup.AWS) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Backup.AWS.svg)](https://www.nuget.org/packages/Ecng.Backup.AWS) – backup providers for Amazon S3 and Glacier.
- **Ecng.Backup.Azure** [![NuGet](https://img.shields.io/nuget/v/Ecng.Backup.Azure.svg)](https://www.nuget.org/packages/Ecng.Backup.Azure) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Backup.Azure.svg)](https://www.nuget.org/packages/Ecng.Backup.Azure) – backup provider for Azure storage.
- **Ecng.Backup.Mega** [![NuGet](https://img.shields.io/nuget/v/Ecng.Backup.Mega.svg)](https://www.nuget.org/packages/Ecng.Backup.Mega) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Backup.Mega.svg)](https://www.nuget.org/packages/Ecng.Backup.Mega) – provider for backups to MEGA cloud.
- **Ecng.Backup.Yandex** [![NuGet](https://img.shields.io/nuget/v/Ecng.Backup.Yandex.svg)](https://www.nuget.org/packages/Ecng.Backup.Yandex) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Backup.Yandex.svg)](https://www.nuget.org/packages/Ecng.Backup.Yandex) – provider for Yandex Disk based backups.
- **Ecng.Compilation** [![NuGet](https://img.shields.io/nuget/v/Ecng.Compilation.svg)](https://www.nuget.org/packages/Ecng.Compilation) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Compilation.svg)](https://www.nuget.org/packages/Ecng.Compilation) – dynamic compilation helpers and abstractions.
- **Ecng.Compilation.FSharp** [![NuGet](https://img.shields.io/nuget/v/Ecng.Compilation.FSharp.svg)](https://www.nuget.org/packages/Ecng.Compilation.FSharp) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Compilation.FSharp.svg)](https://www.nuget.org/packages/Ecng.Compilation.FSharp) – F# compiler integration.
- **Ecng.Compilation.Python** [![NuGet](https://img.shields.io/nuget/v/Ecng.Compilation.Python.svg)](https://www.nuget.org/packages/Ecng.Compilation.Python) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Compilation.Python.svg)](https://www.nuget.org/packages/Ecng.Compilation.Python) – utilities for running Python scripts.
- **Ecng.Compilation.Roslyn** [![NuGet](https://img.shields.io/nuget/v/Ecng.Compilation.Roslyn.svg)](https://www.nuget.org/packages/Ecng.Compilation.Roslyn) [![NuGet](https://img.shields.io/nuget/dt/Ecng.Compilation.Roslyn.svg)](https://www.nuget.org/packages/Ecng.Compilation.Roslyn) – C# compiler services via Roslyn.
- **Ecng.Tests** – project containing unit tests for the libraries.

## Contribution

Contributions are welcome! If you have improvements or bug fixes, please feel free to fork the repository, make your changes, and submit a pull request.


