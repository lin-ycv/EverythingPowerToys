namespace Community.PowerToys.Run.Plugin.Everything
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Community.PowerToys.Run.Plugin.Everything.Interop;
    using Wox.Plugin.Logger;

    internal class Settings
    {
        // Settings from PTR settings
        internal bool Copy { get; set; } = false;
        internal bool MatchPath { get; set; } = false;
        internal bool Preview { get; set; } = false;
        internal bool QueryText { get; set; } = false;
        internal bool RegEx { get; set; } = false;
        internal bool Debug { get; set; } = false;

        // Settings from settings.toml
        internal uint Max { get; set; } = 20;
        internal int Sort { get; set; } = 14;
        internal Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();
        internal bool SkipUpdate { get; set; } = false;
        internal Settings()
        {
            string[] strArr;
            try { strArr = File.ReadAllLines("modules\\launcher\\Plugins\\Everything\\settings.toml"); }
            catch { return; }
            var culture = new System.Globalization.CultureInfo("en-US");
            foreach (string str in strArr)
            {
                if (str.Length == 0 || str[0] == '#') continue;
                string[] kv = str.Split('=', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
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
                    case "skip":
                        if (kv[1][0] == 'Y') SkipUpdate = true;
                        break;
                    default:
                        if (kv[0].Contains(':'))
                            Filters.TryAdd(kv[0].Split(':')[0].ToLowerInvariant(), kv[1]);
                        break;
                }
            }

            if (Debug)
            {
                string msg = $"Max: {Max}\nSort: {Sort}\nFilters: {string.Join("\n - ", Filters.Select(x => { return x.Key + "_" + x.Value; }))}";
                Log.Info(msg, typeof(NativeMethods));
            }
        }
    }
}
