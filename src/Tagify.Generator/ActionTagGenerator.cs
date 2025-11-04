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
");

        // Build the line that initializes the local 'prefix' variable in the generated method.
        string prefixLine;
        if (string.IsNullOrEmpty(classPrefix))
        {
            // No class-level prefix: inherit parentPrefix as-is (or empty if not provided)
            prefixLine = "            var prefix = string.IsNullOrEmpty(parentPrefix) ? string.Empty : parentPrefix;";
        }
        else
        {
            // Class-level prefix present: append to parent when provided
            prefixLine = $"            var prefix = string.IsNullOrEmpty(parentPrefix) ? \"{classPrefix}\" : $\"{{parentPrefix}}.{classPrefix}\";";
        }
        sourceBuilder.AppendLine(prefixLine);
        sourceBuilder.AppendLine("\n");

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

            // Build the tag key expression for primitive properties.
            // Rules:
            // - If propertyPrefix is null: use class-level prefix (variable 'prefix') when not empty; otherwise just tagName
            // - If propertyPrefix is "": ignore class prefix entirely and use just tagName
            // - If propertyPrefix is non-empty: ignore class prefix and use "{propertyPrefix}.{tagName}"
            string fullTagExpr;
            if (propertyPrefix is null)
            {
                fullTagExpr = $"(string.IsNullOrEmpty({prefixName}) ? \"{tagName}\" : $\"{{{prefixName}}}.{tagName}\")";
            }
            else if (propertyPrefix.Length == 0)
            {
                fullTagExpr = $"\"{tagName}\"";
            }
            else
            {
                fullTagExpr = $"\"{propertyPrefix}.{tagName}\"";
            }

            // For nested types, compute what to pass as the parent prefix into the nested call.
            // If propertyPrefix is null -> pass current prefix variable; if "" -> pass null; else pass the literal propertyPrefix.
            string nestedParentPrefixArg;
            if (propertyPrefix is null)
            {
                nestedParentPrefixArg = prefixName;
            }
            else if (propertyPrefix.Length == 0)
            {
                nestedParentPrefixArg = "null";
            }
            else
            {
                nestedParentPrefixArg = $"\"{propertyPrefix}\"";
            }

            var isNullableValueType = property.Type is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            var isNonNullableValueType = property.Type.IsValueType && !isNullableValueType;
            var isNested = IsNestedType(property.Type) && !isNullableValueType;

            if (isNested)
            {
                // Only traverse when the nested object is not null
                sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
            {{
                {property.Type.Name}ActionExtensions.AddActionTagsFor{property.Type.Name}(activity, {objName}.{property.Name}, {nestedParentPrefixArg});
            }}");
            }
            else
            {
                if (isNonNullableValueType)
                {
                    // Always set tag for non-nullable value types
                    sourceBuilder.AppendLine($@"            activity.SetTag({fullTagExpr}, {objName}.{property.Name});
");
                }
                else if (isNullableValueType)
                {
                    // Set tag only when HasValue for nullable value types
                    sourceBuilder.AppendLine($@"            if ({objName}.{property.Name}.HasValue)
            {{
                activity.SetTag({fullTagExpr}, {objName}.{property.Name});
            }}");
                }
                else
                {
                    // Reference types: set when not null
                    sourceBuilder.AppendLine($@"            if ({objName}.{property.Name} != null)
            {{
                activity.SetTag({fullTagExpr}, {objName}.{property.Name});
            }}");
                }
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
