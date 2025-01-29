﻿using System;
using System.Runtime.InteropServices;

namespace Community.PowerToys.Run.Plugin.Everything3.Interop
{
    public sealed class NativeMethods
    {
        #region FlagsEnums
        [Flags]
        internal enum Request
        {
            NAME,
            PATH,
            SIZE,
            EXTENSION,
            TYPE_NAME,
            DATE_MODIFIED,
            DATE_CREATED,
            DATE_ACCESSED,
            ATTRIBUTES,
            DATE_RECENTLY_CHANGED,
            RUN_COUNT,
            DATE_RUN,
            FILE_LIST_FILENAME,
            FULL_PATH = 240,
        }

        public enum Sort
        {
            NAME,
            PATH,
            SIZE,
            EXTENSION,
            TYPE_NAME,
            DATE_MODIFIED,
            DATE_CREATED,
            DATE_ACCESSED,
            ATTRIBUTES,
            DATE_RECENTLY_CHANGED,
            RUN_COUNT,
            DATE_RUN,
            FILE_LIST_FILENAME,
        }
        #endregion
        internal const string dllName = "Everything3_x64.dll";

        // Connect to Everything
        // instance name can be NULL or an empty string to connect to the main unnamed instance.
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Everything3_ConnectW(string instance_name);

        // Destroy an Everything client.
        // disconnects from everything and frees any resources back to the system.
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroyClient(IntPtr client);

        // general
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern uint Everything3_IncRunCountFromFilenameW(IntPtr client, string filename);

        // Setup the search state.
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_CreateSearchState();
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroySearchState(IntPtr search_state);
        [DllImport(dllName)]
        internal static extern bool Everything3_SetSearchMatchPath(IntPtr search_state, bool match_path);
        [DllImport(dllName)]
        internal static extern bool Everything3_SetSearchRegex(IntPtr search_state, bool match_regex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern bool Everything3_SetSearchTextW(IntPtr search_state, string search);
        [DllImport(dllName)]
        internal static extern bool Everything3_AddSearchSort(IntPtr search_state, Sort sort, bool ascending);
        [DllImport(dllName)]
        internal static extern bool Everything3_ClearSearchSorts(IntPtr search_state);

        // execute a search
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_Search(IntPtr client, IntPtr search_state);
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroyResultList(IntPtr result_list);

        // Result list.
        [DllImport(dllName)]
        internal static extern uint Everything3_GetResultListCount(IntPtr result_list);
        [DllImport(dllName)]
        internal static extern bool Everything3_IsFolderResult(IntPtr result_list, uint result_index);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern uint Everything3_GetResultFullPathNameW(IntPtr result_list, uint result_index, [Out] char[] wbuf, uint wbuf_size_in_wchars);
    }
}
