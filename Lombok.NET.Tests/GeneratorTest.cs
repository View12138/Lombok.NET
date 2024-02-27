using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lombok.NET.Tests;

internal static class GeneratorTest
{
	internal record Builder(OutputKind OutputKind, List<ISourceGenerator> Generators);

	public static Builder Build(OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
		=> new(outputKind, []);
	public static Builder AddGenerator(this Builder builder, ISourceGenerator generator)
	{
		builder.Generators.Add(generator);
		return builder;
	}
	public static Builder AddGenerator(this Builder builder, IIncrementalGenerator generator)
	{
		builder.Generators.Add(generator.AsSourceGenerator());
		return builder;
	}

	public static GeneratorDriverRunResult CSharpResult(this Builder builder, params string[] sources)
		=> builder.CSharpResult(sources.Select(_ => CSharpSyntaxTree.ParseText(_)).ToArray());

	public static GeneratorDriverRunResult CSharpResult(this Builder builder, params SyntaxTree[] sourceSyntaxTrees)
	{
		var generatorReferences = builder.Generators.Select(_ => MetadataReference.CreateFromFile(_.GetType().Assembly.Location));

		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
			.Select(_ => MetadataReference.CreateFromFile(_.Location))
			.Concat(generatorReferences);

		var compilationOptions = new CSharpCompilationOptions(builder.OutputKind);

		var compilation = CSharpCompilation.Create("Generator", sourceSyntaxTrees, references, compilationOptions);

		return CSharpGeneratorDriver
			.Create(builder.Generators)
			.RunGenerators(compilation)
			.GetRunResult();
	}
}