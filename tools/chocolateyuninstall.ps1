$ErrorActionPreference = 'Stop'; # stop on all errors
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  ZipFileName = "Everything-x.xx.x-x64.zip"
}

Uninstall-ChocolateyZipPackage @packageArgs