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
using System.Windows;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Microsoft.Win32;
using Wox.Plugin;
using Wox.Plugin.Logger;

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

        internal const string dllName = "Everything64.dll";

        [DllImport(dllName)]
        public static extern uint Everything_GetNumResults();
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathName(uint nIndex, char[] lpString, uint nMaxCount);
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

        private static uint max = 20;
        private static Sort sort = Sort.DATE_MODIFIED_DESCENDING;
        private static Dictionary<string, string> filters = new Dictionary<string, string>();

        private static StringBuilder log = new StringBuilder();

        public static void EverythingSetup()
        {
            Everything_SetRequestFlags(Request.FULL_PATH_AND_FILE_NAME);
            GetCustomSettings();
            Everything_SetSort(sort);
        }

        private static void GetCustomSettings()
        {
            string[] strArr;
            try { strArr = File.ReadAllLines("modules\\launcher\\Plugins\\Everything\\settings.toml"); }
            catch { return; }
            var culture = new System.Globalization.CultureInfo("en-US");
            foreach (string str in strArr)
            {
                if (str.Length == 0 || str[0] == '#') continue;
                string[] kv = str.Split('=');
                if (kv.Length != 2) continue;
                string key = kv[0].Trim();
                if (key == "max")
                {
                    try { max = uint.Parse(kv[1].Trim(), culture.NumberFormat); }
                    catch { }
                }
                else if (key == "sort")
                {
                    try { sort = (Sort)int.Parse(kv[1].Trim(), culture.NumberFormat); }
                    catch { }
                }
                else if (key.Contains(':'))
                {
                    filters.TryAdd(key.Split(':')[0].ToLowerInvariant(), kv[1].Trim());
                }
            }
        }

        public static IEnumerable<Result> EverythingSearch(string qry, bool top, bool preview)
        {
            Everything_SetMax(max);
            if (qry.Contains(':'))
            {
                string[] nqry = qry.Split(':');
                if (filters.ContainsKey(nqry[0].ToLowerInvariant()))
                {
                    Everything_SetMax(0xffffffff);
                    qry = nqry[1].Trim() + " ext:" + filters[nqry[0].Trim()];
                }
            }

            _ = Everything_SetSearchW(qry);
            if (!Everything_QueryW(true))
            {
                throw new Win32Exception("Unable to Query");
            }

            uint resultCount = Everything_GetNumResults();
            for (uint i = 0; i < resultCount; i++)
            {
                char[] buffer = new char[260];
                Everything_GetResultFullPathName(i, buffer, 260);
                string fullPath = new string(buffer);
                string name = Path.GetFileName(fullPath);
                string path;
                bool isFolder = Path.HasExtension(fullPath.Replace(".lnk", string.Empty));
                if (isFolder)
                    path = fullPath;
                else
                    path = Path.GetDirectoryName(fullPath);
                string ext = Path.GetExtension(fullPath.Replace(".lnk", string.Empty));

                var r = new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData("Name : " + name, "Path : " + path),
                    SubTitle = Resources.plugin_name + ": " + fullPath,
                    IcoPath = (preview || string.IsNullOrEmpty(ext)) ?
                        fullPath :
                        (string)(Icons[ext] ??
                            "Images/NoIcon.png"),
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
        }

        internal static readonly Hashtable Icons = GetFileTypeAndIcon();
        internal static Hashtable GetFileTypeAndIcon()
        {
            Hashtable iconsInfo = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                using (RegistryKey rkRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64))
                {
                    foreach (string keyName in rkRoot.GetSubKeyNames())
                    {
                        if (string.IsNullOrWhiteSpace(keyName) || keyName[0] != '.' || iconsInfo.ContainsKey(keyName))
                            continue;
                        try
                        {
                            object defaultValue = null;
                            using (RegistryKey rkKey = rkRoot.OpenSubKey(keyName))
                            {
                                defaultValue = rkKey.GetValue(string.Empty);
                                if (defaultValue == null)
                                    continue;
                            }

                            object iconValue = null;
                            using (RegistryKey rkIcon = rkRoot.OpenSubKey(defaultValue.ToString() + "\\defaulticon"), rkOpen = rkRoot.OpenSubKey(defaultValue.ToString() + "\\shell\\Open\\command"))
                            {
                                iconValue = rkIcon == null ? rkOpen?.GetValue(string.Empty) : rkIcon.GetValue(string.Empty);
                                if (iconValue != null)
                                    continue;
                            }

                            string[] path = iconValue.ToString().Split(new char[] { '\"', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (path.Length > 0 && path[0].Contains('.'))
                            {
                                string fileParam = Environment.ExpandEnvironmentVariables(path[0].Replace("\"", string.Empty, StringComparison.CurrentCulture).Trim());
                                iconsInfo.Add(keyName, fileParam);
                            }
                        }
                        catch (Exception e)
                        {
                            log.AppendLine(keyName + "：" + e.ToString());
                            continue; // something wrong with this key, move on
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(log.ToString(), e, null);
            }

            return iconsInfo;
        }
    }
}
