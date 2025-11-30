global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.VisualStudio.TestTools.UnitTesting;

global using Ecng.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]