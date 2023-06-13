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
    [bool]$selfContained = $True,
    [bool]$bundle = $False
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
  dotnet publish "SFP_UI/SFP_UI.csproj" --configuration $configuration --output $configuration/publish --runtime $TargetRuntime @selfContainedFlag
  if ($TargetRuntime.StartsWith("osx") -and $bundle)
  {
    New-Item -Path "$configuration/publish/SFP_UI.app/Contents/Resources" -ItemType Directory -Force
    New-Item -Path "$configuration/publish/SFP_UI.app/Contents/MacOS" -ItemType Directory -Force
    Copy-Item -Path "./SFP_UI/RawAssets/SFP-logo.icns" -Destination "$configuration/publish/SFP_UI.app/Contents/Resources/SFP-logo.icns" -Force
    Copy-Item -Path "./SFP_UI/RawAssets/Info.plist" -Destination "$configuration/publish/SFP_UI.app/Contents/Info.plist" -Force
    Get-ChildItem "$configuration/publish/*" -Exclude "SFP_UI.app" | Move-Item -Destination "$configuration/publish/SFP_UI.app/Contents/MacOS" -Force
    if ($IsMacOS)
    {
     xattr -c "$configuration/publish/SFP_UI.app"
    }
  }
  if ($createzip)
  {
    $excludeFiles = "*.log", "*.pdb", "*.config"
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
      $output = if ($TargetRuntime.StartsWith("osx") -and $bundle)
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
