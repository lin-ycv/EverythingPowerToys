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
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IPluginI18n
    {
        private const string AltIcon = nameof(AltIcon);
        private const string RegEx = nameof(RegEx);
        private const string NoPreview = nameof(NoPreview);
        private readonly string reservedStringPattern = @"^[\/\\\$\%]+$|^.*[<>].*$";
        private bool regEx;
        private bool preview;
        private bool altIcon;

#if DEBUG
        private const string Debug = nameof(Debug);
        private bool debug;
#endif

        public string Name => Resources.plugin_name;

        public string Description => Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = AltIcon,
                DisplayLabel = Resources.AltIcon,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = NoPreview,
                DisplayLabel = Resources.Preview,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = RegEx,
                DisplayLabel = Resources.RegEx,
                Value = false,
            },
#if DEBUG
            new PluginAdditionalOption()
            {
                Key = Debug,
                DisplayLabel = "Output debug data",
                Value = true,
            },
#endif
        };

        private IContextMenu contextMenuLoader;
        private PluginInitContext context;
        private bool disposed;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            this.contextMenuLoader = new ContextMenuLoader(context);
            EverythingSetup();
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

                var regexMatch = Regex.Match(searchQuery, this.reservedStringPattern);

                if (!regexMatch.Success)
                {
                    try
                    {
                        results.AddRange(EverythingSearch(searchQuery, this.preview, this.altIcon));
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
                        Log.Exception("Everything Exception", e, this.GetType());
                    }
                }
            }

            return results;
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
            var regX = false;
            var nopreview = false;
            var alt = false;
#if DEBUG
            var debuging = true;
#endif
            if (settings != null && settings.AdditionalOptions != null)
            {
                regX = settings.AdditionalOptions.FirstOrDefault(x => x.Key == RegEx)?.Value ?? false;
                nopreview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == NoPreview)?.Value ?? false;
                alt = settings.AdditionalOptions.FirstOrDefault(x => x.Key == AltIcon)?.Value ?? false;
#if DEBUG
                debuging = settings.AdditionalOptions.FirstOrDefault(x => x.Key == Debug)?.Value ?? true;
#endif
            }

            this.regEx = regX;
            Everything_SetRegex(this.regEx);
            this.preview = nopreview;
            this.altIcon = alt;
#if DEBUG
            this.debug = debuging;
#endif
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
