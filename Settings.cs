using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal class Settings
    {
        // Settings from PTR settings
        internal bool Copy { get; set; } = false;
        internal bool MatchPath { get; set; } = false;
        internal bool Preview { get; set; } = false;
        internal bool QueryText { get; set; } = false;
        internal bool RegEx { get; set; } = false;
        internal bool Updates { get; set; } = true;

        // Settings from settings.toml
        internal uint Max { get; } = 20;
        internal int Sort { get; } = 14;
        internal int[] Options { get; } = new int[] { 0, 1, 2, 3, 4, 5 };
        internal Dictionary<string, string> Filters { get; } = new Dictionary<string, string>();
        internal Settings()
        {
            string[] strArr;
            try { strArr = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.toml")); }
            catch { return; }
            var culture = new System.Globalization.CultureInfo("en-US");
            foreach (string str in strArr)
            {
                if (str.Length == 0 || str[0] == '#') continue;
                string[] kv = str.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (kv.Length != 2) continue;
                switch (kv[0])
                {
                    case "max":
                        try { Max = uint.Parse(kv[1], culture.NumberFormat); }
                        catch { }
                        break;
                    case "sort":
                        try { Sort = int.Parse(kv[1], culture.NumberFormat); }
                        catch { }
                        break;
                    case "options":
                        Options = Array.ConvertAll(kv[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), int.Parse);
                        break;
                    default:
                        if (kv[0].Contains(':'))
                            Filters.TryAdd(kv[0].Split(':')[0].ToLowerInvariant(), kv[1]);
                        break;
                }
            }
        }
    }
}
