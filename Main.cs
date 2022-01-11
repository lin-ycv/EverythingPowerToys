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
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu
    {
        private readonly string reservedStringPattern = @"^[\/\\\$\%]+$|^.*[<>].*$";

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

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
                        var found = EverythingSearch(searchQuery);
                        if (found.ElementAt(0).Title == "!")
                        {
                            results.Add(new Result()
                            {
                                Title = Properties.Resources.Everything_not_running,
                                SubTitle = Properties.Resources.Everything_ini,
                                IcoPath = _warningIconPath,
                                QueryTextDisplay = Properties.Resources.Everything_url,
                            });
                        }
                        else
                        {
                            results.AddRange(found);
                        }
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
