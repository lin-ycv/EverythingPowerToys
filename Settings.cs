namespace Community.PowerToys.Run.Plugin.Everything
{
    using System.Collections.Generic;
    using System.IO;

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
        internal string[] Options { get; }
        internal Dictionary<string, string> Filters { get; } = new Dictionary<string, string>();
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
                    case "options":
                        Options = kv[1].Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                        break;
                    default:
                        if (kv[0].Contains(':'))
                            Filters.TryAdd(kv[0].Split(':')[0].ToLowerInvariant(), kv[1]);
                        break;
                }
            }

            Options ??= new string[] { "0", "1", "2", "3", "4", "5" };
        }
    }
}
