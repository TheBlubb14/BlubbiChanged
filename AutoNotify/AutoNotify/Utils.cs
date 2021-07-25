using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly:InternalsVisibleTo("AutoNotify.Test")]
namespace AutoNotify
{
    internal static class Utils
    {
        private static readonly Regex removeMultipleNewLinesRegex = new(@"(\r\n){2,}");

        internal static string FormatCode(string code, Workspace workspace)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            // Format C#
            var formatted = Formatter.Format(root, workspace).ToFullString();

            // Remove multiple empty lines
            formatted = removeMultipleNewLinesRegex.Replace(formatted, Environment.NewLine + Environment.NewLine);

            // Remove leading and trailing newlines
            return formatted.Trim('\r', 'n');
        }
    }
}
