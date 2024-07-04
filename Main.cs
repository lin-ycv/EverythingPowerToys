using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IPluginI18n
    {
        public static string PluginID => "A86867E2D932459CBD77D176373DD657";
        public string Name => Resources.plugin_name;
        public string Description => Resources.plugin_description;
        private readonly Settings _setting = new();
        private Everything _everything;
        private ContextMenuLoader _contextMenuLoader;
        private bool _disposed;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
        [
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
                ComboBoxItems = Enum.GetValues(typeof(Sort)).Cast<int>().Select(v => new KeyValuePair<string, string>(((Sort)v).ToString(), v + string.Empty)).ToList(),
                ComboBoxValue = (int)_setting.Sort,
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
                Key = nameof(Settings.Copy),
                DisplayLabel = Resources.SwapCopy,
                DisplayDescription = Resources.SwapCopy_Description,
                Value = _setting.Copy,
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
                Key = nameof(Settings.EnvVar),
                DisplayLabel = Resources.EnvVar,
                DisplayDescription = Resources.EnvVar_Description,
                Value = _setting.EnvVar,
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
                Key = nameof(Settings.Log),
                DisplayLabel = "Debug Mode",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = Enum.GetValues(typeof(LogLevel)).Cast<int>().Select(d => new KeyValuePair<string, string>(((LogLevel)d).ToString(), d + string.Empty)).ToList(),
                ComboBoxValue = (int)_setting.Log,
            },
        ];

        public void Init(PluginInitContext context)
        {
            if (_setting.Updates)
                Task.Run(() => new Update().UpdateAsync(Assembly.GetExecutingAssembly().GetName().Version, _setting));
            _setting.Getfilters();
            _everything = new Everything(_setting);
            _contextMenuLoader = new ContextMenuLoader(context, _setting.Context);
            _contextMenuLoader.Update(_setting);
            if (_setting.Log > LogLevel.None)
                Debugger.Write("Init Complete\r\n");
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings.AdditionalOptions != null)
            {
                _setting.Sort = (Sort)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Sort)).ComboBoxValue;
                _setting.Max = (uint)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Max)).NumberValue;
                _setting.Context = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Context)).TextValue;
                _setting.RegEx = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.RegEx)).Value;
                _setting.Preview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Preview)).Value;
                _setting.MatchPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.MatchPath)).Value;
                _setting.Copy = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Copy)).Value;
                _setting.QueryText = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.QueryText)).Value;
                _setting.EnvVar = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.EnvVar)).Value;
                _setting.Updates = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Updates)).Value;
                _setting.Log = (LogLevel)settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Log)).ComboBoxValue;
                _setting.Prefix = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Prefix)).TextValue;
                _setting.EverythingPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.EverythingPath)).TextValue;

                _everything?.UpdateSettings(_setting);
                _contextMenuLoader?.Update(_setting);
            }
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = [];
            return results;
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            List<Result> results = [];
            if (!string.IsNullOrEmpty(query.Search))
            {
                string searchQuery = query.Search;

                try
                {
                    results.AddRange(_everything.Query(searchQuery, _setting));
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
                    if (_setting.Log > LogLevel.None)
                        Debugger.Write($"Everything Exception: {e.Message}\r\n{e.StackTrace}\r\n");

                    Log.Exception("Everything Exception", e, GetType());
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
    }
}
