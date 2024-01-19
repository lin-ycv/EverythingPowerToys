using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            string orgqry = query;
            if (orgqry.Contains('\"') && !setting.MatchPath)
            {
                Everything_SetMatchPath(true);
            }

            if (setting.EnvVar && orgqry.Contains('%'))
            {
                query = Environment.ExpandEnvironmentVariables(query).Replace(';', '|');
            }

            if (orgqry.Contains(':'))
            {
                StringBuilder sb = new();
                foreach (var kv in setting.Filters)
                {
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(kv.Value + ';');
                        query = query.Replace(kv.Key, string.Empty);
                    }
                }

                if (sb.Length > 0)
                {
                    query = query.Trim() + " ext:" + sb.ToString();
                }
            }

            _ = Everything_SetSearchW(query);
            if (!Everything_QueryW(true))
            {
                throw new Win32Exception("Unable to Query");
            }

            if (orgqry.Contains('\"') && !setting.MatchPath)
            {
                Everything_SetMatchPath(false);
            }

            uint resultCount = Everything_GetNumResults();

            for (uint i = 0; i < resultCount; i++)
            {
                StringBuilder buffer = new(260);
                Everything_GetResultFullPathName(i, buffer, 260);
                string fullPath = buffer.ToString();
                string name = Path.GetFileName(fullPath);
                bool isFolder = Everything_IsFolderResult(i);
                string path = isFolder ? fullPath : Path.GetDirectoryName(fullPath);
                string ext = Path.GetExtension(fullPath.Replace(".lnk", string.Empty));

                var r = new Result()
                {
                    Title = name,
                    ToolTipData = new ToolTipData(name, fullPath),
                    SubTitle = Resources.plugin_name + ": " + fullPath,

                    IcoPath = isFolder ? "Images/folder.png" : (setting.Preview ?
                        fullPath : (SearchHelper.IconLoader.Icon(ext) ?? "Images/file.png")),
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
