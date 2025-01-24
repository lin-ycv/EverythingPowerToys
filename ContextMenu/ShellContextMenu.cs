using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

#pragma warning disable CA1051
#pragma warning disable CA1401
#pragma warning disable SA1123
#pragma warning disable SA1310
#pragma warning disable SA1401
#pragma warning disable SA1402
#pragma warning disable SA1513
#pragma warning disable SA1514
#pragma warning disable SA1515
#pragma warning disable SA1617
#pragma warning disable SA1642
#pragma warning disable SA1643
namespace Community.PowerToys.Run.Plugin.Everything.ContextMenu
{
    /// <summary>
    /// "Stand-alone" shell context menu
    ///
    /// It isn't really debugged but is mostly working.
    /// Create an instance and call ShowContextMenu with a list of FileInfo for the files.
    /// Limitation is that it only handles files in the same directory but it can be fixed
    /// by changing the way files are translated into PIDLs.
    ///
    /// Based on FileBrowser in C# from CodeProject
    /// http://www.codeproject.com/useritems/FileBrowser.asp
    ///
    /// Hooking class taken from MSDN Magazine Cutting Edge column
    /// http://msdn.microsoft.com/msdnmag/issues/02/10/CuttingEdge/
    ///
    /// Andreas Johansson
    /// afjohansson@hotmail.com
    /// http://afjohansson.spaces.live.com
    ///
    /// Revised by Victor Yu Chieh Lin for EverythingPowerToys
    /// </summary>
    /// <example>
    ///    ShellContextMenu scm = new();
    ///    FileInfo file = new FileInfo(@"c:\windows\notepad.exe");
    ///    scm.ShowContextMenu(file, Cursor.Position);
    /// </example>
    public class ShellContextMenu : NativeWindow
    {
        #region Constructor
        /// <summary>Default constructor</summary>
        public ShellContextMenu()
        {
            CreateHandle(new CreateParams());
        }
        #endregion

        #region Destructor
        /// <summary>Ensure all resources get released</summary>
        ~ShellContextMenu()
        {
            ReleaseAll();
        }
        #endregion

        #region GetContextMenuInterfaces()
        /// <summary>Gets the interfaces to the context menu</summary>
        /// <param name="oParentFolder">Parent folder</param>
        /// <param name="pild">PIDLs</param>
        /// <returns>true if it got the interfaces, otherwise false</returns>
        private bool GetContextMenuInterfaces(IShellFolder oParentFolder, nint pild, out nint ctxMenuPtr)
        {
            var nResult = oParentFolder.GetUIObjectOf(
                nint.Zero,
                1U,
                [pild],
                ref iID_IContextMenu,
                nint.Zero,
                out ctxMenuPtr);

            if (nResult == S_OK)
            {
                _oContextMenu = (IContextMenu)Marshal.GetTypedObjectForIUnknown(ctxMenuPtr, typeof(IContextMenu));

                return true;
            }

            ctxMenuPtr = nint.Zero;
            _oContextMenu = null;
            return false;
        }
        #endregion

        #region Override

        /// <summary>
        /// This method receives WindowMessages. It will make the "Open With" and "Send To" work
        /// by calling HandleMenuMsg and HandleMenuMsg2. It will also call the OnContextMenuMouseHover
        /// method of Browser when hovering over a ContextMenu item.
        /// </summary>
        /// <param name="m">the Message of the Browser's WndProc</param>
        /// <returns>true if the message has been handled, false otherwise</returns>
        protected override void WndProc(ref Message m)
        {
            #region IContextMenu2

            if (_oContextMenu2 != null &&
                (m.Msg == (int)WM.INITMENUPOPUP ||
                 m.Msg == (int)WM.MEASUREITEM ||
                 m.Msg == (int)WM.DRAWITEM))
            {
                if (_oContextMenu2.HandleMenuMsg(
                    (uint)m.Msg, m.WParam, m.LParam) == S_OK)
                    return;
            }

            #endregion

            #region IContextMenu3

            if (_oContextMenu3 != null &&
                m.Msg == (int)WM.MENUCHAR)
            {
                if (_oContextMenu3.HandleMenuMsg2(
                    (uint)m.Msg, m.WParam, m.LParam, nint.Zero) == S_OK)
                    return;
            }

            #endregion

            base.WndProc(ref m);
        }

        #endregion

        #region InvokeCommand
        private void InvokeCommand(IContextMenu oContextMenu, uint nCmd, string strFolder, Point pointInvoke)
        {
            var invoke = new CMINVOKECOMMANDINFOEX
            {
                CbSize = CbInvokeCommand,
                LpVerb = (nint)(nCmd - CMD_FIRST),
                LpDirectory = strFolder,
                LpVerbW = (nint)(nCmd - CMD_FIRST),
                LpDirectoryW = strFolder,
                FMask = CMIC.UNICODE | CMIC.PTINVOKE |
                ((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
                ((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0),
                PtInvoke = new POINT(pointInvoke.X, pointInvoke.Y),
                NShow = SW.SHOWNORMAL,
            };

            oContextMenu.InvokeCommand(ref invoke);
        }
        #endregion

        #region ReleaseAll()
        /// <summary>
        /// Release all allocated interfaces, PIDLs
        /// </summary>
        private void ReleaseAll()
        {
            if (_oContextMenu != null)
            {
                Marshal.ReleaseComObject(_oContextMenu);
                _oContextMenu = null;
            }
            if (_oContextMenu2 != null)
            {
                Marshal.ReleaseComObject(_oContextMenu2);
                _oContextMenu2 = null;
            }
            if (_oContextMenu3 != null)
            {
                Marshal.ReleaseComObject(_oContextMenu3);
                _oContextMenu3 = null;
            }
            if (_oDesktopFolder != null)
            {
                Marshal.ReleaseComObject(_oDesktopFolder);
                _oDesktopFolder = null;
            }
            if (_oParentFolder != null)
            {
                Marshal.ReleaseComObject(_oParentFolder);
                _oParentFolder = null;
            }
            if (_pidl != nint.Zero)
            {
                FreePIDLs(_pidl);
                _pidl = nint.Zero;
            }
        }
        #endregion

        #region GetDesktopFolder()
        /// <summary>
        /// Gets the desktop folder
        /// </summary>
        /// <returns>IShellFolder for desktop folder</returns>
        private IShellFolder GetDesktopFolder()
        {
            if (_oDesktopFolder == null)
            {
                // Get desktop IShellFolder
                var nResult = SHGetDesktopFolder(out var pUnkownDesktopFolder);
                if (nResult != S_OK)
                {
                    throw new ShellContextMenuException("Failed to get the desktop shell folder");
                }
                _oDesktopFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(pUnkownDesktopFolder, typeof(IShellFolder));
            }

            return _oDesktopFolder;
        }
        #endregion

        #region GetParentFolder()
        /// <summary>
        /// Gets the parent folder
        /// </summary>
        /// <param name="folderName">Folder path</param>
        /// <returns>IShellFolder for the folder (relative from the desktop)</returns>
        private IShellFolder GetParentFolder(string folderName)
        {
            if (_oParentFolder == null)
            {
                var oDesktopFolder = GetDesktopFolder();
                if (oDesktopFolder == null)
                {
                    return null;
                }

                // Get the PIDL for the folder file is in
                uint pchEaten = 0;
                SFGAO pdwAttributes = 0;
                var nResult = oDesktopFolder.ParseDisplayName(nint.Zero, nint.Zero, folderName, ref pchEaten, out var pPIDL, ref pdwAttributes);
                if (nResult != S_OK)
                {
                    return null;
                }

                var pStrRet = Marshal.AllocCoTaskMem((MAX_PATH * 2) + 4);
                Marshal.WriteInt32(pStrRet, 0, 0);
                _ = _oDesktopFolder.GetDisplayNameOf(pPIDL, SHGNO.FORPARSING, pStrRet);
                var strFolder = new StringBuilder(MAX_PATH);
                _ = StrRetToBuf(pStrRet, pPIDL, strFolder, MAX_PATH);
                Marshal.FreeCoTaskMem(pStrRet);
                _strParentFolder = strFolder.ToString();

                // Get the IShellFolder for folder
                nResult = oDesktopFolder.BindToObject(pPIDL, nint.Zero, ref iID_IShellFolder, out var pUnknownParentFolder);
                // Free the PIDL first
                Marshal.FreeCoTaskMem(pPIDL);
                if (nResult != S_OK)
                {
                    return null;
                }
                _oParentFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(pUnknownParentFolder, typeof(IShellFolder));
            }

            return _oParentFolder;
        }
        #endregion

        #region GetPIDL()
        /// <summary>
        /// Get the PIDLs
        /// </summary>
        /// <param name="fi">Array of FileInfo</param>
        /// <returns>Array of PIDLs</returns>
        protected nint GetPIDL(FileInfo fi)
        {
            if (fi == null || !fi.Exists)
            {
                return nint.Zero;
            }

            var oParentFolder = GetParentFolder(fi.DirectoryName);
            if (oParentFolder == null)
            {
                return nint.Zero;
            }

            nint pidl = nint.Zero;
            // Get the file relative to folder
            uint pchEaten = 0;
            SFGAO pdwAttributes = 0;
            var nResult = oParentFolder.ParseDisplayName(nint.Zero, nint.Zero, fi.Name, ref pchEaten, out nint ppidl, ref pdwAttributes);
            if (nResult != S_OK)
            {
                FreePIDLs(pidl);
                return nint.Zero;
            }
            pidl = ppidl;

            return pidl;
        }

        /// <summary>
        /// Get the PIDLs
        /// </summary>
        /// <param name="di">DirectoryInfo</param>
        /// <returns>PIDL</returns>
        protected nint GetPIDL(DirectoryInfo di)
        {
            if (di == null || !di.Exists)
            {
                return nint.Zero;
            }

            var oParentFolder = GetParentFolder(di.Parent.FullName);
            if (oParentFolder == null)
            {
                return nint.Zero;
            }

            nint pidl = nint.Zero;

            uint pchEaten = 0;
            SFGAO pdwAttributes = 0;
            var nResult = oParentFolder.ParseDisplayName(nint.Zero, nint.Zero, di.Name, ref pchEaten, out nint pPIDL, ref pdwAttributes);
            if (nResult != S_OK)
            {
                FreePIDLs(pidl);
                return nint.Zero;
            }
            pidl = pPIDL;

            return pidl;
        }
        #endregion

        #region FreePIDLs()
        /// <summary>
        /// Free the PIDLs
        /// </summary>
        /// <param name="pidl">PIDL (IntPtr)</param>
        protected void FreePIDLs(nint pidl)
        {
            if (pidl != nint.Zero)
            {
                Marshal.FreeCoTaskMem(pidl);
                pidl = nint.Zero;
            }
        }
        #endregion

        #region ShowContextMenu()

        /// <summary>
        /// Shows the context menu
        /// </summary>
        /// <param name="file">FileInfos</param>
        /// <param name="pointScreen">Where to show the menu</param>
        public void ShowContextMenu(FileInfo file, Point pointScreen)
        {
            // Release all resources first.
            ReleaseAll();
            _pidl = GetPIDL(file);
            ShowContextMenu(pointScreen);
        }

        /// <summary>
        /// Shows the context menu
        /// </summary>
        /// <param name="dir">DirectoryInfos (should all be in same directory)</param>
        /// <param name="pointScreen">Where to show the menu</param>
        public void ShowContextMenu(DirectoryInfo dir, Point pointScreen)
        {
            // Release all resources first.
            ReleaseAll();
            _pidl = GetPIDL(dir);
            ShowContextMenu(pointScreen);
        }

        /// <summary>
        /// Shows the context menu
        /// </summary>
        /// <param name="pointScreen">Where to show the menu</param>
        private void ShowContextMenu(Point pointScreen)
        {
            nint pMenu = nint.Zero,
                iContextMenuPtr = nint.Zero,
                iContextMenuPtr2 = nint.Zero,
                iContextMenuPtr3 = nint.Zero;

            try
            {
                if (_pidl == nint.Zero)
                {
                    ReleaseAll();
                    return;
                }

                if (!GetContextMenuInterfaces(_oParentFolder, _pidl, out iContextMenuPtr))
                {
                    ReleaseAll();
                    return;
                }

                pMenu = CreatePopupMenu();

                _oContextMenu.QueryContextMenu(
                    pMenu,
                    0,
                    CMD_FIRST,
                    CMD_LAST,
                    CMF.EXPLORE | CMF.NORMAL | CMF.EXTENDEDVERBS);

                Marshal.QueryInterface(iContextMenuPtr, in iID_IContextMenu2, out iContextMenuPtr2);
                Marshal.QueryInterface(iContextMenuPtr, in iID_IContextMenu3, out iContextMenuPtr3);

                _oContextMenu2 = (IContextMenu2)Marshal.GetTypedObjectForIUnknown(iContextMenuPtr2, typeof(IContextMenu2));
                _oContextMenu3 = (IContextMenu3)Marshal.GetTypedObjectForIUnknown(iContextMenuPtr3, typeof(IContextMenu3));

                var nSelected = TrackPopupMenuEx(
                    pMenu,
                    TPM.RETURNCMD,
                    pointScreen.X,
                    pointScreen.Y,
                    Handle,
                    nint.Zero);

                DestroyMenu(pMenu);
                pMenu = nint.Zero;

                if (nSelected != 0)
                {
                    InvokeCommand(_oContextMenu, nSelected, _strParentFolder, pointScreen);
                }
            }
            finally
            {
                //hook.Uninstall();
                if (pMenu != nint.Zero)
                {
                    DestroyMenu(pMenu);
                }

                if (iContextMenuPtr != nint.Zero)
                    Marshal.Release(iContextMenuPtr);

                if (iContextMenuPtr2 != nint.Zero)
                    Marshal.Release(iContextMenuPtr2);

                if (iContextMenuPtr3 != nint.Zero)
                    Marshal.Release(iContextMenuPtr3);

                ReleaseAll();
            }
        }
        #endregion

        #region Local variabled
        private IContextMenu _oContextMenu;
        private IContextMenu2 _oContextMenu2;
        private IContextMenu3 _oContextMenu3;
        private IShellFolder _oDesktopFolder;
        private IShellFolder _oParentFolder;
        private nint _pidl;
        private string _strParentFolder;
        #endregion

        #region Variables and Constants

        private const int MAX_PATH = 260;
        private const uint CMD_FIRST = 1;
        private const uint CMD_LAST = 30000;

        private const int S_OK = 0;
        private static readonly int CbMenuItemInfo = Marshal.SizeOf<MENUITEMINFO>();
        private static readonly int CbInvokeCommand = Marshal.SizeOf<CMINVOKECOMMANDINFOEX>();

        #endregion

        #region DLL Import

        // Retrieves the IShellFolder interface for the desktop folder, which is the root of the Shell's namespace.
        [DllImport("shell32.dll")]
        private static extern int SHGetDesktopFolder(out nint ppshf);

        // Takes a STRRET structure returned by IShellFolder::GetDisplayNameOf, converts it to a string, and places the result in a buffer.
        [DllImport("shlwapi.dll", EntryPoint = "StrRetToBuf", ExactSpelling = false, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int StrRetToBuf(nint pstr, nint pidl, StringBuilder pszBuf, int cchBuf);

        // The TrackPopupMenuEx function displays a shortcut menu at the specified location and tracks the selection of items on the shortcut menu. The shortcut menu can appear anywhere on the screen.
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern uint TrackPopupMenuEx(nint hmenu, TPM flags, int x, int y, nint hwnd, nint lptpm);

        // The CreatePopupMenu function creates a drop-down menu, submenu, or shortcut menu. The menu is initially empty. You can insert or append menu items by using the InsertMenuItem function. You can also use the InsertMenu function to insert menu items and the AppendMenu function to append menu items.
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern nint CreatePopupMenu();

        // The DestroyMenu function destroys the specified menu and frees any memory that the menu occupies.
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DestroyMenu(nint hMenu);

        #endregion

        #region Shell GUIDs

        private static Guid iID_IShellFolder = new("{000214E6-0000-0000-C000-000000000046}");
        private static Guid iID_IContextMenu = new("{000214e4-0000-0000-c000-000000000046}");
        private static Guid iID_IContextMenu2 = new("{000214f4-0000-0000-c000-000000000046}");
        private static Guid iID_IContextMenu3 = new("{bcfce0a0-ec17-11d0-8d10-00a0c90f2719}");

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        private struct CWPSTRUCT
        {
            public nint Lparam;
            public nint Wparam;
            public int Message;
            public nint Hwnd;
        }

        // Contains extended information about a shortcut menu command
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CMINVOKECOMMANDINFOEX
        {
            public int CbSize;
            public CMIC FMask;
            public nint Hwnd;
            public nint LpVerb;
            [MarshalAs(UnmanagedType.LPStr)]
            public string LpParameters;
            [MarshalAs(UnmanagedType.LPStr)]
            public string LpDirectory;
            public SW NShow;
            public int DwHotKey;
            public nint HIcon;
            [MarshalAs(UnmanagedType.LPStr)]
            public string LpTitle;
            public nint LpVerbW;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LpParametersW;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LpDirectoryW;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LpTitleW;
            public POINT PtInvoke;
        }

        // Contains information about a menu item
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MENUITEMINFO
        {
            public MENUITEMINFO(string text)
            {
                CbSize = CbMenuItemInfo;
                DwTypeData = text;
                Cch = text.Length;
                FMask = 0;
                FType = 0;
                FState = 0;
                WID = 0;
                HSubMenu = nint.Zero;
                HbmpChecked = nint.Zero;
                HbmpUnchecked = nint.Zero;
                DwItemData = nint.Zero;
                HbmpItem = nint.Zero;
            }

            public int CbSize;
            public MIIM FMask;
            public MFT FType;
            public MFS FState;
            public uint WID;
            public nint HSubMenu;
            public nint HbmpChecked;
            public nint HbmpUnchecked;
            public nint DwItemData;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string DwTypeData;
            public int Cch;
            public nint HbmpItem;
        }

        // A generalized global memory handle used for data transfer operations by the
        // IAdviseSink, IDataObject, and IOleCache interfaces
        [StructLayout(LayoutKind.Sequential)]
        private struct STGMEDIUM
        {
            public TYMED Tymed;
            public nint HBitmap;
            public nint HMetaFilePict;
            public nint HEnhMetaFile;
            public nint HGlobal;
            public nint LpszFileName;
            public nint Pstm;
            public nint Pstg;
            public nint PUnkForRelease;
        }

        // Defines the x- and y-coordinates of a point
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct POINT
        {
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X;
            public int Y;
        }

        #endregion

        #region Enums

        // Defines the values used with the IShellFolder::GetDisplayNameOf and IShellFolder::SetNameOf
        // methods to specify the type of file or folder names used by those methods
        [Flags]
        private enum SHGNO
        {
            NORMAL = 0x0000,
            INFOLDER = 0x0001,
            FOREDITING = 0x1000,
            FORADDRESSBAR = 0x4000,
            FORPARSING = 0x8000,
        }

        // The attributes that the caller is requesting, when calling IShellFolder::GetAttributesOf
        [Flags]
        private enum SFGAO : uint
        {
            BROWSABLE = 0x8000000,
            CANCOPY = 1,
            CANDELETE = 0x20,
            CANLINK = 4,
            CANMONIKER = 0x400000,
            CANMOVE = 2,
            CANRENAME = 0x10,
            CAPABILITYMASK = 0x177,
            COMPRESSED = 0x4000000,
            CONTENTSMASK = 0x80000000,
            DISPLAYATTRMASK = 0xfc000,
            DROPTARGET = 0x100,
            ENCRYPTED = 0x2000,
            FILESYSANCESTOR = 0x10000000,
            FILESYSTEM = 0x40000000,
            FOLDER = 0x20000000,
            GHOSTED = 0x8000,
            HASPROPSHEET = 0x40,
            HASSTORAGE = CANMONIKER,
            HASSUBFOLDER = CONTENTSMASK,
            HIDDEN = 0x80000,
            ISSLOW = 0x4000,
            LINK = 0x10000,
            NEWCONTENT = 0x200000,
            NONENUMERATED = 0x100000,
            READONLY = 0x40000,
            REMOVABLE = 0x2000000,
            SHARE = 0x20000,
            STORAGE = 8,
            STORAGEANCESTOR = 0x800000,
            STORAGECAPMASK = 0x70c50008,
            STREAM = CANMONIKER,
            VALIDATE = 0x1000000,
        }

        // Determines the type of items included in an enumeration.
        // These values are used with the IShellFolder::EnumObjects method
        [Flags]
        private enum SHCONTF
        {
            FOLDERS = 0x0020,
            NONFOLDERS = 0x0040,
            INCLUDEHIDDEN = 0x0080,
            INIT_ON_FIRST_NEXT = 0x0100,
            NETPRINTERSRCH = 0x0200,
            SHAREABLE = 0x0400,
            STORAGE = 0x0800,
        }

        // Specifies how the shortcut menu can be changed when calling IContextMenu::QueryContextMenu
        [Flags]
        private enum CMF : uint
        {
            NORMAL = 0x00000000,
            DEFAULTONLY = 0x00000001,
            VERBSONLY = 0x00000002,
            EXPLORE = 0x00000004,
            NOVERBS = 0x00000008,
            CANRENAME = 0x00000010,
            NODEFAULT = 0x00000020,
            INCLUDESTATIC = 0x00000040,
            EXTENDEDVERBS = 0x00000100,
            RESERVED = 0xffff0000,
        }

        // Flags specifying the information to return when calling IContextMenu::GetCommandString
        [Flags]
        private enum GCS : uint
        {
            VERBA = 0,
            HELPTEXTA = 1,
            VALIDATEA = 2,
            VERBW = 4,
            HELPTEXTW = 5,
            VALIDATEW = 6,
        }

        // Specifies how TrackPopupMenuEx positions the shortcut menu horizontally
        [Flags]
        private enum TPM : uint
        {
            LEFTBUTTON = 0x0000,
            RIGHTBUTTON = 0x0002,
            LEFTALIGN = LEFTBUTTON,
            CENTERALIGN = 0x0004,
            RIGHTALIGN = 0x0008,
            TOPALIGN = LEFTBUTTON,
            VCENTERALIGN = 0x0010,
            BOTTOMALIGN = 0x0020,
            HORIZONTAL = LEFTBUTTON,
            VERTICAL = 0x0040,
            NONOTIFY = 0x0080,
            RETURNCMD = 0x0100,
            RECURSE = 0x0001,
            HORPOSANIMATION = 0x0400,
            HORNEGANIMATION = 0x0800,
            VERPOSANIMATION = 0x1000,
            VERNEGANIMATION = 0x2000,
            NOANIMATION = 0x4000,
            LAYOUTRTL = 0x8000,
        }

        // The cmd for a custom added menu item
        private enum CMD_CUSTOM
        {
            ExpandCollapse = (int)CMD_LAST + 1,
        }

        // Flags used with the CMINVOKECOMMANDINFOEX structure
        [Flags]
        private enum CMIC : uint
        {
            HOTKEY = 0x00000020,
            ICON = 0x00000010,
            FLAG_NO_UI = 0x00000400,
            UNICODE = 0x00004000,
            NO_CONSOLE = 0x00008000,
            ASYNCOK = 0x00100000,
            NOZONECHECKS = 0x00800000,
            SHIFT_DOWN = 0x10000000,
            CONTROL_DOWN = 0x40000000,
            FLAG_LOG_USAGE = 0x04000000,
            PTINVOKE = 0x20000000,
        }

        // Specifies how the window is to be shown
        [Flags]
        private enum SW
        {
            HIDE = 0,
            SHOWNORMAL = 1,
            NORMAL = SHOWNORMAL,
            SHOWMINIMIZED = 2,
            SHOWMAXIMIZED = 3,
            MAXIMIZE = SHOWMAXIMIZED,
            SHOWNOACTIVATE = 4,
            SHOW = 5,
            MINIMIZE = 6,
            SHOWMINNOACTIVE = 7,
            SHOWNA = 8,
            RESTORE = 9,
            SHOWDEFAULT = 10,
        }

        // Window message flags
        [Flags]
        private enum WM : uint
        {
            ACTIVATE = 0x6,
            ACTIVATEAPP = 0x1C,
            AFXFIRST = 0x360,
            AFXLAST = 0x37F,
            APP = 0x8000,
            ASKCBFORMATNAME = 0x30C,
            CANCELJOURNAL = 0x4B,
            CANCELMODE = 0x1F,
            CAPTURECHANGED = 0x215,
            CHANGECBCHAIN = 0x30D,
            CHAR = 0x102,
            CHARTOITEM = 0x2F,
            CHILDACTIVATE = 0x22,
            CLEAR = 0x303,
            CLOSE = 0x10,
            COMMAND = 0x111,
            COMPACTING = 0x41,
            COMPAREITEM = 0x39,
            CONTEXTMENU = 0x7B,
            COPY = 0x301,
            COPYDATA = 0x4A,
            CREATE = 0x1,
            CTLCOLORBTN = 0x135,
            CTLCOLORDLG = 0x136,
            CTLCOLOREDIT = 0x133,
            CTLCOLORLISTBOX = 0x134,
            CTLCOLORMSGBOX = 0x132,
            CTLCOLORSCROLLBAR = 0x137,
            CTLCOLORSTATIC = 0x138,
            CUT = 0x300,
            DEADCHAR = 0x103,
            DELETEITEM = 0x2D,
            DESTROY = 0x2,
            DESTROYCLIPBOARD = 0x307,
            DEVICECHANGE = 0x219,
            DEVMODECHANGE = 0x1B,
            DISPLAYCHANGE = 0x7E,
            DRAWCLIPBOARD = 0x308,
            DRAWITEM = 0x2B,
            DROPFILES = 0x233,
            ENABLE = 0xA,
            ENDSESSION = 0x16,
            ENTERIDLE = 0x121,
            ENTERMENULOOP = 0x211,
            ENTERSIZEMOVE = 0x231,
            ERASEBKGND = 0x14,
            EXITMENULOOP = 0x212,
            EXITSIZEMOVE = 0x232,
            FONTCHANGE = 0x1D,
            GETDLGCODE = 0x87,
            GETFONT = 0x31,
            GETHOTKEY = 0x33,
            GETICON = 0x7F,
            GETMINMAXINFO = 0x24,
            GETOBJECT = 0x3D,
            GETSYSMENU = 0x313,
            GETTEXT = 0xD,
            GETTEXTLENGTH = 0xE,
            HANDHELDFIRST = 0x358,
            HANDHELDLAST = 0x35F,
            HELP = 0x53,
            HOTKEY = 0x312,
            HSCROLL = 0x114,
            HSCROLLCLIPBOARD = 0x30E,
            ICONERASEBKGND = 0x27,
            IME_CHAR = 0x286,
            IME_COMPOSITION = 0x10F,
            IME_COMPOSITIONFULL = 0x284,
            IME_CONTROL = 0x283,
            IME_ENDCOMPOSITION = 0x10E,
            IME_KEYDOWN = 0x290,
            IME_KEYLAST = IME_COMPOSITION,
            IME_KEYUP = 0x291,
            IME_NOTIFY = 0x282,
            IME_REQUEST = 0x288,
            IME_SELECT = 0x285,
            IME_SETCONTEXT = 0x281,
            IME_STARTCOMPOSITION = 0x10D,
            INITDIALOG = 0x110,
            INITMENU = 0x116,
            INITMENUPOPUP = 0x117,
            INPUTLANGCHANGE = 0x51,
            INPUTLANGCHANGEREQUEST = 0x50,
            KEYDOWN = 0x100,
            KEYFIRST = KEYDOWN,
            KEYLAST = 0x108,
            KEYUP = 0x101,
            KILLFOCUS = 0x8,
            LBUTTONDBLCLK = 0x203,
            LBUTTONDOWN = 0x201,
            LBUTTONUP = 0x202,
            LVM_GETEDITCONTROL = 0x1018,
            LVM_SETIMAGELIST = 0x1003,
            MBUTTONDBLCLK = 0x209,
            MBUTTONDOWN = 0x207,
            MBUTTONUP = 0x208,
            MDIACTIVATE = 0x222,
            MDICASCADE = 0x227,
            MDICREATE = 0x220,
            MDIDESTROY = 0x221,
            MDIGETACTIVE = 0x229,
            MDIICONARRANGE = 0x228,
            MDIMAXIMIZE = 0x225,
            MDINEXT = 0x224,
            MDIREFRESHMENU = 0x234,
            MDIRESTORE = 0x223,
            MDISETMENU = 0x230,
            MDITILE = 0x226,
            MEASUREITEM = 0x2C,
            MENUCHAR = 0x120,
            MENUCOMMAND = 0x126,
            MENUDRAG = 0x123,
            MENUGETOBJECT = 0x124,
            MENURBUTTONUP = 0x122,
            MENUSELECT = 0x11F,
            MOUSEACTIVATE = 0x21,
            MOUSEFIRST = 0x200,
            MOUSEHOVER = 0x2A1,
            MOUSELAST = 0x20A,
            MOUSELEAVE = 0x2A3,
            MOUSEMOVE = MOUSEFIRST,
            MOUSEWHEEL = MOUSELAST,
            MOVE = 0x3,
            MOVING = 0x216,
            NCACTIVATE = 0x86,
            NCCALCSIZE = 0x83,
            NCCREATE = 0x81,
            NCDESTROY = 0x82,
            NCHITTEST = 0x84,
            NCLBUTTONDBLCLK = 0xA3,
            NCLBUTTONDOWN = 0xA1,
            NCLBUTTONUP = 0xA2,
            NCMBUTTONDBLCLK = 0xA9,
            NCMBUTTONDOWN = 0xA7,
            NCMBUTTONUP = 0xA8,
            NCMOUSEHOVER = 0x2A0,
            NCMOUSELEAVE = 0x2A2,
            NCMOUSEMOVE = 0xA0,
            NCPAINT = 0x85,
            NCRBUTTONDBLCLK = 0xA6,
            NCRBUTTONDOWN = 0xA4,
            NCRBUTTONUP = 0xA5,
            NEXTDLGCTL = 0x28,
            NEXTMENU = 0x213,
            NOTIFY = 0x4E,
            NOTIFYFORMAT = 0x55,
            NULL = 0x0,
            PAINT = 0xF,
            PAINTCLIPBOARD = 0x309,
            PAINTICON = 0x26,
            PALETTECHANGED = 0x311,
            PALETTEISCHANGING = 0x310,
            PARENTNOTIFY = 0x210,
            PASTE = 0x302,
            PENWINFIRST = 0x380,
            PENWINLAST = 0x38F,
            POWER = 0x48,
            PRINT = 0x317,
            PRINTCLIENT = 0x318,
            QUERYDRAGICON = 0x37,
            QUERYENDSESSION = 0x11,
            QUERYNEWPALETTE = 0x30F,
            QUERYOPEN = 0x13,
            QUEUESYNC = 0x23,
            QUIT = 0x12,
            RBUTTONDBLCLK = 0x206,
            RBUTTONDOWN = 0x204,
            RBUTTONUP = 0x205,
            RENDERALLFORMATS = 0x306,
            RENDERFORMAT = 0x305,
            SETCURSOR = 0x20,
            SETFOCUS = 0x7,
            SETFONT = 0x30,
            SETHOTKEY = 0x32,
            SETICON = 0x80,
            SETMARGINS = 0xD3,
            SETREDRAW = 0xB,
            SETTEXT = 0xC,
            SETTINGCHANGE = 0x1A,
            SHOWWINDOW = 0x18,
            SIZE = 0x5,
            SIZECLIPBOARD = 0x30B,
            SIZING = 0x214,
            SPOOLERSTATUS = 0x2A,
            STYLECHANGED = 0x7D,
            STYLECHANGING = 0x7C,
            SYNCPAINT = 0x88,
            SYSCHAR = 0x106,
            SYSCOLORCHANGE = 0x15,
            SYSCOMMAND = 0x112,
            SYSDEADCHAR = 0x107,
            SYSKEYDOWN = 0x104,
            SYSKEYUP = 0x105,
            TCARD = 0x52,
            TIMECHANGE = 0x1E,
            TIMER = 0x113,
            TVM_GETEDITCONTROL = 0x110F,
            TVM_SETIMAGELIST = 0x1109,
            UNDO = 0x304,
            UNINITMENUPOPUP = 0x125,
            USER = 0x400,
            USERCHANGED = 0x54,
            VKEYTOITEM = 0x2E,
            VSCROLL = 0x115,
            VSCROLLCLIPBOARD = 0x30A,
            WINDOWPOSCHANGED = 0x47,
            WINDOWPOSCHANGING = 0x46,
            WININICHANGE = SETTINGCHANGE,
            SH_NOTIFY = 0x0401,
        }

        // Specifies the content of the new menu item
        [Flags]
        private enum MFT : uint
        {
            GRAYED = 0x00000003,
            DISABLED = GRAYED,
            CHECKED = 0x00000008,
            SEPARATOR = 0x00000800,
            RADIOCHECK = 0x00000200,
            BITMAP = 0x00000004,
            OWNERDRAW = 0x00000100,
            MENUBARBREAK = 0x00000020,
            MENUBREAK = 0x00000040,
            RIGHTORDER = 0x00002000,
            BYCOMMAND = 0x00000000,
            BYPOSITION = 0x00000400,
            POPUP = 0x00000010,
        }

        // Specifies the state of the new menu item
        [Flags]
        private enum MFS : uint
        {
            GRAYED = 0x00000003,
            DISABLED = GRAYED,
            CHECKED = 0x00000008,
            HILITE = 0x00000080,
            ENABLED = 0x00000000,
            UNCHECKED = ENABLED,
            UNHILITE = ENABLED,
            DEFAULT = 0x00001000,
        }

        // Specifies the content of the new menu item
        [Flags]
        private enum MIIM : uint
        {
            BITMAP = 0x80,
            CHECKMARKS = 0x08,
            DATA = 0x20,
            FTYPE = 0x100,
            ID = 0x02,
            STATE = 0x01,
            STRING = 0x40,
            SUBMENU = 0x04,
            TYPE = 0x10,
        }

        // Indicates the type of storage medium being used in a data transfer
        [Flags]
        private enum TYMED
        {
            ENHMF = 0x40,
            FILE = 2,
            GDI = 0x10,
            HGLOBAL = 1,
            ISTORAGE = 8,
            ISTREAM = 4,
            MFPICT = 0x20,
            NULL = 0,
        }

        #endregion

        #region IShellFolder
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214E6-0000-0000-C000-000000000046")]
        private interface IShellFolder
        {
            // Translates a file object's or folder's display name into an item identifier list.
            // Return value: error code, if any
            [PreserveSig]
            int ParseDisplayName(
                nint hwnd,
                nint pbc,
                [MarshalAs(UnmanagedType.LPWStr)]
                string pszDisplayName,
                ref uint pchEaten,
                out nint ppidl,
                ref SFGAO pdwAttributes);

            // Allows a client to determine the contents of a folder by creating an item
            // identifier enumeration object and returning its IEnumIDList interface.
            // Return value: error code, if any
            [PreserveSig]
            int EnumObjects(
                nint hwnd,
                SHCONTF grfFlags,
                out nint enumIDList);

            // Retrieves an IShellFolder object for a subfolder.
            // Return value: error code, if any
            [PreserveSig]
            int BindToObject(
                nint pidl,
                nint pbc,
                ref Guid riid,
                out nint ppv);

            // Requests a pointer to an object's storage interface.
            // Return value: error code, if any
            [PreserveSig]
            int BindToStorage(
                nint pidl,
                nint pbc,
                ref Guid riid,
                out nint ppv);

            // Determines the relative order of two file objects or folders, given their
            // item identifier lists. Return value: If this method is successful, the
            // CODE field of the HRESULT contains one of the following values (the code
            // can be retrived using the helper function GetHResultCode): Negative A
            // negative return value indicates that the first item should precede
            // the second (pidl1 < pidl2).

            // Positive A positive return value indicates that the first item should
            // follow the second (pidl1 > pidl2).  Zero A return value of zero
            // indicates that the two items are the same (pidl1 = pidl2).
            [PreserveSig]
            int CompareIDs(
                nint lParam,
                nint pidl1,
                nint pidl2);

            // Requests an object that can be used to obtain information from or interact
            // with a folder object.
            // Return value: error code, if any
            [PreserveSig]
            int CreateViewObject(
                nint hwndOwner,
                Guid riid,
                out nint ppv);

            // Retrieves the attributes of one or more file objects or subfolders.
            // Return value: error code, if any
            [PreserveSig]
            int GetAttributesOf(
                uint cidl,
                [MarshalAs(UnmanagedType.LPArray)]
                nint[] apidl,
                ref SFGAO rgfInOut);

            // Retrieves an OLE interface that can be used to carry out actions on the
            // specified file objects or folders.
            // Return value: error code, if any
            [PreserveSig]
            int GetUIObjectOf(
                nint hwndOwner,
                uint cidl,
                [MarshalAs(UnmanagedType.LPArray)]
                nint[] apidl,
                ref Guid riid,
                nint rgfReserved,
                out nint ppv);

            // Retrieves the display name for the specified file object or subfolder.
            // Return value: error code, if any
            [PreserveSig]
            int GetDisplayNameOf(
                nint pidl,
                SHGNO uFlags,
                nint lpName);

            // Sets the display name of a file object or subfolder, changing the item
            // identifier in the process.
            // Return value: error code, if any
            [PreserveSig]
            int SetNameOf(
                nint hwnd,
                nint pidl,
                [MarshalAs(UnmanagedType.LPWStr)]
                string pszName,
                SHGNO uFlags,
                out nint ppidlOut);
        }
        #endregion

        #region IContextMenu
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214e4-0000-0000-c000-000000000046")]
        private interface IContextMenu
        {
            // Adds commands to a shortcut menu
            [PreserveSig]
            int QueryContextMenu(
                nint hmenu,
                uint iMenu,
                uint idCmdFirst,
                uint idCmdLast,
                CMF uFlags);

            // Carries out the command associated with a shortcut menu item
            [PreserveSig]
            int InvokeCommand(
                ref CMINVOKECOMMANDINFOEX info);

            // Retrieves information about a shortcut menu command,
            // including the help string and the language-independent,
            // or canonical, name for the command
            [PreserveSig]
            int GetCommandString(
                uint idcmd,
                GCS uflags,
                uint reserved,
                [MarshalAs(UnmanagedType.LPArray)]
                byte[] commandstring,
                int cch);
        }

        [ComImport]
        [Guid("000214f4-0000-0000-c000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IContextMenu2
        {
            // Adds commands to a shortcut menu
            [PreserveSig]
            int QueryContextMenu(
                nint hmenu,
                uint iMenu,
                uint idCmdFirst,
                uint idCmdLast,
                CMF uFlags);

            // Carries out the command associated with a shortcut menu item
            [PreserveSig]
            int InvokeCommand(
                ref CMINVOKECOMMANDINFOEX info);

            // Retrieves information about a shortcut menu command,
            // including the help string and the language-independent,
            // or canonical, name for the command
            [PreserveSig]
            int GetCommandString(
                uint idcmd,
                GCS uflags,
                uint reserved,
                [MarshalAs(UnmanagedType.LPWStr)]
                StringBuilder commandstring,
                int cch);

            // Allows client objects of the IContextMenu interface to
            // handle messages associated with owner-drawn menu items
            [PreserveSig]
            int HandleMenuMsg(
                uint uMsg,
                nint wParam,
                nint lParam);
        }

        [ComImport]
        [Guid("bcfce0a0-ec17-11d0-8d10-00a0c90f2719")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IContextMenu3
        {
            // Adds commands to a shortcut menu
            [PreserveSig]
            int QueryContextMenu(
                nint hmenu,
                uint iMenu,
                uint idCmdFirst,
                uint idCmdLast,
                CMF uFlags);

            // Carries out the command associated with a shortcut menu item
            [PreserveSig]
            int InvokeCommand(
                ref CMINVOKECOMMANDINFOEX info);

            // Retrieves information about a shortcut menu command,
            // including the help string and the language-independent,
            // or canonical, name for the command
            [PreserveSig]
            int GetCommandString(
                uint idcmd,
                GCS uflags,
                uint reserved,
                [MarshalAs(UnmanagedType.LPWStr)]
                StringBuilder commandstring,
                int cch);

            // Allows client objects of the IContextMenu interface to
            // handle messages associated with owner-drawn menu items
            [PreserveSig]
            int HandleMenuMsg(
                uint uMsg,
                nint wParam,
                nint lParam);

            // Allows client objects of the IContextMenu3 interface to
            // handle messages associated with owner-drawn menu items
            [PreserveSig]
            int HandleMenuMsg2(
                uint uMsg,
                nint wParam,
                nint lParam,
                nint plResult);
        }
        #endregion
    }

    #region ShellContextMenuException
    public class ShellContextMenuException : Exception
    {
        /// <summary>Default contructor</summary>
        public ShellContextMenuException()
        {
        }

        /// <summary>Constructor with message</summary>
        /// <param name="message">Message</param>
        public ShellContextMenuException(string message)
            : base(message)
        {
        }
    }
    #endregion

    #region Class HookEventArgs
    public class HookEventArgs : EventArgs
    {
        public int HookCode;    // Hook code
        public nint WParam;   // WPARAM argument
        public nint LParam;   // LPARAM argument
    }
    #endregion

    #region Enum HookType
    // Hook Types
    public enum HookType
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14,
    }
    #endregion

    #region Class LocalWindowsHook
    public class LocalWindowsHook
    {
        // ************************************************************************
        // Filter function delegate
        public delegate int HookProc(int code, nint wParam, nint lParam);
        // ************************************************************************

        // ************************************************************************
        // Internal properties
        protected nint hhook = nint.Zero;
        protected HookProc filterFunc;
        protected HookType hookType;
        // ************************************************************************

        // ************************************************************************
        // Event delegate
        public delegate void HookEventHandler(object sender, HookEventArgs e);
        // ************************************************************************

        // ************************************************************************
        // Event: HookInvoked
        public event HookEventHandler HookInvoked;
        protected void OnHookInvoked(HookEventArgs e)
        {
            HookInvoked?.Invoke(this, e);
        }
        // ************************************************************************

        // ************************************************************************
        // Class constructor(s)
        public
LocalWindowsHook(HookType hook)
        {
            hookType = hook;
            filterFunc = CoreHookProc;
        }
        public LocalWindowsHook(HookType hook, HookProc func)
        {
            hookType = hook;
            filterFunc = func;
        }
        // ************************************************************************

        // ************************************************************************
        // Default filter function
        protected int CoreHookProc(int code, nint wParam, nint lParam)
        {
            if (code < 0)
                return CallNextHookEx(hhook, code, wParam, lParam);

            // Let clients determine what to do
            var e = new HookEventArgs
            {
                HookCode = code,
                WParam = wParam,
                LParam = lParam,
            };
            OnHookInvoked(e);

            // Yield to the next hook in the chain
            return CallNextHookEx(hhook, code, wParam, lParam);
        }
        // ************************************************************************

        // ************************************************************************
        // Install the hook
        public void Install()
        {
            hhook = SetWindowsHookEx(
                hookType,
                filterFunc,
                nint.Zero,
                Environment.CurrentManagedThreadId);
        }
        // ************************************************************************

        // ************************************************************************
        // Uninstall the hook
        public void Uninstall()
        {
            _ = UnhookWindowsHookEx(hhook);
        }
        // ************************************************************************

        #region Win32 Imports
        // ************************************************************************
        // Win32: SetWindowsHookEx()
        [DllImport("user32.dll")]
        protected static extern nint SetWindowsHookEx(
            HookType code,
            HookProc func,
            nint hInstance,
            int threadID);
        // ************************************************************************

        // ************************************************************************
        // Win32: UnhookWindowsHookEx()
        [DllImport("user32.dll")]
        protected static extern int UnhookWindowsHookEx(nint hhook);
        // ************************************************************************

        // ************************************************************************
        // Win32: CallNextHookEx()
        [DllImport("user32.dll")]
        protected static extern int CallNextHookEx(
            nint hhook,
            int code,
            nint wParam,
            nint lParam);
        // ************************************************************************
        #endregion
    }
    #endregion
}
