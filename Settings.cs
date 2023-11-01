using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Settings
    {
        // Settings from PTR settings
        internal Interop.NativeMethods.Sort Sort { get; set; } = Interop.NativeMethods.Sort.DATE_MODIFIED_DESCENDING;
        internal uint Max { get; set; } = 20;
        internal string Context { get; set; } = "012345";
        internal bool Copy { get; set; }
        internal bool MatchPath { get; set; }
        internal bool Preview { get; set; } = true;
        internal bool QueryText { get; set; }
        internal bool RegEx { get; set; }
        internal bool Updates { get; set; } = true;
        internal string Skip { get; set; }

        // Get Filters from settings.toml
        internal Dictionary<string, string> Filters { get; } = new Dictionary<string, string>();
        internal void Getfilters()
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

                if (kv[0].Contains(':'))
                    Filters.TryAdd(kv[0].Split(':')[0].ToLowerInvariant(), kv[1]);
            }
        }
    }
}
