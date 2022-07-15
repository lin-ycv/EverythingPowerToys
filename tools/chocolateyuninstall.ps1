$ErrorActionPreference = 'Stop'; # stop on all errors
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  ZipFileName = "Everything-_VERSION_-x64.zip"
}

Uninstall-ChocolateyZipPackage @packageArgs