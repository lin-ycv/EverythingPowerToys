using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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

        internal const string dllName = "Everything64.dll";

        [DllImport(dllName)]
        internal static extern uint Everything_GetNumResults();
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport(dllName)]
        internal static extern bool Everything_IsFolderResult(uint index);
        [DllImport(dllName)]
        internal static extern bool Everything_QueryW(bool bWait);
        [DllImport(dllName)]
        internal static extern void Everything_SetMax(uint dwMax);
        [DllImport(dllName)]
        internal static extern void Everything_SetRegex(bool bEnable);
        [DllImport(dllName)]
        internal static extern void Everything_SetRequestFlags(Request RequestFlags);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern uint Everything_SetSearchW(string lpSearchString);
        [DllImport(dllName)]
        internal static extern bool Everything_SetMatchPath(bool bEnable);
        [DllImport(dllName)]
        internal static extern void Everything_SetSort(Sort SortType);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] char[] pszOut, [In][Out] ref uint pcchOut);

        private static uint max = 20;
        private static Sort sort = Sort.DATE_MODIFIED_DESCENDING;
        private static Dictionary<string, string> filters = new Dictionary<string, string>();

        public static void EverythingSetup(bool debug)
        {
            Everything_SetRequestFlags(Request.FULL_PATH_AND_FILE_NAME);
            GetCustomSettings(debug);
            Everything_SetSort(sort);
        }

        private static void GetCustomSettings(bool debug)
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

            if (debug)
            {
                string msg = $"Max: {max}\nSort: {sort}\nFilters: {string.Join("\n - ", filters.Select(x => { return x.Key + "_" + x.Value; }))}";
                Log.Info(msg, typeof(NativeMethods));
            }
        }

        public static IEnumerable<Result> EverythingSearch(string qry, bool preview, bool matchpath, bool debug)
        {
            string orgqry = qry;
            Everything_SetMax(max);
            if (orgqry.Contains('\"') && !matchpath)
            {
                Everything_SetMatchPath(true);
            }

            if (orgqry.Contains(':'))
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

            if (orgqry.Contains('\"') && !matchpath)
            {
                Everything_SetMatchPath(false);
            }

            uint resultCount = Everything_GetNumResults();

            if (debug)
            {
                Log.Info(qry + " => " + resultCount, typeof(NativeMethods), "EverythingSearch.ResultCount", string.Empty, 217);
            }

            for (uint i = 0; i < resultCount; i++)
            {
                StringBuilder buffer = new StringBuilder(260);
                Everything_GetResultFullPathName(i, buffer, 260);
                string fullPath = buffer.ToString();
                string name = Path.GetFileName(fullPath);
                bool isFolder = Everything_IsFolderResult(i);
                string path = isFolder ? fullPath : Path.GetDirectoryName(fullPath);
                string ext = Path.GetExtension(fullPath.Replace(".lnk", string.Empty));

                if (debug)
                {
                    Log.Info(i + " : " + name + " = " + fullPath, typeof(NativeMethods), "EverythingSearch.Result", string.Empty, 229);
                }

                var r = new Result()
                {
                    Title = name,
                    ToolTipData = debug ?
                    new ToolTipData(orgqry, qry) :
                    new ToolTipData("Name : " + name, fullPath),
                    SubTitle = Resources.plugin_name + ": " + fullPath,

                    IcoPath = isFolder ? "Images/folder.png" : (preview ?
                        fullPath : (Icon(ext) ?? "Images/file.png")),
                    ContextData = new SearchResult()
                    {
                        Path = fullPath,
                        Title = name,
                        File = !isFolder,
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
                yield return r;
            }
        }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        internal static string? Icon(string doctype)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            uint pcchOut = 0;
            _ = AssocQueryString(AssocF.NONE, AssocStr.DEFAULTICON, doctype, null, null, ref pcchOut);
            char[] pszOut = new char[pcchOut];
            if (AssocQueryString(AssocF.NONE, AssocStr.DEFAULTICON, doctype, null, pszOut, ref pcchOut) != 0) return null;
            string doc = Environment.ExpandEnvironmentVariables(new string(pszOut).Split(new char[] { '\"', ',' }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("\"", string.Empty, StringComparison.CurrentCulture).Trim());

            return File.Exists(doc) ? doc : null;
        }
    }
}
