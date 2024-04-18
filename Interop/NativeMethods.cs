using System.Runtime.InteropServices;

namespace Community.PowerToys.Run.Plugin.Everything.Interop
{
    public sealed partial class NativeMethods
    {
        #region FlagsEnums
        [Flags]
        internal enum Request
        {
            FILE_NAME = 0x00000001,
            PATH = 0x00000002,
            FULL_PATH_AND_FILE_NAME = 0x00000004,
            EXTENSION = 0x00000008,
            SIZE = 0x00000010,
            DATE_CREATED = 0x00000020,
            DATE_MODIFIED = 0x00000040,
            DATE_ACCESSED = 0x00000080,
            ATTRIBUTES = 0x00000100,
            FILE_LIST_FILE_NAME = 0x00000200,
            RUN_COUNT = 0x00000400,
            DATE_RUN = 0x00000800,
            DATE_RECENTLY_CHANGED = 0x00001000,
            HIGHLIGHTED_FILE_NAME = 0x00002000,
            HIGHLIGHTED_PATH = 0x00004000,
            HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000,
        }

        public enum Sort
        {
            NAME_ASCENDING = 1,
            NAME_DESCENDING,
            PATH_ASCENDING,
            PATH_DESCENDING,
            SIZE_ASCENDING,
            SIZE_DESCENDING,
            EXTENSION_ASCENDING,
            EXTENSION_DESCENDING,
            TYPE_NAME_ASCENDING,
            TYPE_NAME_DESCENDING,
            DATE_CREATED_ASCENDING,
            DATE_CREATED_DESCENDING,
            DATE_MODIFIED_ASCENDING,
            DATE_MODIFIED_DESCENDING,
            ATTRIBUTES_ASCENDING,
            ATTRIBUTES_DESCENDING,
            FILE_LIST_FILENAME_ASCENDING,
            FILE_LIST_FILENAME_DESCENDING,
            RUN_COUNT_ASCENDING,
            RUN_COUNT_DESCENDING,
            DATE_RECENTLY_CHANGED_ASCENDING,
            DATE_RECENTLY_CHANGED_DESCENDING,
            DATE_ACCESSED_ASCENDING,
            DATE_ACCESSED_DESCENDING,
            DATE_RUN_ASCENDING,
            DATE_RUN_DESCENDING,
        }

        [Flags]
        internal enum AssocF
        {
            NONE = 0x00000000,
            INIT_NOREMAPCLSID = 0x00000001,
            INIT_BYEXENAME = 0x00000002,
            INIT_DEFAULTTOSTAR = 0x00000004,
            INIT_DEFAULTTOFOLDER = 0x00000008,
            NOUSERSETTINGS = 0x00000010,
            NOTRUNCATE = 0x00000020,
            VERIFY = 0x00000040,
            REMAPRUNDLL = 0x00000080,
            NOFIXUPS = 0x00000100,
            IGNOREBASECLASS = 0x00000200,
            INIT_IGNOREUNKNOWN = 0x00000400,
            INIT_FIXED_PROGID = 0x00000800,
            IS_PROTOCOL = 0x00001000,
            INIT_FOR_FILE = 0x00002000,
        }

        internal enum AssocStr
        {
            COMMAND = 1,
            EXECUTABLE,
            FRIENDLYDOCNAME,
            FRIENDLYAPPNAME,
            NOOPEN,
            SHELLNEWVALUE,
            DDECOMMAND,
            DDEIFEXEC,
            DDEAPPLICATION,
            DDETOPIC,
            INFOTIP,
            QUICKTIP,
            TILEINFO,
            CONTENTTYPE,
            DEFAULTICON,
            SHELLEXTENSION,
            DROPTARGET,
            DELEGATEEXECUTE,
            SUPPORTED_URI_PROTOCOLS,
            PROGID,
            APPID,
            APPPUBLISHER,
            APPICONREFERENCE,
            MAX,
        }
        #endregion
        internal const string dllName = "Everything64.dll";
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetLastError();
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetNumResults();
        [LibraryImport(dllName, EntryPoint = "Everything_GetResultFullPathNameW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint Everything_GetResultFullPathName(uint nIndex, [Out] char[] lpString, uint nMaxCount);
        [LibraryImport(dllName, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint Everything_IncRunCountFromFileName(string lpFileName);
        [LibraryImport(dllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_IsFolderResult(uint index);
        [LibraryImport(dllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_QueryW([MarshalAs(UnmanagedType.Bool)] bool bWait);
        [LibraryImport(dllName)]
        internal static partial void Everything_SetMax(uint dwMax);
        [LibraryImport(dllName)]
        internal static partial void Everything_SetRegex([MarshalAs(UnmanagedType.Bool)] bool bEnable);
        [LibraryImport(dllName)]
        internal static partial void Everything_SetRequestFlags(Request RequestFlags);
        [LibraryImport(dllName, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void Everything_SetSearchW(string lpSearchString);
        [LibraryImport(dllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_SetMatchPath([MarshalAs(UnmanagedType.Bool)] bool bEnable);
        [LibraryImport(dllName)]
        internal static partial void Everything_SetSort(Sort SortType);
    }
}
