global using global::System;
global using global::System.Collections;
global using global::System.Collections.Concurrent;
global using global::System.Collections.Generic;
global using global::System.Diagnostics;
global using global::System.Globalization;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net;
global using global::System.Net.Http;
global using global::System.Net.Sockets;
global using global::System.Reflection;
global using global::System.Runtime.CompilerServices;
global using global::System.Security;
global using global::System.Text;
global using global::System.Threading;
global using global::System.Threading.Tasks;

global using global::Ecng.Collections;
global using global::Ecng.Common;
global using global::Ecng.ComponentModel;
global using global::Ecng.Data;
global using global::Ecng.IO;
global using global::Ecng.Logging;
global using global::Ecng.Net;
global using global::Ecng.Serialization;
global using global::Ecng.UnitTesting;

global using global::Microsoft.VisualStudio.TestTools.UnitTesting;

global using global::Nito.AsyncEx;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
