using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
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

        private AdhocWorkspace Workspace;

        [SetUp]
        public void Setup()
        {
            Workspace = new();
        }

        [Test, Explicit]
        public void DebugGenerator()
        {
            var input = File.ReadAllText(@"D:\Entwicklung\GitHub\Projects\AutoNotify\AutoNotify\AutoNotify.Playground\ViewModel\test.cs");
            var sources = new Dictionary<string, string>()
            {
                { "test.cs", input }
            };
            var outputs = GetGeneratedOutput<AutoNotifyGenerator>(sources);
            ;
        }

        [TestCaseSource(typeof(AutoNotifyGeneratiorTestCases), nameof(AutoNotifyGeneratiorTestCases.SomeTestCases))]
        public void Test1(string input, string expected)
        {
            var sources = new Dictionary<string, string>()
            {
                { "test.cs", input }
            };
            var outputs = GetGeneratedOutput<AutoNotifyGenerator>(sources);
            Assert.That(outputs.Last().Value, Is.EqualTo(Utils.FormatCode(expected, Workspace)));
        }

        [TearDown]
        public void TearDown()
        {
            Workspace.Dispose();
        }

        private static class AutoNotifyGeneratiorTestCases
        {
            public static IEnumerable<TestCaseData> SomeTestCases()
            {
                yield return new TestCaseData(
                    @"
using System;
using AutoNotify;

namespace UnitTest
{
    private partial class UnitTestClass
    {
        [AutoNotify]
        private string normalProperty;
    }
}",
                    @"
namespace UnitTest
{
    public partial class UnitTestClass : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public string NormalProperty
        {
            get => this.normalProperty;
            set
            {
                if (global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(this.normalProperty, value))
                    return;

                this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(""NormalProperty""));

                this.normalProperty = value;

                this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(""NormalProperty""));
            }
        }

    }
}
")
                    .SetName("Normal property");

                yield return new TestCaseData(
                @"
using System;
using AutoNotify;

namespace UnitTest
{
    private partial class UnitTestClass
    {
        [AutoNotify]
        private string _normalProperty;
    }
}",
                @"
namespace UnitTest
{
    public partial class UnitTestClass : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public string NormalProperty
        {
            get => this._normalProperty;
            set
            {
                if (global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(this._normalProperty, value))
                    return;

                this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(""NormalProperty""));

                this._normalProperty = value;

                this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(""NormalProperty""));
            }
        }

    }
}
")
                .SetName("Normal property with underscore");

                yield return new TestCaseData(
                @"
using System;
using AutoNotify;

namespace UnitTest
{
    private partial class UnitTestClass
    {
        /// <summary>
        /// So much summary. Its <see langword=""true""/>.
        /// We created from <see cref=""normalPropertyWithSummary""/>
        /// a new property of <see langword=""int""/> <see cref=""NormalPropertyWithSummary""/>
        /// </summary>
        [AutoNotify]
        private int normalPropertyWithSummary;
    }
}",
                @"
namespace UnitTest
{
    public partial class UnitTestClass : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// So much summary. Its <see langword=""true""/>.
        /// We created from <see cref=""F:UnitTest.UnitTestClass.normalPropertyWithSummary""/>
        /// a new property of <see langword=""int""/> <see cref=""!:NormalPropertyWithSummary""/>
        /// </summary>
        public int NormalPropertyWithSummary
        {
            get => this.normalPropertyWithSummary;
            set
            {
                if (global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(this.normalPropertyWithSummary, value))
                    return;

                this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(""NormalPropertyWithSummary""));

                this.normalPropertyWithSummary = value;

                this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(""NormalPropertyWithSummary""));
            }
        }

    }
}
")
                .SetName("Normal property with summary");

                yield return new TestCaseData(
                    @"
using System;
using AutoNotify;

namespace UnitTest
{
    private partial class UnitTestClass
    {
        [AutoNotify]
        private static bool staticProperty;
    }
}",
                    @"
namespace UnitTest
{
    public partial class UnitTestClass : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc/>
        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public bool StaticProperty
        {
            get => staticProperty;
            set
            {
                if (global::System.Collections.Generic.EqualityComparer<bool>.Default.Equals(staticProperty, value))
                    return;

                this.PropertyChanging?.Invoke(this, new global::System.ComponentModel.PropertyChangingEventArgs(""StaticProperty""));

                staticProperty = value;

                this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(""StaticProperty""));
            }
        }

    }
}
")
                    .SetName("Static property");
            }
        }
    }
}