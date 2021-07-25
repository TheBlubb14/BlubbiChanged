using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoNotify
{
    public class ClassGenerator : IDisposable
    {
        private readonly INamedTypeSymbol NotifyPropertyChangedSymbol;
        private readonly INamedTypeSymbol NotifyPropertyChangingSymbol;
        private readonly INamedTypeSymbol AttributeSymbol;
        private readonly AdhocWorkspace workspace = new();

        public ClassGenerator(INamedTypeSymbol attributeSymbol, INamedTypeSymbol notifyChangingSymbol, INamedTypeSymbol notifyChangedSymbol)
        {
            AttributeSymbol = attributeSymbol;
            NotifyPropertyChangingSymbol = notifyChangingSymbol;
            NotifyPropertyChangedSymbol = notifyChangedSymbol;
        }

        public string Construct(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            var nameSpace = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var INotifyPropertyChanging = NotifyPropertyChangingSymbol.ToDisplayString();
            var INotifyPropertyChanged = NotifyPropertyChangedSymbol.ToDisplayString();

            return
                Utils.FormatCode($@"
namespace {nameSpace}
{{
    public partial class {className} : {INotifyPropertyChanging}, {INotifyPropertyChanged}
    {{
        {PropertyChangingEventHandler(classSymbol)}
        {PropertyChangedEventHandler(classSymbol)}

        {GenerateProperties(fields)}
    }}
}}
", workspace);
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

            if (field.IsReadOnly)
            {
                // Not supported
                // TODO: issue diagnostic
                return null;
            }

            var fieldName = field.Name;
            if (!field.IsStatic)
                fieldName = "this." + fieldName;

            return $@"
{GetSummary(field)}
public {field.Type} {propertyName} 
{{
    get => {fieldName};
    set
    {{
        if (global::System.Collections.Generic.EqualityComparer<{field.Type}>.Default.Equals({fieldName}, value))
            return;

        this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(""{propertyName}""));

        {fieldName} = value;

        this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(""{propertyName}""));
    }}
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

        public void Dispose()
        {
            workspace.Dispose();
        }
    }
}
