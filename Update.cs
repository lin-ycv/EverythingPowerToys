using System;
using System.Diagnostics;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using Wox.Infrastructure.Storage;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal sealed class Update
    {
        internal Update(Version v, Settings s)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("https://img.shields.io/github/v/release/lin-ycv/everythingpowertoys");
                Version latest = Version.Parse(doc.GetElementsByTagName("title")[0].InnerXml.Split(':', StringSplitOptions.TrimEntries)[1].AsSpan(1));
                if (latest > v && latest.ToString() != s.Skip)
                {
                    MessageBoxResult mbox = MessageBox.Show($"New version available for EverythingPowerToys.\n\nInstalled:\t {v}\nLatest:\t {latest}", "Download Update?", MessageBoxButton.OKCancel);
                    if (mbox == MessageBoxResult.OK)
                    {
                        ProcessStartInfo p = new ProcessStartInfo("https://github.com/lin-ycv/EverythingPowerToys/releases/latest")
                        {
                            UseShellExecute = true,
                            Verb = "Open",
                        };
                        Process.Start(p);
                    }
                    else
                    {
                        s.Skip = latest.ToString();
                    }
                }
            }
            catch { }
        }
    }
}
