using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Community.PowerToys.Run.Plugin.Everything.Properties;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Update
    {
        internal async Task UpdateAsync(Version v, Settings s)
        {
            string apiUrl = "https://api.github.com/repos/lin-ycv/EverythingPowerToys/releases/latest";
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    JsonElement root = jsonDocument.RootElement;
                    Version latest = Version.TryParse(root.GetProperty("tag_name").GetString().AsSpan(1), out var vNumber)
                        ? vNumber
                        : Version.Parse(root.GetProperty("tag_name").GetString());
                    if (latest > v && latest.ToString() != s.Skip)
                    {
                        MessageBoxResult mbox = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.UpdateAvailable, v, latest), "Updater", MessageBoxButton.YesNoCancel);
                        if (mbox == MessageBoxResult.Yes && root.TryGetProperty("assets", out JsonElement assets))
                        {
                            string[] nameUrl = [string.Empty, string.Empty];
                            foreach (JsonElement asset in assets.EnumerateArray())
                            {
                                if (asset.TryGetProperty("browser_download_url", out JsonElement downUrl) && downUrl.ToString().EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    nameUrl[0] = asset.GetProperty("name").ToString();
                                    nameUrl[1] = downUrl.ToString();
                                }
                            }

                            byte[] fileContent = await httpClient.GetByteArrayAsync(nameUrl[1]);
                            string fileName = Path.Combine(Path.GetTempPath(), nameUrl[0]);
                            File.WriteAllBytes(fileName, fileContent);
                            using Process updater = Process.Start(fileName);
                            updater.WaitForExit();
                            if (updater.ExitCode == 1)
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
            catch { }
        }
    }
}
