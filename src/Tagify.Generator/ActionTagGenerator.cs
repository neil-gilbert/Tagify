using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Tagify.Generator;

[Generator]
public class ActionTagGenerator : ISourceGenerator
{
    public const string ActionTagAttributeName = "ActionTagAttribute";
    
    public void Initialize(GeneratorInitializationContext context)
    {
#pragma warning disable RS1035
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
#pragma warning restore RS1035
    }

    public void Execute(GeneratorExecutionContext context)
    {
#pragma warning disable RS1035
        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
#pragma warning restore RS1035
            return;

        foreach (var typeSymbol in receiver.CandidateTypes)
        {
            var classSource = GenerateExtensionMethod(typeSymbol);
#pragma warning disable RS1035
            context.AddSource($"{typeSymbol.Name}_ActionTags.g.cs", SourceText.From(classSource, Encoding.UTF8));
#pragma warning restore RS1035
        }
    }

    private string GenerateExtensionMethod(INamedTypeSymbol typeSymbol)
    {
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;
        var methodName = $"AddActionTagsFor{typeName}";

        var classAttribute = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == ActionTagAttributeName);
        var classPrefix = classAttribute?.ConstructorArguments.Length > 1 
            ? classAttribute?.ConstructorArguments[1].Value?.ToString() 
            : null;

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine($@"
using System.Diagnostics;
using System.Collections.Generic;

namespace {namespaceName}
{{
    public static class {typeName}ActionExtensions
    {{
        public static Activity {methodName}(this Activity activity, {typeName} obj, string? parentPrefix = null, IEnumerable<KeyValuePair<string, object?>>? additionalTags = null)
        {{
            if (activity == null || obj == null) return activity;

            var prefix = string.IsNullOrEmpty(parentPrefix) ? ""{classPrefix ?? ""}"" : $""{{parentPrefix}}.{classPrefix ?? ""}"";
");

        GeneratePropertyTags(sourceBuilder, typeSymbol, "obj", "prefix");

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

    private void GeneratePropertyTags(StringBuilder sourceBuilder, INamedTypeSymbol typeSymbol, string objName, string prefixName)
    {
        var taggedProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                        p.Name != "EqualityContract" &&
                        (typeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == ActionTagAttributeName) ||
                         p.GetAttributes().Any(a => a.AttributeClass?.Name == ActionTagAttributeName)))
            .ToList();

        foreach (var property in taggedProperties)
        {
            var attribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == ActionTagAttributeName);
            var tagName = attribute?.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value != null
                ? attribute.ConstructorArguments[0].Value?.ToString()
                : property.Name.ToLowerInvariant();

            var propertyPrefix = attribute?.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value != null
                ? attribute.ConstructorArguments[1].Value?.ToString()
                : null;

            var fullTagName = string.IsNullOrEmpty(propertyPrefix) 
                ? $"$\"{{{{prefixName}}}}.{{tagName}}\""
                : $"$\"{{{{prefixName}}}}.{{propertyPrefix}}.{{tagName}}\"";

            if (IsNestedType(property.Type))
            {
                sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
                {{
                    {property.Type.Name}ActionExtensions.AddActionTagsFor{property.Type.Name}(activity, {objName}.{property.Name}, {fullTagName});
                }}");
            }
            else
            {
                sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
                {{
                    activity.SetTag({fullTagName}, {objName}.{property.Name});
                }}");
            }
        }
    }

    private bool IsNestedType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return !namedType.IsPrimitive() &&
                   (namedType.TypeKind == TypeKind.Class || namedType.TypeKind == TypeKind.Struct);
        }
        return false;
    }
}

internal static class TypeSymbolExtensions
{
    public static bool IsPrimitive(this ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean or
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64 or
            SpecialType.System_Decimal or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Char or
            SpecialType.System_String or
            SpecialType.System_Object => true,
            _ => false
        };
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
