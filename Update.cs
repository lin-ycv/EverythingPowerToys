namespace Community.PowerToys.Run.Plugin.Everything
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Xml;

    internal class Update
    {
        internal Update(Version v)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("https://img.shields.io/github/v/release/lin-ycv/everythingpowertoys");
                Version latest = Version.Parse(doc.GetElementsByTagName("title")[0].InnerXml.Split(':', StringSplitOptions.TrimEntries)[1].AsSpan(1));
                if (latest > v)
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
                }
            }
            catch { }
        }
    }
}
