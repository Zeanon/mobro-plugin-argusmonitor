Remove-Item -Force -Recurse -ErrorAction SilentlyContinue .\ArgusMonitorPlugin.zip

dotnet publish --framework net8.0-windows --runtime win-x64 --self-contained false --configuration Release -p:DebugType=None -p:DebugSymbols=false -p:GenerateRuntimeConfigurationFiles=false --output ArgusMonitorPlugin .

& "C:\Program Files\7-Zip\7z.exe" a -tzip "ArgusMonitorPlugin.zip" .\ArgusMonitorPlugin\*

Remove-Item -Force -Recurse -ErrorAction SilentlyContinue .\ArgusMonitorPlugin