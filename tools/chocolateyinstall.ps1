stop-process -Name "PowerToys"

$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = '_URL_'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = "$env:LOCALAPPDATA\\Microsoft\\PowerToys\\PowerToys Run\\Plugins\\"
  url           = $url

  softwareName  = 'EverythingPowerToys*'

  checksum      = '_CRC_'
  checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs
