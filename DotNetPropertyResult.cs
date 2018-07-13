using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    public class DotNetPropertyResult
    {
        private readonly TypeSyntax type;
        private readonly IdentifierNameSyntax dpName;
        private readonly VariableDeclaratorSyntax variable;

        public DotNetPropertyResult(TypeSyntax type, IdentifierNameSyntax dpName, VariableDeclaratorSyntax variable)
        {
            this.type = type;
            this.dpName = dpName;
            this.variable = variable;
        }

        private PropertyDeclarationSyntax WrapProperty()
        {
            return PropertyDeclaration(type, variable.Identifier)
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(Whitespace(" "))))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithLeadingTrivia(CarriageReturnLineFeed, Tab)
                        .WithBody(Block(ReturnStatement(CastExpression(type.WithoutTrailingTrivia(), InvocationExpression(Program.GetValue, ArgumentList(SingletonSeparatedList(Argument(dpName)))))
                                                                                                    .WithLeadingTrivia(Whitespace(" ")))
                                                                    .WithLeadingTrivia(Whitespace(" "))
                                                                    .WithTrailingTrivia(Whitespace(" ")))),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithLeadingTrivia(CarriageReturnLineFeed, Tab)
                        .WithBody(Block(ExpressionStatement(InvocationExpression(Program.SetValue, ArgumentList(SeparatedList(new ArgumentSyntax[] { Argument(dpName), Argument(IdentifierName("value")).WithLeadingTrivia(Whitespace(" ")) }))))
                                                                    .WithLeadingTrivia(Whitespace(" "))
                                                                    .WithTrailingTrivia(Whitespace(" "))))
                        .WithTrailingTrivia(CarriageReturnLineFeed));
        }

        public override string ToString()
        {
            return WrapProperty().ToString();
        }
    }
}
