using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Community.PowerToys.Run.Plugin.Everything3.ContextMenu;
using Community.PowerToys.Run.Plugin.Everything3.Properties;
using Microsoft.PowerToys.Settings.UI.Library;
using NLog;
using PowerLauncher.Plugin;
using Wox.Infrastructure.Storage;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything3.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything3
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IPluginI18n, ISavable
    {
        public static string PluginID => "9240993D2A80417BAA1963D106C79E77";
        public string Name => Resources.plugin_name;
        public string Description => Resources.plugin_description;
        private readonly Settings _setting = new();
        private readonly PluginJsonStorage<Update.UpdateSettings> _storage = new();
        private readonly bool _isArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        private Everything _everything;
        private ContextMenuLoader _contextMenuLoader;
        private CancellationTokenSource cts = new();
        private bool _disposed;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
        [
            new()
            {
                Key = nameof(Settings.InstanceName),
                DisplayLabel = Resources.InstanceName,
                DisplayDescription = Resources.InstanceName_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.InstanceName,
            },
            new()
            {
                Key = nameof(Settings.Context),
                DisplayLabel = Resources.Context,
                DisplayDescription = Resources.Context_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.Context,
            },
            new()
            {
                Key = nameof(Settings.Sort),
                DisplayLabel = Resources.Sort,
                DisplayDescription = Resources.Sort_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = Enum.GetValues<Sort>().Cast<int>().Select(v => new KeyValuePair<string, string>(((Sort)v).ToString(), v + string.Empty)).ToList(),
                ComboBoxValue = (int)_setting.Sort,
            },
            new()
            {
                Key = nameof(Settings.Descending),
                DisplayLabel = Resources.SortDescending,
                DisplayDescription = Resources.SortDescending_Description,
                Value = _setting.Descending,
            },
            new()
            {
                Key = nameof(Settings.Sort2),
                DisplayLabel = Resources.SortThen,
                DisplayDescription = Resources.Sort_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = Enum.GetValues<Sort>().Cast<int>().Select(v => new KeyValuePair<string, string>(((Sort)v).ToString(), v + string.Empty)).ToList(),
                ComboBoxValue = (int)_setting.Sort2,
            },
            new()
            {
                Key = nameof(Settings.Descending2),
                DisplayLabel = Resources.SortDescending,
                DisplayDescription = Resources.SortDescending_Description,
                Value = _setting.Descending2,
            },
            new()
            {
                Key = nameof(Settings.Max),
                DisplayLabel = Resources.Max,
                DisplayDescription = Resources.Max_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _setting.Max,
            },
            new()
            {
                Key = nameof(Settings.Prefix),
                DisplayLabel = Resources.Prefix,
                DisplayDescription = Resources.Prefix_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.Prefix,
            },
            new()
            {
                Key = nameof(Settings.EverythingPath),
                DisplayLabel = Resources.EverythingPath,
                DisplayDescription = Resources.EverythingPath_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.EverythingPath,
            },
            new()
            {
                Key = nameof(Settings.CustomProgram),
                DisplayLabel = Resources.CustomProgram,
                DisplayDescription = Resources.CustomProgram_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.CustomProgram,
            },
            new()
            {
                Key = nameof(Settings.CustomArg),
                DisplayLabel = Resources.CustomArg,
                DisplayDescription = Resources.CustomArg_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.CustomArg,
            },
            new()
            {
                Key = nameof(Settings.Copy),
                DisplayLabel = Resources.SwapCopy,
                DisplayDescription = Resources.SwapCopy_Description,
                Value = _setting.Copy,
            },
            new()
            {
                Key = nameof(Settings.EnvVar),
                DisplayLabel = Resources.EnvVar,
                DisplayDescription = Resources.EnvVar_Description,
                Value = _setting.EnvVar,
            },
            new()
            {
                Key = nameof(Settings.MatchPath),
                DisplayLabel = Resources.Match_path,
                DisplayDescription = Resources.Match_path_Description,
                Value = _setting.MatchPath,
            },
            new()
            {
                Key = nameof(Settings.Preview),
                DisplayLabel = Resources.Preview,
                DisplayDescription = Resources.Preview_Description,
                Value = _setting.Preview,
            },
            new()
            {
                Key = nameof(Settings.QueryText),
                DisplayLabel = Resources.QueryText,
                DisplayDescription = Resources.QueryText_Description,
                Value = _setting.QueryText,
            },
            new()
            {
                Key = nameof(Settings.RegEx),
                DisplayLabel = Resources.RegEx,
                DisplayDescription = Resources.RegEx_Description,
                Value = _setting.RegEx,
            },
            new()
            {
                Key = nameof(Settings.ShowMore),
                DisplayLabel = Resources.ShowMore,
                DisplayDescription = Resources.ShowMore_Description,
                Value = _setting.ShowMore,
            },
            new()
            {
                Key = nameof(Settings.Updates),
                DisplayLabel = Resources.Updates,
                DisplayDescription = $"v{Assembly.GetExecutingAssembly().GetName().Version}",
                Value = _setting.Updates,
            },
            new()
            {
                Key = nameof(Settings.LoggingLevel),
                DisplayLabel = "Log Level",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = LogLevel.AllLoggingLevels.Select(d => new KeyValuePair<string, string>(d.ToString(), d.Ordinal + string.Empty)).ToList(),
                ComboBoxValue = _setting.LoggingLevel.Ordinal,
            },
        ];

        public void Init(PluginInitContext context)
        {
            if (_isArm)
            {
                MessageBox.Show("Everything3 SDK currently does not have an ARM version, please switch to stable release of EPT", "EPT3: ARM Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new PlatformNotSupportedException("EPT3: ARM is not supported yet with Everything3 SDK, use stable release of EPT instead");
            }

            //string dll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Everything3_x64.dll");

            //if (!File.Exists(dll))
            //{
            //    MessageBoxResult mbox = MessageBox.Show(Resources.MissingLib, "EPT3: Downloader", MessageBoxButton.YesNo);
            //    if (mbox == MessageBoxResult.Yes)
            //    {
            //        using HttpClient httpClient = new();
            //        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            //        string url = $"https://github.com/lin-ycv/EverythingPowerToys/raw/refs/heads/lib/Everything3_x64.dll";
            //        byte[] fileContent = httpClient.GetByteArrayAsync(url).Result;
            //        File.WriteAllBytes(dll, fileContent);
            //    }
            //    else
            //    {
            //        throw new DllNotFoundException("EPT3: Everything3_x64.dll not found, either press Yes on the download prompt, or manually load in the dll @ %LOCALAPPDATA%\\Microsoft\\PowerToys\\PowerToys Run\\Plugins\\Everything3");
            //    }
            //}

            if (_setting.LoggingLevel <= LogLevel.Debug)
                Log.Info("EPT3: Init", GetType());

            Update.UpdateSettings upSettings = _storage.Load();
            if (_setting.Updates)
                Task.Run(() => new Update.UpdateChecker().Async(Assembly.GetExecutingAssembly().GetName().Version, _setting, upSettings));

            // _everything = new Everything(_setting); // Fail to initialize, starts up before Everything, #165
            _contextMenuLoader = new ContextMenuLoader(context, _setting.Context);
            _contextMenuLoader.Update(_setting);
            var history = PluginManager.GlobalPlugins.FirstOrDefault(p => p.Metadata.ID == "C88512156BB74580AADF7252E130BA8D" && !p.Metadata.Disabled);
            if (history != null)
                Task.Run(() => MessageBox.Show(Resources.History, "EPT3: History Conflict", MessageBoxButton.OK, MessageBoxImage.Warning));
            if (_setting.LoggingLevel <= LogLevel.Debug)
                Log.Info("EPT3: Init Complete", GetType());
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings.AdditionalOptions != null)
            {
                _setting.InstanceName = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.InstanceName)).TextValue;
                _setting.Sort = (Sort)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Sort)).ComboBoxValue;
                _setting.Descending = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Descending)).Value;
                _setting.Sort2 = (Sort)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Sort2)).ComboBoxValue;
                _setting.Descending2 = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Descending2)).Value;
                _setting.Max = (uint)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Max)).NumberValue;
                _setting.Context = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Context)).TextValue;
                _setting.RegEx = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.RegEx)).Value;
                _setting.Preview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Preview)).Value;
                _setting.MatchPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.MatchPath)).Value;
                _setting.Copy = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Copy)).Value;
                _setting.QueryText = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.QueryText)).Value;
                _setting.EnvVar = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.EnvVar)).Value;
                _setting.Updates = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Updates)).Value;
                _setting.Prefix = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Prefix)).TextValue;
                _setting.EverythingPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.EverythingPath)).TextValue;
                _setting.CustomProgram = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.CustomProgram)).TextValue;
                _setting.CustomArg = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.CustomArg)).TextValue;
                _setting.ShowMore = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.ShowMore)).Value;
                _setting.LoggingLevel = LogLevel.FromOrdinal(settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.LoggingLevel)).ComboBoxValue);

                _everything?.UpdateSettings(_setting);
                _contextMenuLoader?.Update(_setting);
            }
        }

        public List<Result> Query(Query query)
        {
            if (_everything == null)
            {
                try
                {
                    _everything = new Everything(_setting);
                    _contextMenuLoader.Client = _everything.Client;
                }
                catch (InvalidOperationException)
                {
                    //Log.Warn("EPT3: Unable to create Everything instance", GetType());
                    return
                    [
                        new Result()
                        {
                            Title = Resources.Everything_not_running,
                            SubTitle = Resources.Everything_ini,
                            IcoPath = "Images/warning.png",
                            Score = int.MaxValue,
                        },
                    ];
                }
            }

            return null;
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            if (_everything == null)
                return null;

            List<Result> results = [];
            if (!string.IsNullOrEmpty(query.Search))
            {
                string searchQuery = query.Search;

                try
                {
                    cts.Cancel();
                    cts = new();
                    results.AddRange(_everything.Query(searchQuery, _setting, cts.Token));
                }
                catch (OperationCanceledException)
                {
                    if (_setting.LoggingLevel <= LogLevel.Debug)
                        Log.Info("EPT3: Query Cancelled", GetType());
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    results.Add(new Result()
                    {
                        Title = Resources.Everything_not_running,
                        SubTitle = Resources.Everything_ini,
                        IcoPath = "Images/warning.png",
                        Score = int.MaxValue,
                    });
                }
                catch (Exception e)
                {
                    Log.Exception($"EPT3: Exception! {e.Message}\n", e, GetType());
                }
            }

            return results;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    cts.Cancel();
                    cts.Dispose();
                    try
                    {
                        Everything3_DestroySearchState(_everything.SearchState);
                        Everything3_DestroyClient(_everything.Client);
                    }
                    catch (Exception e)
                    {
                        Log.Exception("EPT3: Dispose", e, GetType());
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult) => _contextMenuLoader.LoadContextMenus(selectedResult);
        public Control CreateSettingPanel() => throw new NotImplementedException();
        public string GetTranslatedPluginTitle() => Resources.plugin_name;
        public string GetTranslatedPluginDescription() => Resources.plugin_description;

        public void Save() => _storage.Save();
    }
}
