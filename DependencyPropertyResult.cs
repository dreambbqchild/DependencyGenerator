using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    [Flags]
    public enum DpOptions
    {
        None = 0,
        RenderFrameworkCallback = (1 << 0),
        RenderAttachedProperty = (2 << 1)
    }

    public class DependencyPropertyResult
    {
        private readonly TypeSyntax type;
        private readonly IdentifierNameSyntax dpName;
        private readonly ClassDeclarationSyntax @class;
        private readonly VariableDeclaratorSyntax variable;

        public DependencyPropertyResult(TypeSyntax type, IdentifierNameSyntax dpName, ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {
            this.type = type;
            this.dpName = dpName;
            this.@class = @class;
            this.variable = variable;
        }        

        private FieldDeclarationSyntax AddDependencyProperty(DpOptions options)
        {
            var memberaccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("DependencyProperty"), IdentifierName((options & DpOptions.RenderAttachedProperty) == DpOptions.RenderAttachedProperty ? "RegisterAttached" : "Register"));

            var frameworkMetadataArguments = new ArgumentSyntax[] { Argument(variable.Initializer?.Value ?? DefaultExpression(type)) };
            if ((options & DpOptions.RenderFrameworkCallback) == DpOptions.RenderFrameworkCallback)
                frameworkMetadataArguments = frameworkMetadataArguments.Concat(new[] { Argument(IdentifierName(Program.GetCallbackMethodName(variable))) }).ToArray();

            var argumentList = SeparatedList(new[]
            {
                (options & DpOptions.RenderAttachedProperty) == DpOptions.RenderAttachedProperty
                    ? Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(variable.Identifier.Text))) :
                    Argument(InvocationExpression(IdentifierName("nameof")).WithArgumentList(ArgumentList(SeparatedList(new[] { Argument(IdentifierName(variable.Identifier.Text))})))),
                Argument(TypeOfExpression(type)),
                Argument(TypeOfExpression(ParseTypeName(@class.Identifier.Text))),
                Argument(ObjectCreationExpression(ParseTypeName("FrameworkPropertyMetadata")).WithArgumentList(ArgumentList(SeparatedList(frameworkMetadataArguments))))
            });

            var registerCall = ExpressionStatement(InvocationExpression(memberaccess, ArgumentList(argumentList)));

            return FieldDeclaration(
                VariableDeclaration(ParseTypeName("DependencyProperty"))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(dpName.Identifier.Text))
                                .WithInitializer(EqualsValueClause(InvocationExpression(memberaccess, ArgumentList(argumentList))
                )))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
            .NormalizeWhitespace();
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, 
                AddDependencyProperty(DpOptions.RenderFrameworkCallback), 
                AddDependencyProperty(DpOptions.None), 
                AddDependencyProperty(DpOptions.RenderAttachedProperty));
        }

        public string ToString(DpOptions options)
        {
            return AddDependencyProperty(options).ToString();
        }
    }
}
