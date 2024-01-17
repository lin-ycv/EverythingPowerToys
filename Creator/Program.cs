using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Resources;
using System.Text.Json;
using System.Windows.Forms;

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
            };
            DialogResult result = folderDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                string folderPath = folderDialog.SelectedPath;
                Console.WriteLine($"You selected: {folderPath}");

                string base64Zip = string.Empty;
                using (MemoryStream memoryStream = new())
                {
                    ZipFile.CreateFromDirectory(folderPath, memoryStream, CompressionLevel.SmallestSize, true);
                    base64Zip = Convert.ToBase64String(memoryStream.ToArray());
                }

                Console.WriteLine("Base64 representation of the ZIP file:" + base64Zip);

                string pluginJsonPath = Path.Combine(folderPath, "plugin.json");
                string jsonContent = File.ReadAllText(pluginJsonPath);

                JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
                JsonElement root = jsonDocument.RootElement;

                string version = root.GetProperty("Version").GetString();
                Console.WriteLine($"Version from plugin.json: {version}");

                ModResx(version, base64Zip, out string otherProjectPath);
                if(string.IsNullOrEmpty(otherProjectPath))
                    return;
                Console.WriteLine(otherProjectPath);

                BuildInstaller(otherProjectPath, version);
                Console.ReadKey();
            }
            else
                Console.WriteLine("No folder selected.");
        }

        static void ModResx(string version, string base64Zip, out string path)
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Choose RESX File",
                FileName = "Resources.resx",
                Filter = "RESX Files (*.resx)|*.resx|All Files (*.*)|*.*",
                DefaultExt = "resx"
            };
            openFileDialog.InitialDirectory = Path.Combine(Environment.CurrentDirectory, "Installer\\Properties");

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
                        else if (key == "versionKey")
                            modifiedEntries[key] = version;
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
                path= string.Empty;
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
    }
}
