using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoNotify
{
    // Sample from: https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs
    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        public const string attributeText = @"
namespace AutoNotify
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

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) => i.AddSource("AutoNotifyAttribute", attributeText));

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            var attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
            var notifyChangingSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanging");
            var notifyChangingHandlerSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangingEventHandler");
            var notifyChangedSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
            var notifyChangedHandlerSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangedEventHandler");

            using var cc = new ClassGenerator(attributeSymbol, notifyChangingSymbol, notifyChangingHandlerSymbol, notifyChangedSymbol, notifyChangedHandlerSymbol);

#pragma warning disable RS1024 // Compare symbols correctly, doesnt work reliable with SymbolEqualityComparer.Default.. misses dlls
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy(f => f.ContainingType))
#pragma warning restore RS1024 // Compare symbols correctly
            {
                context.AddSource($"{group.Key.Name}.autonotify.cs", SourceText.From(ProcessClass(cc, group.Key, group.ToList()), Encoding.UTF8));
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
                if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
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