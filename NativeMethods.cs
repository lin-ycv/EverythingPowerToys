// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
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
            OK,
            ERROR_MEMORY,
            ERROR_IPC,
            ERROR_REGISTERCLASSEX,
            ERROR_CREATEWINDOW,
            ERROR_CREATETHREAD,
            ERROR_INVALIDINDEX,
            ERROR_INVALIDCALL,
        }

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

        internal enum Sort
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

        internal const int TARGET_MACHINE_X86 = 1;
        internal const int TARGET_MACHINE_X64 = 2;
        internal const int TARGET_MACHINE_ARM = 3;
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
        public static extern void Everything_SetRequestFlags(Request RequestFlags);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern bool Everything_SetRunCountFromFileName(string lpFileName, uint dwRunCount);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern uint Everything_SetSearchW(string lpSearchString);
        [DllImport(dllName)]
        public static extern void Everything_SetSort(Sort SortType);

        [DllImport(dllName)]
        public static extern void Everything_SortResultsByPath();
        [DllImport(dllName)]
        public static extern bool Everything_UpdateAllFolderIndexes();

        private static readonly IFileSystem _fileSystem = new FileSystem();
        private const int max = 5;
        private static CancellationTokenSource source;
#pragma warning disable SA1503 // Braces should not be omitted
        public static IEnumerable<Result> EverythingSearch(string qry)
        {
            source?.Cancel();
            source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.CancelAfter(50);

            _ = Everything_SetSearchW(qry);
            if (token.IsCancellationRequested) yield return new Result();
            Everything_SetRequestFlags(Request.FULL_PATH_AND_FILE_NAME | Request.DATE_MODIFIED);
            if (token.IsCancellationRequested) yield return new Result();
            Everything_SetSort(Sort.DATE_MODIFIED_DESCENDING);
            if (token.IsCancellationRequested) yield return new Result();
            Everything_SetMax(max);
            if (token.IsCancellationRequested) yield return new Result();

            if (!Everything_QueryW(true))
            {
                yield return new Result() { Title = "!", };
            }

            uint resultCount = Everything_GetNumResults();

            if (token.IsCancellationRequested) yield return new Result();
            for (uint i = 0; i < resultCount; i++)
            {
                /*Marshal.PtrToStringUni(*/
                StringBuilder sb = new StringBuilder(260);
                Everything_GetResultFullPathName(i, sb, 260);
                string fullPath = sb.ToString();
                string name = Path.GetFileName(fullPath);
                string path;
                bool isFolder = _fileSystem.Directory.Exists(fullPath);
                if (isFolder)
                    path = fullPath;
                else
                    path = Path.GetDirectoryName(fullPath);

                yield return new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData("Name : " + name, "Path : " + path),
                    SubTitle = Properties.Resources.plugin_name + ": " + fullPath,
                    IcoPath = fullPath,
                    Score = (int)(max - i),
                    ContextData = new SearchResult()
                    {
                        Path = path,
                        Title = name,
                    },
                    Action = e =>
                    {
                        using (var process = new Process())
                        {
                            process.StartInfo.FileName = fullPath;
                            process.StartInfo.WorkingDirectory = path;
                            process.StartInfo.UseShellExecute = true;

                            try
                            {
                                process.Start();
                                return true;
                            }
                            catch (System.ComponentModel.Win32Exception)
                            {
                                return false;
                            }
                        }
                    },
                    QueryTextDisplay = isFolder ? path : name,
                };
                if (token.IsCancellationRequested) break;
            }
        }
#pragma warning restore SA1503 // Braces should not be omitted
    }
}
