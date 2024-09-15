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

        var taggedProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass.Name == "ActionTagAttribute"))
            .ToList();

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine($@"
using System.Diagnostics;
using System.Collections.Generic;

namespace {namespaceName}
{{
    public static class {className}ActionExtensions
    {{
        public static Activity {methodName}(this Activity activity, {className} obj, IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
        {{
            if (activity == null || obj == null) return activity;
");

        foreach (var property in taggedProperties)
        {
            var attribute = property.GetAttributes().First(a => a.AttributeClass.Name == "ActionTagAttribute");
            var tagName = attribute.ConstructorArguments[0].Value?.ToString() ?? property.Name.ToLowerInvariant();
            var prefix = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value?.ToString() : null;
            var fullTagName = string.IsNullOrEmpty(prefix) ? tagName : $"{prefix}.{tagName}";

            sourceBuilder.AppendLine($@"            if (obj.{property.Name} != null)
                activity.SetTag(""{fullTagName}"", obj.{property.Name});");
        }

        sourceBuilder.AppendLine(@"
            if (additionalTags != null)
            {
                foreach (var tag in additionalTags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }

            return activity;
        }
    }
}");

        return sourceBuilder.ToString();
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
