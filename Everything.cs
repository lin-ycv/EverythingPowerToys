using System.ComponentModel;

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
                foreach (var kv in setting.Filters)
                {
                    if (query.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Replace(kv.Key, kv.Value);
                    }
                }
            }

            Everything_SetSearchW(query);
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
                char[] buffer = new char[260];
                uint length = Everything_GetResultFullPathName(i, buffer, (uint)buffer.Length);
                string fullPath = new(buffer, 0, (int)length);
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
