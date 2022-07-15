
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = '_URL_'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = "C:\\Program Files\\PowerToys\\modules\\launcher\\Plugins\\"
  url           = $url

  softwareName  = 'EverythingPowerToys*'

  checksum      = '_CRC_'
  checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs