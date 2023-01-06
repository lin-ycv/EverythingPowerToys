using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Everything
{
    internal class ContextMenuLoader : IContextMenu
    {
        private readonly PluginInitContext _context;

        // Extensions for adding run as admin context menu item for applications
        private readonly string[] _appExtensions = { ".exe", ".bat", ".appref-ms", ".lnk" };

        private bool _swapCopy;
        private int[] _options;
        internal void UpdateCopy(bool swapCopy)
        {
            _swapCopy = swapCopy;
        }

        public ContextMenuLoader(PluginInitContext context, int[] options)
        {
            _context = context;
            _options = options;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var contextMenus = new List<ContextMenuResult>();
            if (selectedResult.ContextData is SearchResult record)
            {
                bool isFile = record.File, runAs = CanFileBeRunAsAdmin(record.Path);
                foreach (int o in _options)
                {
                    switch (o)
                    {
                        case 0:
                            // Open folder
                            if (isFile)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Properties.Resources.open_containing_folder,
                                    Glyph = "\xE838",
                                    FontFamily = "Segoe MDL2 Assets",
                                    AcceleratorKey = Key.E,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = _ =>
                                    {
                                        if (!Helper.OpenInShell("explorer.exe", $"/select,\"{record.Path}\""))
                                        {
                                            var message = $"{Properties.Resources.folder_open_failed} {Path.GetDirectoryName(record.Path)}";
                                            _context.API.ShowMsg(message);
                                            return false;
                                        }

                                        return true;
                                    },
                                });
                            }

                            break;
                        case 1:
                            // Run as Adsmin
                            if (runAs)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Properties.Resources.run_as_admin,
                                    Glyph = "\xE7EF",
                                    FontFamily = "Segoe MDL2 Assets",
                                    AcceleratorKey = Key.Enter,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = _ =>
                                    {
                                        try
                                        {
                                            Task.Run(() => Helper.RunAsAdmin(record.Path));
                                            return true;
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Exception($"Failed to run {record.Path} as admin, {e.Message}", e, MethodBase.GetCurrentMethod().DeclaringType);
                                            return false;
                                        }
                                    },
                                });
                            }

                            break;
                        case 2:
                            // Run as User
                            if (runAs)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Properties.Resources.run_as_user,
                                    Glyph = "\xE7EE",
                                    FontFamily = "Segoe MDL2 Assets",
                                    AcceleratorKey = Key.U,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = _ =>
                                    {
                                        try
                                        {
                                            Task.Run(() => Helper.RunAsUser(record.Path));
                                            return true;
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Exception($"Failed to run {record.Path} as different user, {e.Message}", e, MethodBase.GetCurrentMethod().DeclaringType);
                                            return false;
                                        }
                                    },
                                });
                            }

                            break;
                        case 3:
                            // Copy File/Folder
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Properties.Resources.copy_file + (_swapCopy ? Properties.Resources.copy_shortcut : Properties.Resources.copy_shortcutAlt),
                                Glyph = "\xE8C8",
                                FontFamily = "Segoe MDL2 Assets",
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = _swapCopy ? ModifierKeys.Control : ModifierKeys.Control | ModifierKeys.Alt,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        Clipboard.SetData(DataFormats.FileDrop, new string[] { record.Path });
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        var message = Properties.Resources.clipboard_failed;
                                        Log.Exception(message, e, GetType());

                                        _context.API.ShowMsg(message);
                                        return false;
                                    }
                                },
                            });
                            break;
                        case 4:
                            // Copy Path
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Properties.Resources.copy_path + (_swapCopy ? Properties.Resources.copy_shortcutAlt : Properties.Resources.copy_shortcut),
                                Glyph = "\xE71B",
                                FontFamily = "Segoe MDL2 Assets",
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = _swapCopy ? ModifierKeys.Control | ModifierKeys.Alt : ModifierKeys.Control,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        Clipboard.SetText(record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        var message = Properties.Resources.clipboard_failed;
                                        Log.Exception(message, e, GetType());

                                        _context.API.ShowMsg(message);
                                        return false;
                                    }
                                },
                            });
                            break;
                        case 5:
                            // Open in Shell
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Properties.Resources.open_in_console,
                                Glyph = "\xE756",
                                FontFamily = "Segoe MDL2 Assets",
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        if (isFile)
                                        {
                                            Helper.OpenInConsole(Path.GetDirectoryName(record.Path));
                                        }
                                        else
                                        {
                                            Helper.OpenInConsole(record.Path);
                                        }

                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Exception($"Failed to open {record.Path} in console, {e.Message}", e, GetType());
                                        return false;
                                    }
                                },
                            });
                            break;
                    }
                }
            }

            return contextMenus;
        }

        private bool CanFileBeRunAsAdmin(string path)
        {
            string fileExtension = Path.GetExtension(path);
            foreach (string extension in _appExtensions)
            {
                // Using OrdinalIgnoreCase since this is internal
                if (extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
