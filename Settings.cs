﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Settings
    {
        // Settings from PTR settings
        public Sort Sort { get; set; } = Sort.NAME_ASCENDING;
        public uint Max { get; set; } = 10;
        public string Context { get; set; } = "0123456";
        public bool Copy { get; set; }
        public bool MatchPath { get; set; }
        public bool Preview { get; set; } = true;
        public bool QueryText { get; set; }
        public bool RegEx { get; set; }
        public bool EnvVar { get; set; }
        public bool Updates { get; set; } = true;
        public string Skip { get; set; }
        public string Prefix { get; set; }
        public string EverythingPath { get; set; }
        public bool ShowMore { get; set; } = true;
        public string CustomProgram { get; set; } = "notepad.exe";
        public string CustomArg { get; set; } = "$P";
#if DEBUG
        public LogLevel Log { get; set; } = LogLevel.None;
#endif

        // Get Filters from settings.toml
        public Dictionary<string, string> Filters { get; } = [];
        internal void Getfilters()
        {
#if DEBUG
            if (Log > LogLevel.None)
                Debugger.Write("2.Getting Filters...");
#endif
            string[] strArr;
            try { strArr = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.toml")); }
            catch (Exception e)
            {
#if DEBUG
                if (Log > LogLevel.None)
                    Debugger.Write($"\r\nERROR: {e.Message}\r\n");
#endif
                Wox.Plugin.Logger.Log.Error($"Error reading settings.toml: {e.Message}", GetType());
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
#if DEBUG
            if (Log > LogLevel.None)
                Debugger.Write(Log > LogLevel.Debug ? string.Join(Environment.NewLine, Filters) + "\r\n" : string.Empty + "  GettingFilters...Done");
#endif
        }
    }
#if DEBUG
    public enum LogLevel
    {
        None,
        Debug,
        Verbose,
    }
#endif
}
