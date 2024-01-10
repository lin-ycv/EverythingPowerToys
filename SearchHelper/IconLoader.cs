using System;
using System.IO;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything.SearchHelper
{
    internal sealed class IconLoader
    {
        internal static readonly char[] Separator = ['\"', ','];
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        internal static string? Icon(string doctype)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            uint pcchOut = 0;
            _ = AssocQueryString(AssocF.NONE, AssocStr.DEFAULTICON, doctype, null, null, ref pcchOut);
            char[] pszOut = new char[pcchOut];
            if (AssocQueryString(AssocF.NONE, AssocStr.DEFAULTICON, doctype, null, pszOut, ref pcchOut) != 0) return null;
            string doc = Environment.ExpandEnvironmentVariables(new string(pszOut).Split(Separator, StringSplitOptions.RemoveEmptyEntries)[0].Replace("\"", string.Empty, StringComparison.CurrentCulture).Trim());

            return File.Exists(doc) ? doc : null;
        }
    }
}
