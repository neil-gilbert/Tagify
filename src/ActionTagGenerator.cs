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

        foreach (var typeSymbol in receiver.CandidateTypes)
        {
            var classSource = GenerateExtensionMethod(typeSymbol);
            context.AddSource($"{typeSymbol.Name}_ActionTags.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private string GenerateExtensionMethod(INamedTypeSymbol typeSymbol)
    {
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;
        var methodName = $"AddActionTagsFor{typeName}";

        var classAttribute = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ActionTagAttribute");
        var classPrefix = classAttribute?.ConstructorArguments.Length > 1 
            ? classAttribute?.ConstructorArguments[1].Value?.ToString() 
            : null;

        var taggedProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                        p.Name != "EqualityContract" &&
                        (classAttribute != null || p.GetAttributes().Any(a => a.AttributeClass?.Name == "ActionTagAttribute")))
            .ToList();

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine($@"
using System.Diagnostics;
using System.Collections.Generic;

namespace {namespaceName}
{{
    public static class {typeName}ActionExtensions
    {{
        public static Activity {methodName}(this Activity activity, {typeName} obj, IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
        {{
            if (activity == null || obj == null) return activity;
");

        foreach (var property in taggedProperties)
        {
            var attribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ActionTagAttribute");
            var tagName = attribute?.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value != null
                ? attribute.ConstructorArguments[0].Value.ToString()
                : property.Name.ToLowerInvariant();

            var propertyPrefix = attribute?.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value != null
                ? attribute.ConstructorArguments[1].Value.ToString()
                : classPrefix;

            var fullTagName = string.IsNullOrEmpty(propertyPrefix) ? tagName : $"{propertyPrefix}.{tagName}";

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
    public List<INamedTypeSymbol> CandidateTypes { get; } = new List<INamedTypeSymbol>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDeclaration)
        {
            ProcessTypeDeclaration(context, classDeclaration);
        }
        else if (context.Node is RecordDeclarationSyntax recordDeclaration)
        {
            ProcessTypeDeclaration(context, recordDeclaration);
        }
    }

    private void ProcessTypeDeclaration(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclaration)
    {
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol
            { DeclaredAccessibility: Accessibility.Public } symbol)
            return;

        var hasClassAttribute = symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == ActionTagGenerator.ActionTagAttributeName);

        var hasPropertyAttributes = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(prop => prop.DeclaredAccessibility == Accessibility.Public &&
                         prop.Name != "EqualityContract" &&
                         prop.GetAttributes()
                             .Any(attr => attr.AttributeClass?.Name == ActionTagGenerator.ActionTagAttributeName));

        if (hasClassAttribute || hasPropertyAttributes)
        {
            CandidateTypes.Add(symbol);
        }
    }
}
