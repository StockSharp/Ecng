global using System;
global using System.IO;
global using System.Linq;
global using System.Collections.Generic;
global using System.Threading.Tasks;

global using Ecng.Common;
global using Ecng.Collections;
global using Ecng.UnitTesting;

global using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]