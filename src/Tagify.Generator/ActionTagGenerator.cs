using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Tagify;

[Generator]
public class ActionTagGenerator : ISourceGenerator
{
    public const string ActionTagAttributeName = "ActionTagAttribute";
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            return;

        foreach (var classSymbol in receiver.CandidateClasses)
        {
            var classSource = GenerateExtensionMethod(classSymbol);
            context.AddSource($"{classSymbol.Name}_ActionTags.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private string GenerateExtensionMethod(INamedTypeSymbol classSymbol)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var methodName = $"AddActionTagsFor{className}";

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine($@"
using System.Diagnostics;
using System.Collections.Generic;

namespace {namespaceName}
{{
    public static class {className}ActionExtensions
    {{
        public static Activity {methodName}(this Activity activity, {className} obj, string prefix = """", IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
        {{
            if (activity == null || obj == null) return activity;

            {GeneratePropertyTaggingCode(classSymbol, "obj", "prefix")}

            if (additionalTags != null)
            {{
                foreach (var tag in additionalTags)
                {{
                    activity.SetTag(tag.Key, tag.Value);
                }}
            }}

            return activity;
        }}
    }}
}}");

        return sourceBuilder.ToString();
    }

    private string GeneratePropertyTaggingCode(INamedTypeSymbol classSymbol, string objName, string prefixName)
    {
        var sourceBuilder = new StringBuilder();
        var classAttribute = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == ActionTagAttributeName);
        var classPrefix = classAttribute?.ConstructorArguments.Length > 0 ? classAttribute.ConstructorArguments[0].Value?.ToString() : null;

        foreach (var property in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var attribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == ActionTagAttributeName);
            if (attribute == null && !HasActionTagAttribute(property.Type)) continue;

            var tagName = attribute?.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value?.ToString() : property.Name.ToLowerInvariant();
            var propertyPrefix = attribute?.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value?.ToString() : null;

            var fullTagName = BuildFullTagName(prefixName, classPrefix, propertyPrefix, tagName);

            if (HasActionTagAttribute(property.Type))
            {
                sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
                {methodName}(activity, {objName}.{property.Name}, {fullTagName});");
            }
            else
            {
                sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
                activity.SetTag({fullTagName}, {objName}.{property.Name});");
            }
        }

        return sourceBuilder.ToString();
    }

    private bool HasActionTagAttribute(ITypeSymbol type)
    {
        return type.GetAttributes().Any(a => a.AttributeClass?.Name == ActionTagAttributeName);
    }

    private string BuildFullTagName(string prefixName, string? classPrefix, string? propertyPrefix, string tagName)
    {
        var parts = new[] { $"{prefixName}", classPrefix, propertyPrefix, tagName }
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => $"\"{p}\"");

        return $"string.Join(\".\", new[] {{ {string.Join(", ", parts)} }}.Where(p => !string.IsNullOrEmpty(p)))";
    }
}

internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> CandidateClasses { get; } = new List<INamedTypeSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax) return;
        
        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol
            { DeclaredAccessibility: Accessibility.Public } symbol) return;
        
        var hasClassAttribute = symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == ActionTagGenerator.ActionTagAttributeName);

        var hasPropertyAttributes = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(prop => prop.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == ActionTagGenerator.ActionTagAttributeName));

        if (hasClassAttribute || hasPropertyAttributes)
        {
            CandidateClasses.Add(symbol);
        }
    }
}
