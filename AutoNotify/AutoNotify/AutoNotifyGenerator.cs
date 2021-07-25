using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoNotify
{
    // https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs
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
        public string PropertyName { get; set; }

        public AutoNotifyAttribute()
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("AutoNotifyAttribute", attributeText));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private CLassGenerator cc;

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            // get the added attribute, INotifyPropertyChanged and INotifyPropertyChanging
            var attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
            var notifyChangingSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanging");
            var notifyChangedSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            cc = new CLassGenerator(attributeSymbol, notifyChangingSymbol, notifyChangedSymbol);

            // group the fields by class, and generate the source
#pragma warning disable RS1024 // Compare symbols correctly, doesnt work reliable with SymbolEqualityComparer.Default.. misses dlls
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy(f => f.ContainingType))
#pragma warning restore RS1024 // Compare symbols correctly
            {
                context.AddSource($"{group.Key.Name}_autoNotify.cs", SourceText.From(ProcessClass(group.Key, group.ToList()), Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return null; //TODO: issue a diagnostic that it must be top level

            return cc.Construct(classSymbol, fields);
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // any field with at least one attribute is a candidate for property generation
                if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        // Get the symbol being declared by the field, and keep it if its annotated
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
                        {
                            Fields.Add(fieldSymbol);
                        }
                    }
                }
                else if (context.Node is IdentifierNameSyntax nameSyntax && nameSyntax.Identifier.ToString() == "miro")
                {
                    // https://www.google.com/search?q=roslyn+symbolfinder
                    // https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.findsymbols.symbolfinder?view=roslyn-dotnet-3.10.0
                    // https://stackoverflow.com/questions/34340828/symbolfinder-findreferencesasync-doesnt-find-anything
                    ;
                }
            }
        }
    }
}