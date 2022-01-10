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
using Wox.Plugin;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin
    {
        private readonly string reservedStringPattern = @"^[\/\\\$\%]+$|^.*[<>].*$";

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        private PluginInitContext _context;
        private bool disposedValue;

        private string _warningIconPath;
        private ErrorCode errorCode;

        private string IconPath { get; set; }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            uint major = Everything_GetMajorVersion();
            uint minor = Everything_GetMinorVersion();
            uint rev = Everything_GetRevision();
            if (major == 0 && minor == 0 && rev == 0 && (ErrorCode)Everything_GetLastError() != ErrorCode.EVERYTHING_OK)
            {
                errorCode = (ErrorCode)Everything_GetLastError();
                results.Add(new Result
                {
                    Title = Properties.Resources.Everything_not_running,
                    IcoPath = _warningIconPath,
                });
            }
            else
            {
                errorCode = ErrorCode.EVERYTHING_OK;
            }

            return results;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to keep the process alive but will log the exception")]
        public List<Result> Query(Query query, bool isFullQuery)
        {
            List<Result> results = new List<Result>();
            if (query != null && !string.IsNullOrEmpty(query.Search))
            {
                var searchQuery = query.RawQuery;

                var regexMatch = Regex.Match(searchQuery, reservedStringPattern);

                if (errorCode == ErrorCode.EVERYTHING_OK && !regexMatch.Success)
                {
                    try
                    {
                        results.AddRange(EverythingSearch(searchQuery));
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
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
