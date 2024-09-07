using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace OtelTagify;

[Generator]
public class OtelTagGenerator : ISourceGenerator
{
    private const string AttributeName = "OtelTagAttribute";
    private const string ConfigurationClassName = "OtelTagConfiguration";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
        {
            return;
        }

        foreach (var classSymbol in receiver.CandidateClasses)
        {
            string classSource = ProcessClass(classSymbol);
            context.AddSource($"{classSymbol.Name}_OtelTags.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private string ProcessClass(INamedTypeSymbol classSymbol)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var properties = classSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine($@"
using System.Diagnostics;

namespace {namespaceName}
{{
    public static class {classSymbol.Name}OtelExtensions
    {{
        public static Activity SetTagsFromObject(this Activity activity, {classSymbol.Name} obj)
        {{
            if (activity == null || obj == null) return activity;

            if ({ConfigurationClassName}.TagAllProperties)
            {{
                {GenerateAllPropertiesCode(properties)}
            }}
            else
            {{
                {GenerateTaggedPropertiesCode(properties)}
            }}
            return activity;
        }}
    }}
}}");

        return sourceBuilder.ToString();
    }

    private string GenerateAllPropertiesCode(ImmutableArray<IPropertySymbol> properties)
    {
        var codeBuilder = new StringBuilder();
        foreach (var property in properties)
        {
            codeBuilder.AppendLine($@"                if (obj.{property.Name} != null) 
                    activity.SetTag(""{property.Name.ToLowerInvariant()}"", obj.{property.Name}.ToString());");
        }
        return codeBuilder.ToString();
    }

    private string GenerateTaggedPropertiesCode(ImmutableArray<IPropertySymbol> properties)
    {
        var codeBuilder = new StringBuilder();
        foreach (var property in properties)
        {
            var attribute = property.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Name == AttributeName);
            if (attribute != null)
            {
                var tagName = attribute.ConstructorArguments[0].Value?.ToString();
                var prefix = attribute.ConstructorArguments.Length > 1 
                    ? attribute.ConstructorArguments[1].Value?.ToString() 
                    : null;

                var fullTagName = string.IsNullOrEmpty(prefix) ? tagName : $"{prefix}.{tagName}";
                codeBuilder.AppendLine($@"                if (obj.{property.Name} != null) 
                    activity.SetTag(""{fullTagName}"", obj.{property.Name}.ToString());");
            }
        }
        return codeBuilder.ToString();
    }
}

public class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> CandidateClasses { get; } = new List<INamedTypeSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
            if (symbol != null)
            {
                CandidateClasses.Add(symbol);
            }
        }
    }
}
