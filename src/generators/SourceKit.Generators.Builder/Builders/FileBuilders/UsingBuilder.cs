using FluentChaining;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceKit.Generators.Builder.Commands;
using SourceKit.Tools;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceKit.Generators.Builder.Builders.FileBuilders;

public class UsingBuilder : ILink<FileBuildingCommand, CompilationUnitSyntax>
{
    private static readonly IEqualityComparer<UsingDirectiveSyntax> Comparer =
        EqualityComparerFactory.Create<UsingDirectiveSyntax>(
            (a, b) => a.Name.ToString().Equals(b.Name.ToString()),
            x => x.Name.ToString().GetHashCode());

    private readonly IChain<UsingBuildingCommand, UsingDirectiveSyntax> _commentChain;

    public UsingBuilder(IChain<UsingBuildingCommand, UsingDirectiveSyntax> commentChain)
    {
        _commentChain = commentChain;
    }

    public CompilationUnitSyntax Process(
        FileBuildingCommand request,
        SynchronousContext context,
        LinkDelegate<FileBuildingCommand, SynchronousContext, CompilationUnitSyntax> next)
    {
        var unit = next(request, context);

        UsingDirectiveSyntax[] usingDirectives = unit.Usings
            .Append(UsingDirective(IdentifierName("System")))
            .Append(UsingDirective(IdentifierName("System.Linq")))
            .Append(UsingDirective(IdentifierName("System.Collections.Generic")))
            .Distinct(Comparer)
            .OrderBy(x => x.Name.ToString())
            .ToArray();

        var firstDirective = usingDirectives[0];

        var commentBuildingCommand = new UsingBuildingCommand(firstDirective, request.Symbol);
        usingDirectives[0] = _commentChain.Process(commentBuildingCommand);

        return unit.WithUsings(List(usingDirectives));
    }
}