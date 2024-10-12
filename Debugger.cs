#if DEBUG
using System;
using System.IO;
namespace Community.PowerToys.Run.Plugin.Everything
{
    internal static class Debugger
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "EverythingPT.log");
        public static void Write(string message)
        {
            using StreamWriter writer = new(FilePath, true);
            writer.WriteLine(message);
        }
    }
}
#endif
