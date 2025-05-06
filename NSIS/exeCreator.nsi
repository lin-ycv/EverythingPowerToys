!include "MUI2.nsh"
!define EPT "EverythingPT"
!define SMWCUE "Software\Microsoft\Windows\CurrentVersion\Uninstall\EverythingPowerToys"
;--------------------------------
;General
  Name "${EPT}"
  BrandingText "v${ver} ${platform}"
  SetCompressor zlib
  OutFile ".\..\bin\${EPT}-${ver}-${platform}.exe"
  Unicode True
  RequestExecutionLevel user
  SetOverwrite ifnewer
  InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything"
  FileErrorText "Can't access: $\r$\n$\r$\n$0$\r$\n$\r$\nPowerToys is probably still running, please close it and retry."
;--------------------------------
;Interface Settings
  !define MUI_ICON "Everything.ico"
  !define MUI_ABORTWARNING

  !define MUI_UNICON "Everything.ico"
  !define MUI_UNABORTWARNING
;--------------------------------
;Pages
  !insertmacro MUI_PAGE_LICENSE "..\LICENSE"
  !insertmacro MUI_PAGE_INSTFILES

  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES

  !insertmacro MUI_LANGUAGE "English"
;--------------------------------
;Version Information
  VIProductVersion "${ver}.0"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${EPT} Setup"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "Everything search plugin for PowerToys Run"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${ver}"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${ver}"
;--------------------------------
Section ""
  SetOutPath $INSTDIR
  File /r "${direct}\*"

  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKCU "${SMWCUE}" "DisplayIcon" "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "${SMWCUE}" "DisplayName" "${EPT} (${platform})"
  WriteRegStr HKCU "${SMWCUE}" "DisplayVersion" "${ver}"
  WriteRegStr HKCU "${SMWCUE}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${SMWCUE}" "Publisher" "Lin-ycv"

  WriteRegStr HKCU "${SMWCUE}" "UninstallString" "$INSTDIR\uninstall.exe"
  WriteRegDWORD HKCU "${SMWCUE}" "NoModify" 1
  WriteRegDWORD HKCU "${SMWCUE}" "NoRepair" 1

  IfErrors 0 +5
  SetErrorlevel 1
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Unable to install, PowerToys is probably still running, please close it manually before install."
  Abort
  
  SetErrorlevel 0
SectionEnd
;--------------------------------
Section "Uninstall"
    
  MessageBox MB_YESNO "This'll forcibly close PowerToys and remove all traces of EverythingPowerToys$\r$\nAre you sure you want to continue?" /SD IDYES IDYES true IDNO false

  false:
  DetailPrint "Uninstall cancelled, all files remains intact"
  Abort

  true:
  ExecWait `taskkill /im PowerToys.exe /f /t`
  Sleep 5000
  rmdir /r "$INSTDIR"
  rmdir /r "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Settings\Plugins\Community.PowerToys.Run.Plugin.Everything"
  DeleteRegKey HKCU "${SMWCUE}"
  
  IfErrors 0 +4
  SetErrorlevel 0
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Some file couldn't be removed, PowerToys is probably still running.$\r$\n$\r$\nCheck for leftover files in $INSTDIR"

  SetErrorlevel 0
SectionEnd