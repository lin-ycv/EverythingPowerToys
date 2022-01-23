// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal static class NativeMethods
    {
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

        internal const string dllName = "Everything64.dll"; // Included dll is a modified file without locking, if this creates issues, replace with official dll

#pragma warning disable SA1516 // Elements should be separated by blank line
        [DllImport(dllName)]
        public static extern uint Everything_GetNumResults();
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport(dllName)]
        public static extern bool Everything_QueryW(bool bWait);
        [DllImport(dllName)]
        public static extern void Everything_SetMax(uint dwMax);
        [DllImport(dllName)]
        public static extern void Everything_SetRequestFlags(Request RequestFlags);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern uint Everything_SetSearchW(string lpSearchString);
        [DllImport(dllName)]
        public static extern void Everything_SetSort(Sort SortType);

        private const int max = 20;
#pragma warning disable SA1503 // Braces should not be omitted
        public static void EverythingSetup()
        {
            Everything_SetRequestFlags(Request.FULL_PATH_AND_FILE_NAME);
            Everything_SetSort(Sort.DATE_MODIFIED_DESCENDING);
            Everything_SetMax(max);
        }

        public static IEnumerable<Result> EverythingSearch(string qry, bool top, bool noPreview, CancellationToken token)
        {
            _ = Everything_SetSearchW(qry);
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
            if (!Everything_QueryW(true))
            {
                throw new Win32Exception("Unable to Query");
            }

            uint resultCount = Everything_GetNumResults();

            for (uint i = 0; i < resultCount; i++)
            {
                if (token.IsCancellationRequested) break;
                StringBuilder sb = new StringBuilder(260);
                Everything_GetResultFullPathName(i, sb, 260);
                string fullPath = sb.ToString();
                string name = Path.GetFileName(fullPath);
                string path;
                bool isFolder = Path.HasExtension(fullPath);
                if (isFolder)
                    path = fullPath;
                else
                    path = Path.GetDirectoryName(fullPath);

                var r = new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData("Name : " + name, "Path : " + path),
                    SubTitle = Resources.plugin_name + ": " + fullPath,
                    IcoPath = noPreview ? "Images/Everything.ico.png" : fullPath,
                    ContextData = new SearchResult()
                    {
                        Path = fullPath,
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
                            catch (Win32Exception)
                            {
                                return false;
                            }
                        }
                    },
                    QueryTextDisplay = isFolder ? path : name,
                };
                if (top) r.Score = (int)(max - i);
                yield return r;
            }

            if (token.IsCancellationRequested)
            {
                yield return new Result()
                {
                    Title = Resources.timeout,
                    SubTitle = Resources.enable_wait,
                    IcoPath = Main.WarningIcon,
                    Score = int.MaxValue,
                };
            }
        }
#pragma warning restore SA1503 // Braces should not be omitted
    }
}
