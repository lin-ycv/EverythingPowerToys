// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
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
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider
    {
        private const string Wait = nameof(Wait);
        private readonly string reservedStringPattern = @"^[\/\\\$\%]+$|^.*[<>].*$";
        private bool _wait;

        public string Name => Resources.plugin_name;

        public string Description => Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = Wait,
                DisplayLabel = Resources.Wait,
                Value = false,
            },
        };

        private string IconPath { get; set; }

        private IContextMenu _contextMenuLoader;
        private PluginInitContext _context;
        private bool disposed;
        private string _warningIconPath;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _contextMenuLoader = new ContextMenuLoader(context);
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            return results;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to keep the process alive but will log the exception")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Already validated")]
        public List<Result> Query(Query query, bool isFullQuery)
        {
            List<Result> results = new List<Result>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var searchQuery = query.Search;

                var regexMatch = Regex.Match(searchQuery, reservedStringPattern);

                if (!regexMatch.Success)
                {
                    try
                    {
                        results.AddRange(EverythingSearch(searchQuery, _wait));
                    }
                    catch (OperationCanceledException)
                    {
                        results.Add(new Result()
                        {
                            Title = Resources.timeout,
                            SubTitle = Resources.enable_wait,
                            IcoPath = _warningIconPath,
                        });
                    }
                    catch (EntryPointNotFoundException)
                    {
                        results.Add(new Result()
                        {
                            Title = Resources.Everything_not_running,
                            SubTitle = Resources.Everything_ini,
                            IcoPath = _warningIconPath,
                            QueryTextDisplay = Resources.Everything_url,
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Exception("Something failed", e, GetType());
                    }
                }
            }

            return results;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                IconPath = "Images/Everything.light.png";
                _warningIconPath = "Images/Warning.light.png";
            }
            else
            {
                IconPath = "Images/Everything.dark.png";
                _warningIconPath = "Images/Warning.dark.png";
            }
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
            var wait = false;
            if (settings != null && settings.AdditionalOptions != null)
            {
                wait = settings.AdditionalOptions.FirstOrDefault(x => x.Key == Wait)?.Value ?? false;
            }

            _wait = wait;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
