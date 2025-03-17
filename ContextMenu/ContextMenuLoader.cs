using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Community.PowerToys.Run.Plugin.Everything3.Interop;
using Community.PowerToys.Run.Plugin.Everything3.Properties;
using Community.PowerToys.Run.Plugin.Everything3.SearchHelper;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;
using wf = System.Windows.Forms;

namespace Community.PowerToys.Run.Plugin.Everything3.ContextMenu
{
    internal sealed class ContextMenuLoader(PluginInitContext context, string options) : IContextMenu
    {
        private readonly PluginInitContext _context = context;
        private readonly string font = "Segoe Fluent Icons,Segoe MDL2 Assets";

        // Extensions for adding run as admin context menu item for applications
        private readonly string[] _appExtensions = [".exe", ".bat", ".appref-ms", ".lnk"];
        internal IntPtr Client { get; set; }
        private bool _swapCopy;
        private string _options = options;
        private string _customProgram;
        private string _customArg;

        internal void Update(Settings s)
        {
            _swapCopy = s.Copy;
            _options = s.Context;
            _customProgram = s.CustomProgram;
            _customArg = s.CustomArg;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var contextMenus = new List<ContextMenuResult>();
            if (selectedResult.ContextData is SearchResult record)
            {
                bool isFile = record.File, runAs = CanFileBeRunAsAdmin(record.Path);
                foreach (char o in _options)
                {
                    switch (o)
                    {
                        case '0':
                            // Open folder
                            if (isFile)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Resources.open_containing_folder,
                                    Glyph = "\xE838",
                                    FontFamily = font,
                                    AcceleratorKey = Key.E,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = (context) =>
                                    {
                                        if (!Helper.OpenInShell("explorer.exe", $"/select,\"{record.Path}\""))
                                        {
                                            var message = $"{Resources.folder_open_failed} {Path.GetDirectoryName(record.Path)}";
                                            _context.API.ShowMsg(message);
                                            Log.Exception($"ETP3: Failed to open folder {Path.GetDirectoryName(record.Path)}", new IOException(), GetType());
                                            return false;
                                        }

                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    },
                                });
                            }

                            break;
                        case '1':
                            // Run as Admin
                            if (runAs)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Resources.run_as_admin,
                                    Glyph = "\xE7EF",
                                    FontFamily = font,
                                    AcceleratorKey = Key.Enter,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = (context) =>
                                    {
                                        try
                                        {
                                            Task.Run(() => Helper.RunAsAdmin(record.Path));
                                            _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                            return true;
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Exception($"EPT3: Failed to run {record.Path} as admin,", e, GetType());
                                            return false;
                                        }
                                    },
                                });
                            }

                            break;
                        case '2':
                            // Run as User
                            if (runAs)
                            {
                                contextMenus.Add(new ContextMenuResult
                                {
                                    PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                    Title = Resources.run_as_user,
                                    Glyph = "\xE7EE",
                                    FontFamily = font,
                                    AcceleratorKey = Key.U,
                                    AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                                    Action = (context) =>
                                    {
                                        try
                                        {
                                            Task.Run(() => Helper.RunAsUser(record.Path));
                                            _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                            return true;
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Exception($"EPT3: Failed to run {record.Path} as different user", e, GetType());
                                            return false;
                                        }
                                    },
                                });
                            }

                            break;
                        case '3':
                            // Copy File/Folder
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.copy_file + (_swapCopy ? Resources.copy_shortcut : Resources.copy_shortcutAlt),
                                Glyph = "\xE8C8",
                                FontFamily = font,
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = _swapCopy ? ModifierKeys.Control : ModifierKeys.Control | ModifierKeys.Alt,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        Clipboard.SetData(DataFormats.FileDrop, new string[] { record.Path });
                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        var message = Resources.clipboard_failed;
                                        Log.Exception($"ETP3: Failed to copy {(_swapCopy ? "file" : "path")} ({record.Path}) to clipboard", e, GetType());

                                        _context.API.ShowMsg(message);
                                        return false;
                                    }
                                },
                            });
                            break;
                        case '4':
                            // Copy Path
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.copy_path + (_swapCopy ? Resources.copy_shortcutAlt : Resources.copy_shortcut),
                                Glyph = "\xE71B",
                                FontFamily = font,
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = _swapCopy ? ModifierKeys.Control | ModifierKeys.Alt : ModifierKeys.Control,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        Clipboard.SetDataObject(record.Path);
                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        var message = Resources.clipboard_failed;
                                        Log.Exception($"ETP3: Failed to copy {(_swapCopy ? "path" : "file")} ({record.Path}) to clipboard", e, GetType());

                                        _context.API.ShowMsg(message);
                                        return false;
                                    }
                                },
                            });
                            break;
                        case '5':
                            // Open in Shell
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.open_in_console,
                                Glyph = "\xE756",
                                FontFamily = font,
                                AcceleratorKey = Key.C,
                                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,

                                Action = (context) =>
                                {
                                    try
                                    {
                                        if (isFile)
                                            Helper.OpenInConsole(Path.GetDirectoryName(record.Path));
                                        else
                                            Helper.OpenInConsole(record.Path);

                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Exception($"ETP3: Failed to open {record.Path} in console", e, GetType());
                                        return false;
                                    }
                                },
                            });
                            break;
                        case '6':
                            // Pass to custom program as parameter
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.open_in_custom,
                                Glyph = "\xE8A7",
                                FontFamily = font,
                                AcceleratorKey = Key.N,
                                AcceleratorModifiers = ModifierKeys.Control,

                                Action = (context) =>
                                {
                                    using var process = new Process();
                                    process.StartInfo.FileName = _customProgram;
                                    process.StartInfo.Arguments = $"\"{_customArg.Replace("$P", record.Path)}\"";
                                    try
                                    {
                                        process.Start();
                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Exception($"ETP3: Failed to execute {_customProgram} with arguments {_customArg}", e, GetType());
                                        return false;
                                    }
                                },
                            });
                            break;
                        case '7':
                            // Delete
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.delete_result,
                                Glyph = "\xE74D",
                                FontFamily = font,
                                AcceleratorKey = Key.Delete,
                                AcceleratorModifiers = ModifierKeys.Control,
                                Action = (context) =>
                                {
                                    try
                                    {
                                        if (isFile)
                                            File.Delete(record.Path);
                                        else
                                            Directory.Delete(record.Path, true);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Exception($"ETP3: Failed to delete {record.Path}", e, GetType());
                                        return false;
                                    }
                                },
                            });
                            break;
                        case '8':
                            // Right Click Context Menu
                            contextMenus.Add(new ContextMenuResult
                            {
                                PluginName = Assembly.GetExecutingAssembly().GetName().Name,
                                Title = Resources.scm,
                                Glyph = "\xE712",
                                FontFamily = font,
                                AcceleratorKey = Key.M,
                                AcceleratorModifiers = ModifierKeys.Control,
                                Action = (context) =>
                                {
                                    try
                                    {
                                        ShellContextMenu scm = new();
                                        if (isFile)
                                            scm.ShowContextMenu(new FileInfo(record.Path), wf.Cursor.Position);
                                        else
                                            scm.ShowContextMenu(new DirectoryInfo(record.Path), wf.Cursor.Position);
                                        _ = NativeMethods.Everything3_IncRunCountFromFilenameW(Client, record.Path);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Exception($"ETP3: Failed to open right click context menu for {record.Path}", e, GetType());
                                        return false;
                                    }
                                },
                            });
                            break;
                        default:
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
                    return true;
            }

            return false;
        }
    }
}
