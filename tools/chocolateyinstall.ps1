
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://github.com/lin-ycv/EverythingPowerToys/releases/download/vx.xx.x/Everything-x.xx.x-x64.zip'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = "C:\\Program Files\\PowerToys\\modules\\launcher\\Plugins\\"
  url           = $url

  softwareName  = 'EverythingPowerToys*'

  checksum      = 'xxx'
  checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs