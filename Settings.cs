using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NLog;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Settings
    {
        internal bool Is1_4 { get; set; }

        // Settings from PTR settings
        internal Sort Sort { get; set; } = Sort.NAME_ASCENDING;
        internal uint Max { get; set; } = 10;
        internal string Context { get; set; } = "01234568";
        internal bool Copy { get; set; }
        internal bool MatchPath { get; set; }
        internal bool Preview { get; set; } = true;
        internal bool QueryText { get; set; }
        internal bool RegEx { get; set; }
        internal bool EnvVar { get; set; }
        internal bool Updates { get; set; } = true;
        internal string Prefix { get; set; }
        internal string EverythingPath { get; set; }
        internal bool ShowMore { get; set; } = true;
        internal string CustomProgram { get; set; } = "notepad.exe";
        internal string CustomArg { get; set; } = "$P";
        internal LogLevel LoggingLevel { get; set; } = LogLevel.Error;

        // Get Filters from settings.toml
        public Dictionary<string, string> Filters { get; } = [];

        internal void Getfilters()
        {
            Is1_4 = true;
            if (LoggingLevel <= LogLevel.Info)
                Log.Info("User on Everything 1.4, GettingFilters...", GetType());

            string[] strArr;
            try { strArr = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.toml")); }
            catch (Exception e)
            {
                Log.Error($"Error reading settings.toml: {e.Message}", GetType());
                return;
            }

            foreach (string str in strArr)
            {
                if (str.Length == 0 || str[0] == '#') continue;
                string[] kv = str.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (kv.Length != 2) continue;

                if (kv[0].Contains(':'))
                    Filters.TryAdd(kv[0].ToLowerInvariant(), kv[1] + (kv[1].EndsWith(';') ? ' ' : string.Empty));
            }

            if (LoggingLevel <= LogLevel.Info)
                Log.Info(LoggingLevel < LogLevel.Debug ? string.Join(Environment.NewLine, Filters) : "  GettingFilters...Done", GetType());
        }
    }
}
