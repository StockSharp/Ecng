# Ecng Native AOT compatibility audit

The core Ecng libraries keep their normal JIT behavior and NuGet package compatibility.

## Compile-time diagnostics

Run the following commands from the repository root:

```bash
dotnet build Common/Common.csproj -c Release -f net10.0 -p:EcngAotAudit=true
dotnet build Serialization/Serialization.csproj -c Release -f net10.0 -p:EcngAotAudit=true
```

This opt-in flag enables AOT, trimming and single-file analyzers on net10.0 builds.
It does not declare that the libraries are globally AOT-compatible.

## Minimal native smoke application

```bash
dotnet publish AotSmoke/AotSmoke.csproj -f net10.0 -c Release -r linux-x64 -p:EcngAotAudit=true
./AotSmoke/bin/Release/net10.0/linux-x64/publish/Ecng.AotSmoke
```

The smoke app requires Linux Clang and zlib development headers. It demonstrates:

- Factory registration with `TypeHelper.RegisterFactory<T>`
- Closed generic registration with `TypeHelper.RegisterConstructedGenericType`
- Name-to-type registration with `Converter.RegisterTypeName<T>`
- Source-generated System.Text.Json through `JsonHelper.FromJson/ToJson` overloads

Old Newtonsoft.Json and dynamic reflection helpers remain available to ordinary JIT applications.
For an AOT process, types must be referenced and registered at build time; arbitrary plugin DLL loading,
dynamic IL compilation and unregistered generic instantiations are not supported by these helpers.

Successful compilation and a passing smoke test do not establish AOT compatibility
of the full Ecng framework. Review the full IL2xxx/IL3xxx warning log before expanding coverage.
