using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Everything
    {
        private string exe = string.Empty;
        internal Everything(Settings setting)
        {
            Everything_SetRequestFlags(Request.FILE_NAME | Request.PATH);
            UpdateSettings(setting);
        }

        internal void UpdateSettings(Settings setting)
        {
            Everything_SetSort(setting.Sort);
            Everything_SetMax(setting.Max);
            Everything_SetMatchPath(setting.MatchPath);
            Everything_SetRegex(setting.RegEx);
            if (!string.IsNullOrEmpty(setting.EverythingPath))
            {
                if (setting.EverythingPath != exe && Path.Exists(setting.EverythingPath))
                    exe = setting.EverythingPath;
            }
            else if (string.IsNullOrEmpty(exe))
            {
                exe = Path.Exists("C:\\Program Files\\Everything 1.5a\\Everything64.exe") ? "C:\\Program Files\\Everything 1.5a\\Everything64.exe" :
                    (Path.Exists("C:\\Program Files\\Everything\\Everything.exe") ? "C:\\Program Files\\Everything\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe") ? "C:\\Program Files (x86)\\Everything 1.5a\\Everything.exe" :
                    (Path.Exists("C:\\Program Files (x86)\\Everything\\Everything.exe") ? "C:\\Program Files (x86)\\Everything\\Everything.exe" : string.Empty)));
            }
        }

        internal IEnumerable<Result> Query(string query, Settings setting)
        {
#if DEBUG
            if (setting.Log > LogLevel.None)
            {
                Debugger.Write($"\r\n\r\nNew Query: {query}\r\n" +
                    $"Prefix {setting.Prefix} | " +
                    $"Sort {(int)setting.Sort}_{Everything_GetSort()} | " +
                    $"Max {setting.Max}_{Everything_GetMax()} | " +
                    $"Match Path {setting.MatchPath}_{Everything_GetMatchPath()} | " +
                    $"Regex {setting.RegEx}_{Everything_GetRegex()}");
            }
#endif

            if (!string.IsNullOrEmpty(setting.Prefix))
                query = setting.Prefix + query;

            string orgqry = query;

            if (setting.EnvVar && orgqry.Contains('%'))
            {
                query = Environment.ExpandEnvironmentVariables(query).Replace(';', '|');
#if DEBUG
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"EnvVariable\r\n{query}");
#endif
            }

            if (Everything_GetMinorVersion() < 5 && orgqry.Contains(':'))
            {
                foreach (var kv in setting.Filters)
                {
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Replace(kv.Key, kv.Value);
#if DEBUG
                        if (setting.Log > LogLevel.None)
                            Debugger.Write($"Contains Filter: {kv.Key}\r\n{query}");
#endif
                    }
                }
            }

            Everything_SetSearchW(query);
            if (!Everything_QueryW(true))
            {
#if DEBUG
                if (setting.Log > LogLevel.None)
                    Debugger.Write("\r\nUnable to Query\r\n");
#endif
                throw new Win32Exception("Unable to Query");
            }

            uint resultCount = Everything_GetNumResults();
#if DEBUG
            if (setting.Log > LogLevel.None)
                Debugger.Write($"Results: {resultCount}");
#endif

            bool showMore = setting.ShowMore && !string.IsNullOrEmpty(exe) && resultCount == setting.Max;
            if (showMore)
            {
                var more = new Result()
                {
                    Title = Resources.more_results,
                    SubTitle = Resources.more_results_Subtitle,
                    IcoPath = "Images/Everything.light.png",
                    Action = e =>
                    {
                        using var process = new Process();
                        process.StartInfo.FileName = exe;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.Arguments = $@"-s ""{query.Replace("\"", "\"\"\"")}""";
                        try
                        {
                            process.Start();
                            return true;
                        }
                        catch (Win32Exception)
                        {
                            return false;
                        }
                    },
                    Score = int.MinValue,
                    QueryTextDisplay = orgqry,
                };
                yield return more;
            }

            for (uint i = 0; i < resultCount; i++)
            {
#if DEBUG
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"\r\n===== RESULT #{i} =====");
#endif
                string name = Marshal.PtrToStringUni(Everything_GetResultFileNameW(i));
                string path = Marshal.PtrToStringUni(Everything_GetResultPathW(i));
                if (name == null || path == null)
                {
                    Log.Warn($"Result {i} is null for {name} and/or {path}, query: {query}", GetType());
                    continue;
                }
                string fullPath = Path.Combine(path, name);
#if DEBUG
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"{fullPath.Length} {(setting.Log == LogLevel.Verbose ? fullPath : string.Empty)}");
#endif
                bool isFolder = Everything_IsFolderResult(i);
                if (isFolder)
                    path = fullPath;
                string ext = Path.GetExtension(fullPath.Replace(".lnk", string.Empty));
#if DEBUG
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"Folder: {isFolder}\r\nFile Path {(setting.Log == LogLevel.Verbose ? path : path.Length)}\r\nFile Name {(setting.Log == LogLevel.Verbose ? name : name.Length)}\r\nExt: {ext}");
#endif
                var r = new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData(name, fullPath),
                    SubTitle = Resources.plugin_name + ": " + fullPath,

                    IcoPath = isFolder ? "Images/folder.png" : (setting.Preview ?
                        fullPath : "Images/file.png"),
                    ContextData = new SearchResult()
                    {
                        Path = fullPath,
                        Title = name,
                        File = !isFolder,
                    },
                    Action = e =>
                    {
                        using var process = new Process();
                        process.StartInfo.FileName = fullPath;
                        process.StartInfo.WorkingDirectory = path;
                        process.StartInfo.UseShellExecute = true;

                        try
                        {
                            process.Start();
                            _ = Everything_IncRunCountFromFileName(fullPath);
                            return true;
                        }
                        catch (Win32Exception)
                        {
                            return false;
                        }
                    },

                    QueryTextDisplay = setting.QueryText ? (isFolder ? path : name) : orgqry,
                };
                yield return r;
            }
        }
    }
}
