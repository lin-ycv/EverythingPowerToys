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
        private const string Top = nameof(Top);
        private const string NoPreview = nameof(NoPreview);
        private readonly string reservedStringPattern = @"^[\/\\\$\%]+$|^.*[<>].*$";
        private bool top;
        private bool preview;

        public string Name => Resources.plugin_name;

        public string Description => Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = Top,
                DisplayLabel = Resources.Top,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = NoPreview,
                DisplayLabel = Resources.Preview,
                Value = false,
            },
        };

        private IContextMenu contextMenuLoader;
        private PluginInitContext context;
        private bool disposed;
        private static string warningIconPath;

        internal static string WarningIcon
        {
            get
            {
                return warningIconPath;
            }
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
            this.contextMenuLoader = new ContextMenuLoader(context);
            this.context.API.ThemeChanged += this.OnThemeChanged;
            UpdateIconPath(this.context.API.GetCurrentTheme());
            EverythingSetup();
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            return results;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to keep the process alive but will log the exception")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Already validated")]
        public List<Result> Query(Query query, bool delayedExecution)
        {
            List<Result> results = new List<Result>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var searchQuery = query.Search;

                var regexMatch = Regex.Match(searchQuery, this.reservedStringPattern);

                if (!regexMatch.Success)
                {
                    try
                    {
                        results.AddRange(EverythingSearch(searchQuery, this.top, this.preview));
                    }
                    catch (OperationCanceledException)
                    {
                        results.Add(new Result()
                        {
                            Title = Resources.timeout,
                            SubTitle = Resources.enable_wait,
                            IcoPath = warningIconPath,
                            Score = int.MaxValue,
                        });
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        results.Add(new Result()
                        {
                            Title = Resources.Everything_not_running,
                            SubTitle = Resources.Everything_ini,
                            IcoPath = warningIconPath,
                            QueryTextDisplay = '.' + Resources.plugin_name,
                            Score = int.MaxValue,
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Exception("Everything Exception", e, this.GetType());
                    }
                }
            }

            return results;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private static void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                warningIconPath = "Images/Warning.light.png";
            }
            else
            {
                warningIconPath = "Images/Warning.dark.png";
            }
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return this.contextMenuLoader.LoadContextMenus(selectedResult);
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            var top = false;
            var nopreview = false;
            if (settings != null && settings.AdditionalOptions != null)
            {
                top = settings.AdditionalOptions.FirstOrDefault(x => x.Key == Top)?.Value ?? false;
                nopreview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == NoPreview)?.Value ?? false;
            }

            this.top = top;
            this.preview = nopreview;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
