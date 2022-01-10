// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal static class NativeMethods
    {
        internal enum ErrorCode
        {
            EVERYTHING_OK,
            EVERYTHING_ERROR_MEMORY,
            EVERYTHING_ERROR_IPC,
            EVERYTHING_ERROR_REGISTERCLASSEX,
            EVERYTHING_ERROR_CREATEWINDOW,
            EVERYTHING_ERROR_CREATETHREAD,
            EVERYTHING_ERROR_INVALIDINDEX,
            EVERYTHING_ERROR_INVALIDCALL,
        }

        internal const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        internal const int EVERYTHING_REQUEST_PATH = 0x00000002;
        internal const int EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
        internal const int EVERYTHING_REQUEST_EXTENSION = 0x00000008;
        internal const int EVERYTHING_REQUEST_SIZE = 0x00000010;
        internal const int EVERYTHING_REQUEST_DATE_CREATED = 0x00000020;
        internal const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
        internal const int EVERYTHING_REQUEST_DATE_ACCESSED = 0x00000080;
        internal const int EVERYTHING_REQUEST_ATTRIBUTES = 0x00000100;
        internal const int EVERYTHING_REQUEST_FILE_LIST_FILE_NAME = 0x00000200;
        internal const int EVERYTHING_REQUEST_RUN_COUNT = 0x00000400;
        internal const int EVERYTHING_REQUEST_DATE_RUN = 0x00000800;
        internal const int EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED = 0x00001000;
        internal const int EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME = 0x00002000;
        internal const int EVERYTHING_REQUEST_HIGHLIGHTED_PATH = 0x00004000;
        internal const int EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 0x00008000;

        internal const int EVERYTHING_SORT_NAME_ASCENDING = 1;
        internal const int EVERYTHING_SORT_NAME_DESCENDING = 2;
        internal const int EVERYTHING_SORT_PATH_ASCENDING = 3;
        internal const int EVERYTHING_SORT_PATH_DESCENDING = 4;
        internal const int EVERYTHING_SORT_SIZE_ASCENDING = 5;
        internal const int EVERYTHING_SORT_SIZE_DESCENDING = 6;
        internal const int EVERYTHING_SORT_EXTENSION_ASCENDING = 7;
        internal const int EVERYTHING_SORT_EXTENSION_DESCENDING = 8;
        internal const int EVERYTHING_SORT_TYPE_NAME_ASCENDING = 9;
        internal const int EVERYTHING_SORT_TYPE_NAME_DESCENDING = 10;
        internal const int EVERYTHING_SORT_DATE_CREATED_ASCENDING = 11;
        internal const int EVERYTHING_SORT_DATE_CREATED_DESCENDING = 12;
        internal const int EVERYTHING_SORT_DATE_MODIFIED_ASCENDING = 13;
        internal const int EVERYTHING_SORT_DATE_MODIFIED_DESCENDING = 14;
        internal const int EVERYTHING_SORT_ATTRIBUTES_ASCENDING = 15;
        internal const int EVERYTHING_SORT_ATTRIBUTES_DESCENDING = 16;
        internal const int EVERYTHING_SORT_FILE_LIST_FILENAME_ASCENDING = 17;
        internal const int EVERYTHING_SORT_FILE_LIST_FILENAME_DESCENDING = 18;
        internal const int EVERYTHING_SORT_RUN_COUNT_ASCENDING = 19;
        internal const int EVERYTHING_SORT_RUN_COUNT_DESCENDING = 20;
        internal const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_ASCENDING = 21;
        internal const int EVERYTHING_SORT_DATE_RECENTLY_CHANGED_DESCENDING = 22;
        internal const int EVERYTHING_SORT_DATE_ACCESSED_ASCENDING = 23;
        internal const int EVERYTHING_SORT_DATE_ACCESSED_DESCENDING = 24;
        internal const int EVERYTHING_SORT_DATE_RUN_ASCENDING = 25;
        internal const int EVERYTHING_SORT_DATE_RUN_DESCENDING = 26;

        internal const int EVERYTHING_TARGET_MACHINE_X86 = 1;
        internal const int EVERYTHING_TARGET_MACHINE_X64 = 2;
        internal const int EVERYTHING_TARGET_MACHINE_ARM = 3;
        private const string dllName = "Everything64.dll";

#pragma warning disable SA1516 // Elements should be separated by blank line
        [DllImport(dllName)]
        public static extern void Everything_CleanUp();
        [DllImport(dllName)]
        public static extern uint Everything_DeleteRunHistory();
        [DllImport(dllName)]
        public static extern uint Everything_Exit();

        [DllImport(dllName)]
        public static extern uint Everything_GetBuildNumber();
        [DllImport(dllName)]
        public static extern uint Everything_GetLastError();
        [DllImport(dllName)]
        public static extern uint Everything_GetMajorVersion();
        [DllImport(dllName)]
        public static extern bool Everything_GetMatchCase();
        [DllImport(dllName)]
        public static extern bool Everything_GetMatchPath();
        [DllImport(dllName)]
        public static extern bool Everything_GetMatchWholeWord();
        [DllImport(dllName)]
        public static extern uint Everything_GetMax();
        [DllImport(dllName)]
        public static extern uint Everything_GetMinorVersion();
        [DllImport(dllName)]
        public static extern uint Everything_GetNumFileResults();
        [DllImport(dllName)]
        public static extern uint Everything_GetNumFolderResults();
        [DllImport(dllName)]
        public static extern uint Everything_GetNumResults();
        [DllImport(dllName)]
        public static extern uint Everything_GetOffset();
        [DllImport(dllName)]
        public static extern bool Everything_GetRegex();
        [DllImport(dllName)]
        public static extern uint Everything_GetReplyID();
        [DllImport(dllName)]
        public static extern uint Everything_GetRequestFlags();
        [DllImport(dllName)]

        public static extern uint Everything_GetResultAttributes(uint nIndex);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultDateRecentlyChanged(uint nIndex, out long lpFileTime);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultDateRun(uint nIndex, out long lpFileTime);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultExtension(uint nIndex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultFileListFileName(uint nIndex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultFileName(uint nIndex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultHighlightedFileName(uint nIndex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultHighlightedFullPathAndFileName(uint nIndex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultHighlightedPath(uint nIndex);
        [DllImport(dllName)]
        public static extern uint Everything_GetResultListRequestFlags();
        [DllImport(dllName)]
        public static extern uint Everything_GetResultListSort();
        [DllImport(dllName)]
        public static extern IntPtr Everything_GetResultPath(uint nIndex);
        [DllImport(dllName)]
        public static extern uint Everything_GetResultRunCount(uint nIndex);
        [DllImport(dllName)]
        public static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);

        [DllImport(dllName)]
        public static extern uint Everything_GetRevision();
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern uint Everything_GetRunCountFromFileName(string lpFileName);
        [DllImport(dllName)]
        public static extern IntPtr Everything_GetSearchW();
        [DllImport(dllName)]
        public static extern uint Everything_GetSort();

        [DllImport(dllName)]
        public static extern uint Everything_GetTotFileResults();
        [DllImport(dllName)]
        public static extern uint Everything_GetTotFolderResults();
        [DllImport(dllName)]
        public static extern uint Everything_GetTotResults();

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern uint Everything_IncRunCountFromFileName(string lpFileName);
        [DllImport(dllName)]
        public static extern bool Everything_IsAdmin();
        [DllImport(dllName)]
        public static extern bool Everything_IsAppData();
        [DllImport(dllName)]
        public static extern bool Everything_IsDBLoaded();
        [DllImport(dllName)]
        public static extern bool Everything_IsFastSort(uint sortType);
        [DllImport(dllName)]
        public static extern bool Everything_IsFileInfoIndexed(uint fileInfoType);
        [DllImport(dllName)]
        public static extern bool Everything_IsFileResult(uint nIndex);
        [DllImport(dllName)]
        public static extern bool Everything_IsFolderResult(uint nIndex);
        [DllImport(dllName)]
        public static extern bool Everything_IsQueryReply(uint message, uint wParam, long lParam, uint nId);
        [DllImport(dllName)]
        public static extern bool Everything_IsVolumeResult(uint nIndex);

        [DllImport(dllName)]
        public static extern bool Everything_QueryW(bool bWait);
        [DllImport(dllName)]
        public static extern bool Everything_RebuildDB();
        [DllImport(dllName)]
        public static extern void Everything_Reset();
        [DllImport(dllName)]
        public static extern bool Everything_SaveDB();
        [DllImport(dllName)]
        public static extern bool Everything_SaveRunHistory();

        [DllImport(dllName)]
        public static extern void Everything_SetMatchCase(bool bEnable);
        [DllImport(dllName)]
        public static extern void Everything_SetMatchPath(bool bEnable);
        [DllImport(dllName)]
        public static extern void Everything_SetMatchWholeWord(bool bEnable);
        [DllImport(dllName)]
        public static extern void Everything_SetMax(uint dwMax);
        [DllImport(dllName)]
        public static extern void Everything_SetOffset(uint dwOffset);
        [DllImport(dllName)]
        public static extern void Everything_SetRegex(bool bEnable);
        [DllImport(dllName)]
        public static extern void Everything_SetReplyID(uint nId);
        [DllImport(dllName)]
        public static extern void Everything_SetRequestFlags(uint dwRequestFlags);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern bool Everything_SetRunCountFromFileName(string lpFileName, uint dwRunCount);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern uint Everything_SetSearchW(string lpSearchString);
        [DllImport(dllName)]
        public static extern void Everything_SetSort(uint dwSortType);

        [DllImport(dllName)]
        public static extern void Everything_SortResultsByPath();
        [DllImport(dllName)]
        public static extern bool Everything_UpdateAllFolderIndexes();

        private static CancellationTokenSource source;

        public static IEnumerable<Result> EverythingSearch(string qry)
        {
            source?.Cancel();
            source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            _ = Everything_SetSearchW(qry);
            Everything_SetRequestFlags(EVERYTHING_REQUEST_FILE_NAME | EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_DATE_MODIFIED | EVERYTHING_REQUEST_SIZE);
            Everything_SetSort(EVERYTHING_SORT_DATE_MODIFIED_DESCENDING);
            Everything_SetMax(20);
            Everything_SetRegex(false);
            if (qry.StartsWith("@", StringComparison.CurrentCulture))
            {
                Everything_SetRegex(true);
                qry = qry.Substring(1);
            }

            _ = Everything_QueryW(true);
            uint resultCount = Everything_GetNumResults();

            var t = Task.Run(
                () =>
                {
                    var results = new List<Result>();
                    for (uint i = 0; i < resultCount; i++)
                    {
                        _ = Everything_GetResultDateModified(i, out long date_modified);
                        _ = Everything_GetResultSize(i, out long size);
                        string fileName = Marshal.PtrToStringUni(Everything_GetResultFileName(i));
                        StringBuilder sb = new StringBuilder(999);
                        Everything_GetResultFullPathName(i, sb, 999);
                        string filePath = sb.ToString();

                        results.Add(new Result()
                        {
                            Title = fileName,
                            ToolTipData = new ToolTipData("Name : " + fileName, "Path : " + filePath),
                            SubTitle = Properties.Resources.plugin_name + ":" + filePath,
                            IcoPath = filePath,
                            Score = (int)(Everything_GetTotResults() - i),
                            Action = e =>
                            {
                                using (Process process = new Process())
                                {
                                    process.StartInfo.FileName = System.IO.Path.Combine(filePath, fileName);
                                    process.StartInfo.WorkingDirectory = filePath;
                                    process.StartInfo.Arguments = string.Empty;

                                    process.StartInfo.UseShellExecute = true;

                                    try
                                    {
                                        _ = process.Start();
                                        return true;
                                    }
                                    catch (System.ComponentModel.Win32Exception)
                                    {
                                        return false;
                                    }
                                }
                            },
                        });
                    }

                    return results;
                }, token);
            return t.Result;
        }
    }
}
