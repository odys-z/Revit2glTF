copy "glTFRevitExport.addin" "%ALLUSERSPROFILE%\Autodesk\Revit\Addins\2017"
copy "bin\Debug2017\Newtonsoft.Json.dll" "%ALLUSERSPROFILE%\Autodesk\Revit/Addins\2017\gltfExporter"
xcopy /y /i "bin\Debug2017\glTF*" "%ALLUSERSPROFILE%\Autodesk\Revit\Addins\2017\gltfExporter\"
