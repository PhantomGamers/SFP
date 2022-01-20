param(
    [String]$os="win;linux;osx",
    [String]$arch="x64",
    [String]$configuration="Release"
    )

foreach ($currentOS in $os.Split(";")) 
{
    $targetRuntime = "$currentOS-$arch"
    
    Remove-Item -Path "./$configuration/publish" -Recurse -Force -ErrorAction Ignore
    dotnet publish "SFP_UI/SFP_UI.csproj" --configuration $configuration --output $configuration/publish --runtime $targetRuntime --self-contained
    # Get-ChildItem "./$configuration/publish/*" -Recurse -Exclude SFP.config,*.log | %{7z.exe a -mx3 "./$configuration/SFP_UI-SelfContained-$targetRuntime.zip" $_.FullName}
    Get-ChildItem "./$configuration/publish/*" -Recurse -Exclude SFP.config,*.log | Compress-Archive -DestinationPath "./$configuration/SFP_UI-SelfContained-$targetRuntime.zip" -Force
}