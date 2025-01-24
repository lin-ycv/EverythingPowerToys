# Everything SDK Libraries

Everything dll's are not included as part of the installer as of v0.87.1, this is to reduce AV FP.  
On startup of the EPT, it'll check if `Everything64.dll` exists in the root folder, if not, it'll prompt you to download automatically from this folder.  
Click Yes and it'll automatically download.  
Click No and it won't download, but also won't load EPT into PTR. Use this option if you want to download the DLL yourself.

Source of files are all from voidtools:
- `Everthing64.dll` (Accessed 2025/01/08) - https://www.voidtools.com/support/everything/sdk/
- `EverythingARM64.dll` (Accessed 2025/01/08) - https://www.voidtools.com/forum/viewtopic.php?p=57654#p57654
- `Everything3_x64.dll` (Accessed 2025/01/08) - https://www.voidtools.com/forum/viewtopic.php?t=15853

Everything3 is SDK version 3, which is explicitly for Everything 1.5, and not compatible with 1.4.