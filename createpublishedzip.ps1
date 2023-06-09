param(
  [String]$os = "win10;linux;osx",
  [String]$arch = "x64",
  [String]$configuration = "Release",
  [bool]$createzip = $True,
  [String]$selfcontained = "both"
)

function Build-SFP
{
  param (
    [string]$TargetRuntime,
    [bool]$selfContained = $True
  )

  Remove-Item -Path "./$configuration/publish" -Recurse -Force -ErrorAction Ignore
  [String[]]$selfContainedFlag = if ($selfContained)
  {
    "--self-contained"
  }
  else
  {
    "--no-self-contained -p:PublishTrimmed=false -p:TrimMode=""full""".Split(" ")
  }
  [String]$bundleFlag = if ($TargetRuntime.StartsWith("osx"))
  {
    "-t:BundleApp"
  }
  else
  {
    ""
  }
  dotnet publish "SFP_UI/SFP_UI.csproj" $bundleFlag --configuration $configuration --output $configuration/publish --runtime $TargetRuntime @selfContainedFlag
  if ($TargetRuntime.StartsWith("osx"))
  {
    Copy-Item -Path "./SFP_UI/Assets/SFP-logo.icns" -Destination "$configuration/publish/SFP_UI.app/Contents/Resources/SFP-logo.icns" -Force
    if ($IsMacOS)
    {
     xattr -c "$configuration/publish/SFP_UI.app"
    }
  }
  if ($createzip)
  {
    $excludeFiles = "SFP.dll.config", "SFP.config", "*.log", "FluentAvalonia.pdb", "FileWatcherEx.pdb"
    $publishDir = Join-Path "." "$configuration" "publish"
    if ($TargetRuntime.StartsWith("win")) {
      $zipname = if ($selfContained)
      {
        "SFP_UI-$TargetRuntime-SelfContained.zip"
      }
      else
      {
        "SFP_UI-$TargetRuntime-net7.zip"
      }
      Get-ChildItem "$publishDir/*" -Recurse -Exclude $excludeFiles | Compress-Archive -DestinationPath "./$configuration/$zipname" -Force
    } else {
      $zipname = if ($selfContained)
      {
        "SFP_UI-$TargetRuntime-SelfContained.tar.gz"
      }
      else
      {
        "SFP_UI-$TargetRuntime-net7.tar.gz"
      }
      $excludeOptions = ""
      foreach ($file in $excludeFiles) {
        $excludeOptions += "--exclude=""$file"" "
      }
      $output = if ($TargetRuntime.StartsWith("osx"))
      {
        "SFP_UI.app"
      }
      else
      {
        "."
      }
      $tarCommand = "tar $excludeOptions -czvf ""./$configuration/$zipname"" -C ""$publishDir"" $output"
      Invoke-Expression $tarCOmmand
    }
  }
}

foreach ($currentOS in $os.Split(";"))
{
  $targetRuntime = "$currentOS-$arch"
  switch ( $selfcontained.ToLower())
  {
    { @("yes", "true", "1") -contains $_ } {
      $selfcontainedValues = $True
    }
    { @("no", "false", "0") -contains $_ } {
      $selfcontainedValues = $False
    }
    "both" {
      $selfcontainedValues = $False, $True
    }
  }

  foreach ($value in $selfcontainedValues)
  {
    Build-SFP -TargetRuntime $targetRuntime -selfContained $value
  }
}
