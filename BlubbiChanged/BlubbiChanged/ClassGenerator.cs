using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlubbiChanged
{
    public class ClassGenerator : IDisposable
    {
        private readonly INamedTypeSymbol NotifyPropertyChangedSymbol;
        private readonly INamedTypeSymbol NotifyPropertyChangedHandlerSymbol;
        private readonly INamedTypeSymbol NotifyPropertyChangingSymbol;
        private readonly INamedTypeSymbol NotifyPropertyChangingHandlerSymbol;
        private readonly INamedTypeSymbol AttributeSymbol;
        private readonly GeneratorExecutionContext Context;
        //private readonly AdhocWorkspace workspace = new();

        public ClassGenerator(GeneratorExecutionContext context, INamedTypeSymbol attributeSymbol, INamedTypeSymbol notifyChangingSymbol, INamedTypeSymbol notifyChangingHandlerSymbol, INamedTypeSymbol notifyChangedSymbol, INamedTypeSymbol notifyChangedHandlerSymbol)
        {
            Context = context;
            AttributeSymbol = attributeSymbol;
            NotifyPropertyChangingSymbol = notifyChangingSymbol;
            NotifyPropertyChangingHandlerSymbol = notifyChangingHandlerSymbol;
            NotifyPropertyChangedSymbol = notifyChangedSymbol;
            NotifyPropertyChangedHandlerSymbol = notifyChangedHandlerSymbol;
        }

        public string Construct(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            var nameSpace = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var INotifyPropertyChanging = NotifyPropertyChangingSymbol.ToDisplayString();
            var INotifyPropertyChanged = NotifyPropertyChangedSymbol.ToDisplayString();
            var eventMembers = classSymbol
                .GetMembers()
                .Where(x => x.Kind == SymbolKind.Event)
                .Cast<IEventSymbol>()
                .ToList();

            return
                Utils.FormatCode($@"
namespace {nameSpace}
{{
    partial class {className} : {INotifyPropertyChanging}, {INotifyPropertyChanged}
    {{
        {PropertyChangingEventHandler(eventMembers)} {PropertyChangedEventHandler(eventMembers)}

        {GenerateProperties(fields)}
    }}
}}
");
        }

        private string PropertyChangedEventHandler(List<IEventSymbol> eventMembers)
        {
            return eventMembers.Any(x => x.Type.Equals(NotifyPropertyChangedHandlerSymbol, SymbolEqualityComparer.Default))
                ? ""
                : "/// <inheritdoc/>" + Environment.NewLine +
                    "public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;" + Environment.NewLine + Environment.NewLine;
        }

        private string PropertyChangingEventHandler(List<IEventSymbol> eventMembers)
        {
            return eventMembers.Any(x => x.Type.Equals(NotifyPropertyChangingHandlerSymbol, SymbolEqualityComparer.Default))
                ? ""
                : "/// <inheritdoc/>" + Environment.NewLine +
                    "public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;" + Environment.NewLine + Environment.NewLine;
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
                Context.ReportDiagnostic(Diagnostic.Create(BlubbiChangedGenerator.CannotFindSuitablePropertyNameWarning, field.Locations.FirstOrDefault(), field.Name));
                return null;
            }

            if (field.IsReadOnly)
            {
                // Not supported
                // TODO: issue diagnostic
                Context.ReportDiagnostic(Diagnostic.Create(BlubbiChangedGenerator.FieldIsReadonlyWarning, field.Locations.FirstOrDefault(), field.Name));
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
            //workspace.Dispose();
        }
    }
}
