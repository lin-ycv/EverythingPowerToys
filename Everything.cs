using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Community.PowerToys.Run.Plugin.Everything3.Properties;
using Community.PowerToys.Run.Plugin.Everything3.SearchHelper;
using Microsoft.Win32;
using NLog;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything3.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything3
{
    internal sealed class Everything
    {
        private readonly string subHeader;
        private string exe = string.Empty;
        internal IntPtr Client { get; set; }
        internal IntPtr SearchState { get; set; }
        internal Everything(Settings setting)
        {
            Client = Everything3_ConnectW(setting.InstanceName);
            if (Client != IntPtr.Zero)
            {
                bool isDefault = setting.InstanceName == string.Empty || setting.InstanceName == "1.5a";
                subHeader = isDefault ? Resources.plugin_name : setting.InstanceName;
            }
            else
            {
                Client = Everything3_ConnectW("1.5a");
                if (Client == IntPtr.Zero)
                {
                    Client = Everything3_ConnectW(string.Empty);
                    if (Client == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to connect to Everything service");
                }

                subHeader = Resources.plugin_name;
            }

            SearchState = Everything3_CreateSearchState();
            if (SearchState == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create search state");

            UpdateSettings(setting);
        }

        internal void UpdateSettings(Settings setting)
        {
            Everything3_ClearSearchSorts(SearchState);
            Everything3_AddSearchSort(SearchState, setting.Sort, !setting.Descending);
            Everything3_AddSearchSort(SearchState, setting.Sort2, !setting.Descending2);
            Everything3_SetSearchRegex(SearchState, setting.RegEx);
            Everything3_SetSearchMatchPath(SearchState, setting.MatchPath);
            if (!string.IsNullOrEmpty(setting.EverythingPath))
            {
                if (setting.EverythingPath != exe && Path.Exists(setting.EverythingPath))
                    exe = setting.EverythingPath;
            }
            else if (string.IsNullOrEmpty(exe))
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
            }
        }

        internal IEnumerable<Result> Query(string query, Settings setting, CancellationToken token)
        {
            if (setting.LoggingLevel <= LogLevel.Debug)
            {
                Log.Info(
                    $"EPT3:\nNew Query: {query}\n" +
                    $"Prefix: {setting.Prefix}",
                    GetType());
            }

            string orgqry = query;

            //query = $"Count:{setting.Max} " + query; // Limits number of returned results, but does not work with sorting

            if (!string.IsNullOrEmpty(setting.Prefix))
                query = setting.Prefix + query;

            if (setting.EnvVar && orgqry.Contains('%'))
            {
                query = Environment.ExpandEnvironmentVariables(query).Replace(';', '|');
                if (setting.LoggingLevel <= LogLevel.Debug)
                    Log.Info($"EPT3:EnvVariable\n{query}", GetType());
            }

            token.ThrowIfCancellationRequested();
            Everything3_SetSearchTextW(SearchState, query);
            IntPtr resultList = Everything3_Search(Client, SearchState);
            if (resultList == IntPtr.Zero)
            {
                if (setting.LoggingLevel < LogLevel.Error)
                    Log.Warn($"EPT3: Unable to Query", GetType());
                throw new Win32Exception("Unable to Query");
            }

            ulong resultCount = Everything3_GetResultListCount(resultList);
            if (setting.LoggingLevel <= LogLevel.Debug)
                Log.Info($"EPT3: Results = {resultCount}", GetType());

            if (token.IsCancellationRequested)
            {
                Everything3_DestroyResultList(resultList);
                token.ThrowIfCancellationRequested();
            }

            bool showMore = setting.ShowMore && !string.IsNullOrEmpty(exe) && resultCount >= setting.Max;
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

            char[] pathBuffer = new char[1024];
            for (uint i = 0; i < Math.Min(resultCount, setting.Max); i++)
            {
                if (token.IsCancellationRequested)
                {
                    Everything3_DestroyResultList(resultList);
                    token.ThrowIfCancellationRequested();
                }

                uint length = Everything3_GetResultFullPathNameW(resultList, i, pathBuffer, (uint)pathBuffer.Length);
                string fullPath = new(pathBuffer, 0, (int)length);
                string name = Path.GetFileName(fullPath);
                string path = Path.GetDirectoryName(fullPath);
                if (setting.LoggingLevel < LogLevel.Error && (name == null || path == null))
                {
                    Log.Warn($"EPT3: Result {i} is null for {name} and/or {path}, query: {query}", GetType());
                    continue;
                }

                bool isFolder = Everything3_IsFolderResult(resultList, i);
                if (isFolder)
                    path = fullPath;

                if (setting.LoggingLevel <= LogLevel.Debug)
                {
                    Log.Info(
                        $"=====EPT3: RESULT #{i} =====\n" +
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
                    SubTitle = subHeader + ": " + fullPath,

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

                            _ = Everything3_IncRunCountFromFilenameW(Client, fullPath);
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

            Everything3_DestroyResultList(resultList);
        }
    }
}
