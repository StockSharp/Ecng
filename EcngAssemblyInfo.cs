#region Using Directives

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Resources;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCompany("StockSharp LP")]
[assembly: AssemblyProduct("Ecng system framework")]
[assembly: AssemblyCopyright("Copyright @ StockSharp 2020")]
[assembly: AssemblyTrademark("StockSharp")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

//[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
//[assembly: AllowPartiallyTrustedCallers]

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en-US")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]