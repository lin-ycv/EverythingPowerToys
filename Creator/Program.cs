using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InstallerCreator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Select a folder:");

            using FolderBrowserDialog folderDialog = new()
            {
                Description = "Select Everything folder to add to ZIP",
                UseDescriptionForTitle = true,
                SelectedPath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, @"PowerToys\x64\Release\RunPlugins\Everything"),
            };
            DialogResult result = folderDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                string folderPath = folderDialog.SelectedPath;
                Console.WriteLine($"You selected: {folderPath}");

                string pluginJsonPath = Path.Combine(folderPath, "plugin.json");
                string jsonContent = File.ReadAllText(pluginJsonPath);

                JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
                JsonElement root = jsonDocument.RootElement;

                string? version = root.GetProperty("Version").GetString();
                if (string.IsNullOrEmpty(version))
                {
                    Console.WriteLine("Version not found in plugin.json");
                    return;
                }

                string base64Zip = string.Empty;
                string fileName = $"Everything-{version}-x64";
                StringBuilder sb = new("zip\r\n");
                using (MemoryStream memoryStream = new())
                {
                    ZipFile.CreateFromDirectory(folderPath, memoryStream, CompressionLevel.SmallestSize, true);
                    byte[] zipBytes = memoryStream.ToArray();
                    sb.AppendLine(CalcSHA256(zipBytes));
                    File.WriteAllBytes($"{fileName}.zip", zipBytes);
                    base64Zip = Convert.ToBase64String(zipBytes);
                }

                Console.WriteLine("Base64 representation of the ZIP file:" + base64Zip);

                Console.WriteLine($"Version from plugin.json: {version}");

                ModResx(base64Zip, out string otherProjectPath);
                if (string.IsNullOrEmpty(otherProjectPath))
                {
                    Console.WriteLine("No folder selected.");
                    return;
                }
                Console.WriteLine(otherProjectPath);

                BuildInstaller(otherProjectPath, version);
                try
                {
                    sb.AppendLine("exe\r\n"+ CalculateSHA256(fileName + ".exe"));
                    File.WriteAllText("CheckSum.txt", sb.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
            else
                Console.WriteLine("No folder selected.");
#if DEBUG
            Console.ReadKey();
#endif
        }

        static void ModResx(string base64Zip, out string path)
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Choose RESX File",
                FileName = "Resources.resx",
                Filter = "RESX Files (*.resx)|*.resx|All Files (*.*)|*.*",
                DefaultExt = "resx",
                InitialDirectory = Path.Combine(Environment.CurrentDirectory, "Installer\\Properties"),
            };

            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string resourceFilePath = openFileDialog.FileName;
                path = Directory.GetParent(Path.GetDirectoryName(resourceFilePath)).FullName;
                Console.WriteLine("Selected RESX File: " + resourceFilePath);

                Dictionary<string, object> modifiedEntries = [];

                using (ResXResourceReader reader = new(resourceFilePath))
                {
                    foreach (DictionaryEntry entry in reader)
                    {
                        string key = entry.Key.ToString();

                        if (key == "base64zipKey")
                            modifiedEntries[key] = base64Zip;
                        else
                            modifiedEntries[key] = entry.Value;
                    }
                }

                using (ResXResourceWriter writer = new(resourceFilePath))
                {
                    foreach (var entry in modifiedEntries)
                        writer.AddResource(entry.Key, entry.Value);
                }
                Console.WriteLine("Resource file updated successfully.");
            }
            else
            {
                Console.WriteLine("File selection canceled");
                path = string.Empty;
            }
        }

        static void BuildInstaller(string otherProjectPath, string version)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "dotnet",
                Arguments = $"publish \"{otherProjectPath}\" --configuration Release /p:PublishProfile=\"{otherProjectPath}\\Properties\\PublishProfiles\\FolderProfile.pubxml\" /p:Version={version} /p:OutputName=Everything-{version}-x64",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = psi };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            Console.WriteLine("Output:");
            Console.WriteLine(output);

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("Error:");
                Console.WriteLine(error);
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Build succeeded.");
            }
            else
            {
                Console.WriteLine($"Build failed with exit code {process.ExitCode}.");
            }
        }

        static string CalculateSHA256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return CalcSHA256(stream);
        }
        static string CalcSHA256(dynamic bytestream)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(bytestream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
