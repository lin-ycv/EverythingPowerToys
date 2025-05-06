using System;
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
        #endregion
#if X64
        internal const string dllName = "Everything2_x64.dll";
#else
        internal const string dllName = "Everything2_ARM64.dll";
#endif
        [LibraryImport(dllName)]
        internal static partial void Everything_CleanUp();
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetLastError();
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetNumResults();
        [LibraryImport(dllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_GetMatchPath();
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetMax();
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetMinorVersion();
        [LibraryImport(dllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_GetRegex();
        [LibraryImport(dllName)]
        internal static partial IntPtr Everything_GetResultFileNameW(uint nIndex);
        [LibraryImport(dllName)]
        internal static partial IntPtr Everything_GetResultPathW(uint nIndex);
        [LibraryImport(dllName)]
        internal static partial uint Everything_GetSort();
        [LibraryImport(dllName, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint Everything_IncRunCountFromFileNameW(string lpFileName);
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
