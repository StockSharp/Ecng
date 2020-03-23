del "pub\*.nupkg"
"c:\program files\dotnet\dotnet.exe" pack -o pub -c Debug --no-build xaml.charting\xaml.charting.csproj
pub\nuget.exe init pub pub\out -Expand
del "pub\*.nupkg"
