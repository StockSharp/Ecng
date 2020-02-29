"c:\program files\dotnet\dotnet.exe" pack -o pub -c Release --no-build Ecng.sln
rd /S /Q pub\out
pub\nuget.exe init pub pub\out -Expand
xcopy "pub\out\*.*" "..\StockSharp (GitHub)\packages\" /K /H /Y /E
rd /S /Q pub\out
del "pub\*.nupkg"
