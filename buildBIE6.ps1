# ----------- BepInEx 6 Mono -----------
dotnet build src/UnityExplorer.sln -c Release_BIE6_Mono
$Path = "Release/UnityExplorer.BepInEx6.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/UnityExplorer.BIE6.Mono.dll $Path/UnityExplorer.BIE6.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/UnityExplorer.BIE6.Mono.dll -Destination $Path/plugins/sinai-dev-UnityExplorer -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/sinai-dev-UnityExplorer -Force
Move-Item -Path $Path/ECSExtension.dll -Destination $Path/plugins/sinai-dev-UnityExplorer -Force
Move-Item -Path $Path/ECSExtension.pdb -Destination $Path/plugins/sinai-dev-UnityExplorer -Force

# (create zip archive)
Remove-Item $Path/../UnityExplorer.BepInEx6.Mono.zip -ErrorAction SilentlyContinue
7z a $Path/../UnityExplorer.BepInEx6.Mono.zip .\$Path\*