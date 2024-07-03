using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Community.PowerToys.Run.Plugin.Everything.Properties;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Update
    {
        private readonly CompositeFormat updateAvailable = CompositeFormat.Parse(Resources.UpdateAvailable);
        internal async Task UpdateAsync(Version v, Settings s)
        {
            string apiUrl = "https://api.github.com/repos/lin-ycv/EverythingPowerToys/releases/latest";
            if (s.Log > LogLevel.None)
                Debugger.Write("1.Checking Update...");

            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (s.Log == LogLevel.Verbose) Debugger.Write($"\tResponse: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    JsonElement root = jsonDocument.RootElement;
                    Version latest = Version.TryParse(root.GetProperty("tag_name").GetString().AsSpan(1), out var vNumber)
                        ? vNumber
                        : Version.Parse(root.GetProperty("tag_name").GetString());
                    if (latest > v && latest.ToString() != s.Skip)
                    {
                        MessageBoxResult mbox = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, updateAvailable, v, latest), "Updater", MessageBoxButton.YesNoCancel);
                        if (mbox == MessageBoxResult.Yes && root.TryGetProperty("assets", out JsonElement assets))
                        {
                            string[] nameUrl = [string.Empty, string.Empty];
                            foreach (JsonElement asset in assets.EnumerateArray())
                            {
#if X64
                                if (asset.TryGetProperty("browser_download_url", out JsonElement downUrl) && downUrl.ToString().EndsWith("x64.exe", StringComparison.OrdinalIgnoreCase))
#elif ARM64
                                if (asset.TryGetProperty("browser_download_url", out JsonElement downUrl) && downUrl.ToString().EndsWith("ARM64.exe", StringComparison.OrdinalIgnoreCase))
#endif
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
                            s.Skip = latest.ToString();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (s.Log > LogLevel.None)
                    Debugger.Write($"\r\nERROR: {e.Message}\r\n{e.StackTrace}\r\n");
            }

            if (s.Log > LogLevel.None)
                Debugger.Write("  Checking Update...Done");
        }
    }
}
