using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlubbiChanged
{
    // Sample from: https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs
    [Generator]
    public class BlubbiChangedGenerator : ISourceGenerator
    {
        internal static readonly DiagnosticDescriptor FieldIsReadonlyWarning = new("BLUBBICHNG001", "Field is readonly", "Readonly field {0} is not supported", "BlubbiChanged", DiagnosticSeverity.Error, true);
        internal static readonly DiagnosticDescriptor CannotFindSuitablePropertyNameWarning = new("BLUBBICHNG002", "Cannot find suitable property name", "Cannot find suitable property name for field {0}", "BlubbiChanged", DiagnosticSeverity.Error, true);

        public const string attributeText = @"
namespace BlubbiChanged
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [global::System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    internal sealed class AutoNotifyAttribute : global::System.Attribute
    {
        /// <summary>
        /// Set your own property name
        /// </summary>
        public string PropertyName { get; set; }

        public AutoNotifyAttribute()
        {
        }
    }
}
";

        public const string classAttributeText = @"
namespace BlubbiChanged
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    [global::System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    internal sealed class AutoNotifyClassAttribute : global::System.Attribute
    {
        public AutoNotifyClassAttribute()
        {
        }
    }
}
";

        public const string ignoreAttributeText = @"
namespace BlubbiChanged
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [global::System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    internal sealed class AutoNotifyIgnoreAttribute : global::System.Attribute
    {
        public AutoNotifyIgnoreAttribute()
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) =>
            {
                i.AddSource("AutoNotifyAttribute", attributeText);
                i.AddSource("AutoNotifyClassAttribute", classAttributeText);
                i.AddSource("AutoNotifyIgnoreAttribute", ignoreAttributeText);
            });

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            var attributeSymbol = context.Compilation.GetTypeByMetadataName("BlubbiChanged.AutoNotifyAttribute");
            var classAttributeSymbol = context.Compilation.GetTypeByMetadataName("BlubbiChanged.AutoNotifyClassAttribute");
            var ignoreAttributeSymbol = context.Compilation.GetTypeByMetadataName("BlubbiChanged.AutoNotifyIgnoreAttribute");
            var notifyChangingSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanging");
            var notifyChangingHandlerSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangingEventHandler");
            var notifyChangedSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
            var notifyChangedHandlerSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangedEventHandler");

            using var cc = new ClassGenerator(context, attributeSymbol, classAttributeSymbol, ignoreAttributeSymbol, notifyChangingSymbol, notifyChangingHandlerSymbol, notifyChangedSymbol, notifyChangedHandlerSymbol);

#pragma warning disable RS1024 // Compare symbols correctly, doesnt work reliable with SymbolEqualityComparer.Default.. misses dlls
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy(f => f.ContainingType))
#pragma warning restore RS1024 // Compare symbols correctly
            {
                context.AddSource($"{group.Key.Name}.blubbichanged.cs", SourceText.From(ProcessClass(cc, group.Key, group.ToList()), Encoding.UTF8));
            }
        }

        private string ProcessClass(ClassGenerator cc, INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return null; //TODO: issue a diagnostic that it must be top level

            return cc.Construct(classSymbol, fields);
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IFieldSymbol> Fields { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
                    && classDeclarationSyntax.AttributeLists.Count > 0)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                    if (symbol.GetAttributes().Any(x => x.AttributeClass.ToDisplayString() == "BlubbiChanged.AutoNotifyClassAttribute"))
                    {
                        Fields.AddRange(symbol.GetMembers().OfType<IFieldSymbol>());
                    }
                }
                else if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "BlubbiChanged.AutoNotifyAttribute"))
                        {
                            Fields.Add(fieldSymbol);
                        }
                    }
                }

                // TODO: issue error diagnostic when field is used and not private
            }
        }
    }
}