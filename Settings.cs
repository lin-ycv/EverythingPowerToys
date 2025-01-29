using NLog;
using static Community.PowerToys.Run.Plugin.Everything3.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything3
{
    public class Settings
    {
        internal string InstanceName { get; set; } = "1.5a";
        internal Sort Sort { get; set; } = Sort.RUN_COUNT;
        internal Sort Sort2 { get; set; } = Sort.DATE_MODIFIED;
        internal bool Descending { get; set; }
        internal bool Descending2 { get; set; }
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
    }
}
