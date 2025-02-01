using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Community.PowerToys.Run.Plugin.Everything3.Properties;
using NLog;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Everything3.Update
{
    internal sealed class UpdateChecker
    {
        private readonly CompositeFormat updateAvailable = CompositeFormat.Parse(Resources.UpdateAvailable);

        internal async Task Async(Version v, Settings s, UpdateSettings us, bool isArm)
        {
            string apiUrl = "https://api.github.com/repos/lin-ycv/EverythingPowerToys/releases/latest";
            if (s.LoggingLevel <= LogLevel.Info) Log.Info("EPT: Checking Update...", GetType());

            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (s.LoggingLevel <= LogLevel.Debug) Log.Info($"EPT:  Response: {response.StatusCode}", GetType());

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    JsonElement root = jsonDocument.RootElement;
                    Version latest = Version.TryParse(root.GetProperty("tag_name").GetString().AsSpan(1), out var vNumber)
                        ? vNumber
                        : Version.Parse(root.GetProperty("tag_name").GetString());
                    if (s.LoggingLevel <= LogLevel.Debug) Log.Info($"EPT:\n\tLastest: {latest}\n\tSkip: {us.Skip}", GetType());

                    if (latest > v && latest != us.Skip)
                    {
                        MessageBoxResult mbox = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, updateAvailable, v, latest), "EPT: Updater", MessageBoxButton.YesNoCancel);
                        if (mbox == MessageBoxResult.Yes && root.TryGetProperty("assets", out JsonElement assets))
                        {
                            string[] nameUrl = [string.Empty, string.Empty];
                            foreach (JsonElement asset in assets.EnumerateArray())
                            {
                                if (asset.TryGetProperty("browser_download_url", out JsonElement downUrl) && downUrl.ToString().EndsWith("x64-SDK3.exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    nameUrl[0] = asset.GetProperty("name").ToString();
                                    nameUrl[1] = downUrl.ToString();
                                }
                            }

                            if (nameUrl[0].Length > 0)
                            {
                                byte[] fileContent = await httpClient.GetByteArrayAsync(nameUrl[1]);
                                string fileName = Path.Combine(Path.GetTempPath(), nameUrl[0]);
                                File.WriteAllBytes(fileName, fileContent);
                                Process.Start(fileName);

                                foreach (Process pt in Process.GetProcessesByName("PowerToys"))
                                    pt.Kill();
                            }
                            else
                            {
                                ProcessStartInfo p = new("https://github.com/lin-ycv/EverythingPowerToys/releases/latest")
                                {
                                    UseShellExecute = true,
                                    Verb = "Open",
                                };
                                Process.Start(p);
                            }
                        }
                        else if (mbox == MessageBoxResult.No)
                        {
                            us.Skip = latest;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (s.LoggingLevel <= LogLevel.Info)
                    Log.Exception($"EPT: Unable to check for update", e, GetType());
            }

            if (s.LoggingLevel <= LogLevel.Info)
                Log.Info("EPT:  Checking Update...Done", GetType());
        }
    }
}
