using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoNotify.Test
{
    public class Tests
    {
        public static Dictionary<string, string> GetGeneratedOutput<TGenerator>(IDictionary<string, string> sources, bool failOnInvalidSource = false)
           where TGenerator : ISourceGenerator, new()
           => GetGeneratedOutput(sources, () => new TGenerator(), failOnInvalidSource);

        public static CSharpCompilation Compile(IDictionary<string, string> sources)
                    => CSharpCompilation.Create(
                        "test",
                        sources.Select(x => CSharpSyntaxTree.ParseText(x.Value, path: x.Key)),
                        new[]
                        {
                            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).GetTypeInfo().Assembly.Location),
                        },
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        public static Dictionary<string, string> GetGeneratedOutput(IDictionary<string, string> sources, Func<ISourceGenerator> makeGenerator, bool failOnInvalidSource = false)
        {
            var compilation = Compile(sources);

            if (failOnInvalidSource)
            {
                FailIfError(compilation.GetDiagnostics());
            }

            var generator = makeGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
            var output = outputCompilation.SyntaxTrees.ToDictionary(tree => tree.FilePath, tree => tree.ToString());
            FailIfError(generateDiagnostics);

            return output;
        }

        private static void FailIfError(IEnumerable<Diagnostic> diag)
        {
            var errors = diag.Where(d => d.Severity == DiagnosticSeverity.Error);
            var msg = "Failed: " + errors.FirstOrDefault()?.GetMessage();
            Assert.That(errors, Is.Empty, msg);
        }

        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void Test1()
        {
            var input = File.ReadAllText(@"D:\Entwicklung\GitHub\Projects\AutoNotify\AutoNotify\AutoNotify.Playground\ViewModel\test.cs");

            var sources = new Dictionary<string, string>()
            {
                //{ "AutoNotifyAttribute.cs", AutoNotifyGenerator.attributeText },
                { "test.cs", input }
            };
            var outputs = GetGeneratedOutput<AutoNotifyGenerator>(sources);
            var aaaaaaaaaaaaa = outputs.Last().Value;
            Assert.Pass();
        }
    }
}