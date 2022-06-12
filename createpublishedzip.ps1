param(
    [String]$os="win10;linux;osx",
    [String]$arch="x64",
    [String]$configuration="Release",
    [bool]$createzip=$True
    )

function Build-SFP {
    param (
        [string]$TargetRuntime
    )

    Remove-Item -Path "./$configuration/publish" -Recurse -Force -ErrorAction Ignore
    dotnet publish "SFP_UI/SFP_UI.csproj" --configuration $configuration --output $configuration/publish --self-contained --runtime $targetRuntime
    if ($createzip)
    {
        Get-ChildItem "./$configuration/publish/*" -Recurse -Exclude SFP.config,*.log,FluentAvalonia.pdb,FileWatcherEx.pdb | Compress-Archive -DestinationPath "./$configuration/SFP_UI-SelfContained-$targetRuntime.zip" -Force
    }
}

foreach ($currentOS in $os.Split(";"))
{
    $targetRuntime = "$currentOS-$arch"

    Build-SFP -TargetRuntime $targetRuntime
}
