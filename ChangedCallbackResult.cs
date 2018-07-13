using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    public class ChangedCallbackResult
    {
        private readonly ClassDeclarationSyntax @class;
        private readonly VariableDeclaratorSyntax variable;

        public ChangedCallbackResult(ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {
            this.@class = @class;
            this.variable = variable;
        }

        private MethodDeclarationSyntax AddChangedMethod()
        {
            var variableName = string.Concat(@class.Identifier.Text[0].ToString().ToLowerInvariant(), @class.Identifier.Text.Substring(1));
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Program.GetCallbackMethodName(variable))
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("d")).WithType(ParseTypeName("DependencyObject")),
                    Parameter(Identifier("e")).WithType(ParseTypeName("DependencyPropertyChangedEventArgs"))
                })))
                .WithBody(Block(LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SeparatedList(new[]
                {
                    VariableDeclarator(variableName)
                        .WithInitializer(EqualsValueClause(BinaryExpression(SyntaxKind.AsExpression, IdentifierName("d"), IdentifierName(@class.Identifier.Text))))
                }))), IfStatement(BinaryExpression(SyntaxKind.NotEqualsExpression, IdentifierName(variableName), LiteralExpression(SyntaxKind.NullLiteralExpression)), Block())))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace();
        }

        public override string ToString()
        {
            return AddChangedMethod().ToString();
        }
    }
}
