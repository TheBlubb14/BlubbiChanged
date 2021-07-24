using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoNotify
{
    public class CLassGenerator
    {
        private readonly INamedTypeSymbol NotifyPropertyChangedSymbol;
        private readonly INamedTypeSymbol NotifyPropertyChangingSymbol;
        private readonly INamedTypeSymbol AttributeSymbol;
        private readonly Regex removeMultipleNewLinesRegex = new(@"(\r\n){2,}");
        private readonly AdhocWorkspace workspace = new();

        public CLassGenerator(INamedTypeSymbol attributeSymbol, INamedTypeSymbol notifyChangedSymbol, INamedTypeSymbol notifyChangingSymbol)
        {
            AttributeSymbol = attributeSymbol;
            NotifyPropertyChangedSymbol = notifyChangedSymbol;
            NotifyPropertyChangingSymbol = notifyChangingSymbol;
        }

        private string FormatCode(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            // Format C#
            var formatted = Formatter.Format(root, workspace).ToFullString();

            // Remove multiple empty lines
            formatted = removeMultipleNewLinesRegex.Replace(formatted, Environment.NewLine + Environment.NewLine);

            // Remove leading and trailing newlines
            return formatted.Trim(Environment.NewLine.ToCharArray());
        }

        public string Construct(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            var nameSpace = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var INotifyPropertyChanging = NotifyPropertyChangingSymbol.ToDisplayString();
            var INotifyPropertyChanged = NotifyPropertyChangedSymbol.ToDisplayString();

            // TODO: move SetpropertyCode into each setter of each property
            return
                FormatCode($@"
namespace {nameSpace}
{{
    public partial class {className} : {INotifyPropertyChanging}, {INotifyPropertyChanged}
    {{
        {PropertyChangingEventHandler(classSymbol)}
        {PropertyChangedEventHandler(classSymbol)}

        {GenerateProperties(fields)}

        protected T SetProperty<T>(ref T field, T value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {{
            if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return value;

            this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(propertyName));

            field = value;

            this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));

            return value;
        }}
    }}
}}
");
        }

        private string PropertyChangedEventHandler(INamedTypeSymbol classSymbol)
        {
            return classSymbol.Interfaces.Contains(NotifyPropertyChangedSymbol)
                ? ""
                : "/// <inheritdoc/>" + Environment.NewLine +
                    "public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;" + Environment.NewLine;
        }

        private string PropertyChangingEventHandler(INamedTypeSymbol classSymbol)
        {
            return classSymbol.Interfaces.Contains(NotifyPropertyChangingSymbol)
                ? ""
                : "/// <inheritdoc/>" + Environment.NewLine +
                    "public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;" + Environment.NewLine;
        }

        private string GenerateProperties(List<IFieldSymbol> fields)
        {
            var builder = new StringBuilder();

            foreach (var field in fields)
            {
                var s = GenerateProperty(field);
                if (!string.IsNullOrEmpty(s))
                    builder.AppendLine(s);
            }

            return builder.ToString();
        }

        private string GenerateProperty(IFieldSymbol field)
        {
            // get the AutoNotify attribute from the field, and any associated data
            var attributeData = field.GetAttributes()
                .Single(ad => ad.AttributeClass.Equals(AttributeSymbol, SymbolEqualityComparer.Default));
            var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            var propertyName = ChooseName(field.Name, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == field.Name)
            {
                //TODO: issue a diagnostic that we can't process this field
                return null;
            }

            return $@"
{GetSummary(field)}
public {field.Type} {propertyName} 
{{
    get => this.{field.Name};
    set => SetProperty(ref this.{field.Name}, value);
}}
";
        }

        private string GetSummary(IFieldSymbol field)
        {
            var summary = "";
            var xml = field.GetDocumentationCommentXml();
            if (!string.IsNullOrWhiteSpace(xml))
            {
                var lines = xml.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                for (var i = 1; i < lines.Length - 2; i++)
                {
                    summary += Environment.NewLine + "/// " + lines[i].Trim();
                }
            }

            return summary;
        }

        private string ChooseName(string fieldName, TypedConstant overridenNameOpt)
        {
            if (!overridenNameOpt.IsNull)
            {
                return overridenNameOpt.Value.ToString();
            }

            fieldName = fieldName.TrimStart('_');
            if (fieldName.Length == 0)
                return string.Empty;

            if (fieldName.Length == 1)
                return fieldName.ToUpper();

            return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
        }
    }
}
