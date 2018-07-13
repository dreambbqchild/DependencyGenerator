using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    public class AttachedPropertyAccessorsResult
    {        
        private readonly TypeSyntax type;
        private readonly VariableDeclaratorSyntax variable;

        public AttachedPropertyAccessorsResult(TypeSyntax type, VariableDeclaratorSyntax variable)
        {
            this.type = type;
            this.variable = variable;
        }

        private MethodDeclarationSyntax CreateAttachedGetter()
        {
            var name = string.Concat("Get", variable.Identifier.ValueText);
            return MethodDeclaration(type, name)
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("element")).WithType(ParseTypeName("UIElement"))
                })))
                .WithBody(Block(ReturnStatement(CastExpression(type, InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("element"), Program.GetValue))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]
                    {
                        Argument(IdentifierName(string.Concat(variable.Identifier.ValueText, "Property")))
                    })))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace();
        }

        private MethodDeclarationSyntax CreateAttachedSetter()
        {
            var name = string.Concat("Set", variable.Identifier.ValueText);
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), name)
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("element")).WithType(ParseTypeName("UIElement")),
                    Parameter(Identifier("value")).WithType(type)
                })))
                .WithBody(Block(ExpressionStatement(
                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("element"), Program.SetValue))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]
                    {
                        Argument(IdentifierName(string.Concat(variable.Identifier.ValueText, "Property"))),
                        Argument(IdentifierName("value")),
                    }))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace();
        }

        public override string ToString()
        {
            return string.Concat(CreateAttachedGetter(), Environment.NewLine, Environment.NewLine, CreateAttachedSetter());
        }
    }
}
