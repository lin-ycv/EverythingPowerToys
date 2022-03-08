// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
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
using Microsoft.Win32;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal static class NativeMethods
    {
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct SHFILEINFO
        {
            internal IntPtr hIcon;
            internal int iIcon;
            internal int dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            internal string szTypeName;
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

        public static IEnumerable<Result> EverythingSearch(string qry, bool top, bool preview, CancellationToken token)
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
                string ext = Path.GetExtension(fullPath);

                var r = new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData("Name : " + name, "Path : " + path),
                    SubTitle = Resources.plugin_name + ": " + fullPath,
                    IcoPath = (preview || string.IsNullOrEmpty(ext)) ? fullPath : (string)Icons[ext],
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

        internal static readonly Hashtable Icons = GetFileTypeAndIcon();
        internal static Hashtable GetFileTypeAndIcon()
        {
            try
            {
                RegistryKey rkRoot = Registry.ClassesRoot;
                string[] keyNames = rkRoot.GetSubKeyNames();
                Hashtable iconsInfo = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
                foreach (string keyName in keyNames)
                {
                    if (string.IsNullOrEmpty(keyName))
                        continue;
                    int indexOfPoint = keyName.IndexOf(".", StringComparison.CurrentCulture);
                    if (indexOfPoint != 0)
                        continue;
                    RegistryKey rkFileType = rkRoot.OpenSubKey(keyName);
                    if (rkFileType == null)
                        continue;
                    object defaultValue = rkFileType.GetValue(string.Empty);
                    if (defaultValue == null)
                        continue;
                    string prog = defaultValue.ToString() + "\\shell\\Open\\command";
                    RegistryKey rkFileIcon = rkRoot.OpenSubKey(prog);
                    if (rkFileIcon != null)
                    {
                        object value = rkFileIcon.GetValue(string.Empty);
                        if (value != null)
                        {
                            string fileParam = value.ToString().Split("\" ")[0].Replace("\"", string.Empty, StringComparison.CurrentCulture).Trim();
                            iconsInfo.Add(keyName, fileParam);
                        }

                        rkFileIcon.Close();
                    }

                    rkFileType.Close();
                }

                rkRoot.Close();
                return iconsInfo;
            }
            catch (Exception)
            {
                throw;
            }
        }
#pragma warning restore SA1503 // Braces should not be omitted
    }
}
