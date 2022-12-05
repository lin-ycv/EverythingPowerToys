// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IPluginI18n
    {
        private const string RegEx = nameof(RegEx);
        private const string NoPreview = nameof(NoPreview);
        private const string MatchPath = nameof(MatchPath);
        private const string SwapCopy = nameof(SwapCopy);
        private const string QueryTextDisplay = nameof(QueryTextDisplay);
        private bool _regEx;
        private bool _preview;
        private bool _matchPath;
        private bool _swapCopy;
        private bool _queryTextDisplay;
        private IContextMenu _contextMenuLoader;
        private PluginInitContext _context;
        private bool _disposed;

        private const string Debug = nameof(Debug);
        private bool _debug;

        public string Name => Resources.plugin_name;

        public string Description => Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = MatchPath,
                DisplayLabel = Resources.Match_path,
                DisplayDescription = Resources.Match_path_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = NoPreview,
                DisplayLabel = Resources.Preview,
                DisplayDescription = Resources.Preview_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = RegEx,
                DisplayLabel = Resources.RegEx,
                DisplayDescription = Resources.RegEx_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = SwapCopy,
                DisplayLabel = Resources.SwapCopy,
                DisplayDescription = Resources.SwapCopy_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = QueryTextDisplay,
                DisplayLabel = Resources.QueryText,
                DisplayDescription = Resources.QueryText_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = Debug,
                DisplayLabel = "Log debug data",
                DisplayDescription = $"v{Assembly.GetExecutingAssembly().GetName().Version}",
                Value = false,
            },
        };

        public void Init(PluginInitContext context)
        {
            _context = context;
            _contextMenuLoader = new ContextMenuLoader(context);
            ((ContextMenuLoader)_contextMenuLoader).UpdateCopy(_swapCopy);
            EverythingSetup(_debug);
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            return results;
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            List<Result> results = new List<Result>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var searchQuery = query.Search;

                try
                {
                    results.AddRange(EverythingSearch(searchQuery, _preview, _matchPath, _queryTextDisplay, _debug));
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    results.Add(new Result()
                    {
                        Title = Resources.Everything_not_running,
                        SubTitle = Resources.Everything_ini,
                        IcoPath = "Images/warning.png",
                        QueryTextDisplay = '.' + Resources.plugin_name,
                        Score = int.MaxValue,
                    });
                }
                catch (Exception e)
                {
                    Log.Exception("Everything Exception", e, GetType());
                }
            }

            return results;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return _contextMenuLoader.LoadContextMenus(selectedResult);
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings != null && settings.AdditionalOptions != null)
            {
                _regEx = settings.AdditionalOptions.FirstOrDefault(x => x.Key == RegEx)?.Value ?? false;
                _preview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == NoPreview)?.Value ?? false;
                _matchPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == MatchPath)?.Value ?? false;
                _swapCopy = settings.AdditionalOptions.FirstOrDefault(x => x.Key == SwapCopy)?.Value ?? false;
                _queryTextDisplay = settings.AdditionalOptions.FirstOrDefault(x => x.Key == QueryTextDisplay)?.Value ?? false;
                _debug = settings.AdditionalOptions.FirstOrDefault(x => x.Key == Debug)?.Value ?? true;

                if (_contextMenuLoader != null) ((ContextMenuLoader)_contextMenuLoader).UpdateCopy(_swapCopy);

                Everything_SetRegex(_regEx);
                Everything_SetMatchPath(_matchPath);
            }
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

        public string GetTranslatedPluginTitle()
        {
            return Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Resources.plugin_description;
        }
    }
}
