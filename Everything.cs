using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Wox.Plugin;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Everything
    {
        internal Everything(Settings setting)
        {
            Everything_SetRequestFlags(Request.FULL_PATH_AND_FILE_NAME);
            UpdateSettings(setting);
        }

        internal void UpdateSettings(Settings setting)
        {
            Everything_SetSort(setting.Sort);
            Everything_SetMax(setting.Max);
            Everything_SetMatchPath(setting.MatchPath);
            Everything_SetRegex(setting.RegEx);
        }

        internal IEnumerable<Result> Query(string query, Settings setting)
        {
            if (setting.Log > LogLevel.None)
            {
                Debugger.Write($"\r\n\r\nNew Query: {query}\r\n" +
                    $"Sort {(int)setting.Sort}_{Everything_GetSort()} | " +
                    $"Max {setting.Max}_{Everything_GetMax()} | " +
                    $"Match Path {setting.MatchPath}_{Everything_GetMatchPath()} | " +
                    $"Regex {setting.RegEx}_{Everything_GetRegex()}");
            }

            string orgqry = query;
            
            if (setting.EnvVar && orgqry.Contains('%'))
            {
                query = Environment.ExpandEnvironmentVariables(query).Replace(';', '|');
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"EnvVariable\r\n{query}");
            }

            if (orgqry.Contains(':'))
            {
                foreach (var kv in setting.Filters)
                {
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Replace(kv.Key, kv.Value);
                        if (setting.Log > LogLevel.None)
                            Debugger.Write($"Contains Filter: {kv.Key}\r\n{query}");
                    }
                }
            }

            Everything_SetSearchW(query);
            if (!Everything_QueryW(true))
            {
                if (setting.Log > LogLevel.None)
                    Debugger.Write("\r\nUnable to Query\r\n");

                throw new Win32Exception("Unable to Query");
            }

            uint resultCount = Everything_GetNumResults();
            if (setting.Log > LogLevel.None)
                Debugger.Write($"Results: {resultCount}");

            for (uint i = 0; i < resultCount; i++)
            {
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"\r\n===== RESULT #{i} =====");

                char[] buffer = new char[32767];
                uint length = Everything_GetResultFullPathNameW(i, buffer, (uint)buffer.Length);

                string fullPath = new(buffer, 0, (int)length);
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"{length} {(setting.Log == LogLevel.Verbose ? fullPath : string.Empty)}");
                string name = Path.GetFileName(fullPath);
                bool isFolder = Everything_IsFolderResult(i);

                string path = isFolder ? fullPath : Path.GetDirectoryName(fullPath);
                string ext = Path.GetExtension(fullPath.Replace(".lnk", string.Empty));
                if (setting.Log > LogLevel.None)
                    Debugger.Write($"Folder: {isFolder}\r\nFile Path {(setting.Log == LogLevel.Verbose ? path : path.Length)}\r\nFile Name {(setting.Log == LogLevel.Verbose ? name : name.Length)}\r\nExt: {ext}");

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
                            _ = Everything_IncRunCountFromFileNameW(fullPath);
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
