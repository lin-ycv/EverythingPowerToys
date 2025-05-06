using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Community.PowerToys.Run.Plugin.Everything.SearchHelper;
using Microsoft.Win32;
using NLog;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Everything
    {
        private bool firstRun = true;
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
            else if (string.IsNullOrEmpty(exe) && firstRun)
            {
                string a64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything64.exe"),
                    s64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.exe"),
                    a32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything 1.5", "Everything.exe"),
                    s32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.exe");

                if (Path.Exists(a64)) { exe = a64; }
                else if (Path.Exists(s64)) { exe = s64; }
                else if (Path.Exists(a32)) { exe = a32; }
                else if (Path.Exists(s32)) { exe = s32; }
                else
                {
                    try
                    {
                        // Check uninstall information in registry for installation locations
                        string[] uninstallKeys =
                        [
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a",
                        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything",
                        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Everything 1.5a"
                        ];

                        foreach (string subKey in uninstallKeys)
                        {
                            using RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);
                            if (key != null)
                            {
                                // Check install location from registry
                                if (key.GetValue("InstallLocation") is string installLocation && Path.Exists(installLocation))
                                {
                                    string exe32 = Path.Combine(installLocation, "Everything.exe");
                                    string exe64 = Path.Combine(installLocation, "Everything64.exe");

                                    if (File.Exists(exe64))
                                    {
                                        exe = exe64;
                                        break;
                                    }

                                    if (File.Exists(exe32))
                                    {
                                        exe = exe32;
                                        break;
                                    }
                                }

                                // Try to extract location from uninstall string
                                if (key.GetValue("UninstallString") is string uninstallString)
                                {
                                    string dir = Path.GetDirectoryName(uninstallString.Contains('"') ? uninstallString.Split('"')[1] : uninstallString);
                                    if (dir != null)
                                    {
                                        string exe32 = Path.Combine(dir, "Everything.exe");
                                        string exe64 = Path.Combine(dir, "Everything64.exe");

                                        if (File.Exists(exe64))
                                        {
                                            exe = exe64;
                                            break;
                                        }

                                        if (File.Exists(exe32))
                                        {
                                            exe = exe32;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        exe = string.Empty;
                    }
                }

                firstRun = false;
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
