nuget pack glTFRevitExport.nuspec
nuget push GLTFExport*.nupkg -Source https://api.nuget.org/v3/index.json
rm GLTFExport*.nupkg
