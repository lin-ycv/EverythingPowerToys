using System.Diagnostics;
using System.IO.Compression;
using Res = Installer.Properties.Resources;
namespace Installer
{
    internal class Program
    {
        static readonly List<string> torestart = [];
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "EverythingPowerToys Installer\\Updater";
                EndPowerToys();
                Console.WriteLine("Extracting EverythingPowerToys...");
                Thread.Sleep(1000);
                string extractionFolder = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins");
                using MemoryStream memoryStream = new(Convert.FromBase64String(Res.base64zipKey));
                using ZipArchive archive = new(memoryStream);
                archive.ExtractToDirectory(extractionFolder, true);
                Console.WriteLine("EverythingPowerToys Installed");
                Thread.Sleep(1000);
                StartPowerToys();
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Environment.ExitCode = 1;
            }
            Console.WriteLine("Press any key to exit...");
#if DEBUG
            Console.ReadKey();
#endif
            Environment.Exit(Environment.ExitCode);
        }
        static void EndPowerToys()
        {
            Console.WriteLine("Closing PowerToys...");
            Process[] processes = Process.GetProcessesByName("PowerToys");
            foreach (Process process in processes)
            {
                if (process.MainModule != null)
                    torestart.Add(process.MainModule.FileName);
                process.CloseMainWindow();
                process.Kill();
                process.WaitForExit();
            }
        }
        static void StartPowerToys()
        {
            Console.WriteLine("Restarting PowerToys...");
            foreach (string file in torestart)
                Process.Start(file);
        }
    }
}
