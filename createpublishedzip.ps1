param(
    [String]$os = "win10;linux;osx",
    [String]$arch = "x64",
    [String]$configuration = "Release",
    [bool]$createzip = $True,
    [String]$selfcontained = "both"
)

function Build-SFP {
    param (
        [string]$TargetRuntime,
        [bool]$selfContained = $True
    )

    Remove-Item -Path "./$configuration/publish" -Recurse -Force -ErrorAction Ignore
    [String[]]$selfContainedFlag = if ($selfContained) { "--self-contained" } else { "--no-self-contained -p:PublishTrimmed=false -p:TrimMode=""full""".Split(" ")}
    dotnet clean -c $configuration -r $TargetRuntime
    dotnet publish "SFP_UI/SFP_UI.csproj" --configuration $configuration --output $configuration/publish --runtime $TargetRuntime @selfContainedFlag
    if ($createzip) {
        $zipname = if ($selfContained) { "SFP_UI-$TargetRuntime-SelfContained.zip" } else { "SFP_UI-$TargetRuntime-net7.zip" }
        Get-ChildItem "./$configuration/publish/*" -Recurse -Exclude SFP.config,*.log,FluentAvalonia.pdb,FileWatcherEx.pdb | Compress-Archive -DestinationPath "./$configuration/$zipname" -Force
    }
}

foreach ($currentOS in $os.Split(";")) {
    $targetRuntime = "$currentOS-$arch"
    switch ($selfcontained.ToLower())
    {
        { @("yes", "true", "1") -contains $_ } {
            $selfcontainedValues = $True
        }
        { @("no", "false", "0") -contains $_ } {
            $selfcontainedValues = $False
        }
        "both" {
            $selfcontainedValues = $True, $False
        }
    }

    foreach ($value in $selfcontainedValues)
    {
        Build-SFP -TargetRuntime $targetRuntime -selfContained $value
    }
}
