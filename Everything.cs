using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Community.PowerToys.Run.Plugin.Everything.SearchHelper;
using NLog;
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

        internal IEnumerable<Result> Query(string query, Settings setting, CancellationToken token)
        {
            if (setting.LoggingLevel <= LogLevel.Debug)
            {
                Log.Info(
                    $"EPT:\nNew Query: {query}\n" +
                    $"Prefix {setting.Prefix}\n" +
                    $"Sort {(int)setting.Sort}_{Everything_GetSort()}\n" +
                    $"Max {setting.Max}_{Everything_GetMax()}\n" +
                    $"Match Path {setting.MatchPath}_{Everything_GetMatchPath()}\n" +
                    $"Regex {setting.RegEx}_{Everything_GetRegex()}",
                    GetType());
            }

            string orgqry = query;

            if (!string.IsNullOrEmpty(setting.Prefix))
                query = setting.Prefix + query;

            if (setting.EnvVar && orgqry.Contains('%'))
            {
                query = Environment.ExpandEnvironmentVariables(query).Replace(';', '|');
                if (setting.LoggingLevel <= LogLevel.Debug)
                    Log.Info($"EPT:EnvVariable\n{query}", GetType());
            }

            if (setting.Is1_4 && orgqry.Contains(':'))
            {
                foreach (var kv in setting.Filters)
                {
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Replace(kv.Key, string.Empty).Trim() + $" {kv.Value}";
                        if (setting.LoggingLevel <= LogLevel.Debug)
                            Log.Info($"EPT: Contains Filter: {kv.Key}\n{query}", GetType());
                    }
                }
            }

            token.ThrowIfCancellationRequested();
            Everything_SetSearchW(query);
            if (!Everything_QueryW(true))
            {
                if (setting.LoggingLevel < LogLevel.Error)
                    Log.Warn($"EPT: Unable to Query ({Everything_GetLastError()})", GetType());
                throw new Win32Exception("Unable to Query");
            }

            uint resultCount = Everything_GetNumResults();
            if (setting.LoggingLevel <= LogLevel.Debug)
                Log.Info($"EPT: Results = {resultCount}", GetType());

            token.ThrowIfCancellationRequested();
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
                token.ThrowIfCancellationRequested();
                string name = Marshal.PtrToStringUni(Everything_GetResultFileNameW(i));
                string path = Marshal.PtrToStringUni(Everything_GetResultPathW(i));
                if (setting.LoggingLevel < LogLevel.Error && (name == null || path == null))
                {
                    Log.Warn($"EPT: Result {i} is null for {name} and/or {path}, query: {query}", GetType());
                    continue;
                }

                string fullPath = Path.Combine(path, name);

                bool isFolder = Everything_IsFolderResult(i);
                if (isFolder)
                    path = fullPath;

                if (setting.LoggingLevel <= LogLevel.Debug)
                {
                    Log.Info(
                        $"=====EPT: RESULT #{i} =====\n" +
                        $"Folder    : {isFolder}\n" +
                        $"File Path : ({path.Length}) {(setting.LoggingLevel == LogLevel.Trace ? path : string.Empty)}\n" +
                        $"File Name : ({name.Length}) {(setting.LoggingLevel == LogLevel.Trace ? name : string.Empty)}\n" +
                        $"Ext       : {Path.GetExtension(fullPath)}",
                        GetType());
                }

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
