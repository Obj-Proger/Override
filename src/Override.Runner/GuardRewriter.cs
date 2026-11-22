using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Override.Runner;

/// <summary>
/// Puts guard calls into the reader's code: a tick at the start of every loop body, and enter and exit around
/// the body of every method, local function, accessor and constructor. An endless loop or recursion then
/// stops with a message instead of freezing the page or overflowing the stack.
/// </summary>
internal sealed class GuardRewriter : CSharpSyntaxRewriter
{
    private static readonly StatementSyntax _tick = SyntaxFactory.ParseStatement($"global::{Harness.GuardType}.Tick();");
    private static readonly StatementSyntax _enter = SyntaxFactory.ParseStatement($"global::{Harness.GuardType}.Enter();");
    private static readonly StatementSyntax _exit = SyntaxFactory.ParseStatement($"global::{Harness.GuardType}.Exit();");

    public static SyntaxTree Guard(SyntaxTree tree) =>
        tree.WithRootAndOptions(new GuardRewriter().Visit(tree.GetRoot()), tree.Options);

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var visited = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return visited.WithStatement(WithTick(visited.Statement));
    }

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
    {
        var visited = (DoStatementSyntax)base.VisitDoStatement(node)!;
        return visited.WithStatement(WithTick(visited.Statement));
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var visited = (ForStatementSyntax)base.VisitForStatement(node)!;
        return visited.WithStatement(WithTick(visited.Statement));
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return visited.WithStatement(WithTick(visited.Statement));
    }

    public override SyntaxNode? VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        var visited = (ForEachVariableStatementSyntax)base.VisitForEachVariableStatement(node)!;
        return visited.WithStatement(WithTick(visited.Statement));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        var body = BodyOf(visited.Body, visited.ExpressionBody, IsVoid(visited.ReturnType));
        return body is null
            ? visited
            : visited.WithBody(Guarded(body)).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var visited = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
        var body = BodyOf(visited.Body, visited.ExpressionBody, IsVoid(visited.ReturnType));
        return body is null
            ? visited
            : visited.WithBody(Guarded(body)).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var visited = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
        var body = BodyOf(visited.Body, visited.ExpressionBody, isVoid: true);
        return body is null
            ? visited
            : visited.WithBody(Guarded(body)).WithExpressionBody(null).WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        var visited = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node)!;
        var isVoid = !visited.Keyword.IsKind(SyntaxKind.GetKeyword);
        var body = BodyOf(visited.Body, visited.ExpressionBody, isVoid);
        return body is null
            ? visited
            : visited.WithBody(Guarded(body)).WithExpressionBody(null).WithSemicolonToken(default);
    }

    /// <summary>A property written as "int X => expression;" is a getter too.</summary>
    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (visited.ExpressionBody is not { } arrow)
            return visited;

        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, Guarded(BodyOf(null, arrow, isVoid: false)!));
        return visited
            .WithExpressionBody(null)
            .WithSemicolonToken(default)
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)));
    }

    private static BlockSyntax WithTick(StatementSyntax body) =>
        body is BlockSyntax block ? block.WithStatements(block.Statements.Insert(0, _tick)) : SyntaxFactory.Block(_tick, body);

    private static bool IsVoid(TypeSyntax type) =>
        type is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);

    /// <summary>The body as a block; an expression body becomes a return, a statement or a throw.</summary>
    private static BlockSyntax? BodyOf(BlockSyntax? body, ArrowExpressionClauseSyntax? arrow, bool isVoid)
    {
        if (body is not null)
            return body;

        if (arrow is null)
            return null;

        StatementSyntax statement = arrow.Expression switch
        {
            ThrowExpressionSyntax thrown => SyntaxFactory.ThrowStatement(thrown.Expression),
            var expression when isVoid => SyntaxFactory.ExpressionStatement(expression),
            var expression => SyntaxFactory.ReturnStatement(expression),
        };

        return SyntaxFactory.Block(statement);
    }

    private static BlockSyntax Guarded(BlockSyntax body) =>
        SyntaxFactory.Block(
            _enter,
            SyntaxFactory.TryStatement(body, default, SyntaxFactory.FinallyClause(SyntaxFactory.Block(_exit))));
}
