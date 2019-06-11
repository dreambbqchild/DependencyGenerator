using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    public enum DpOptions
    {
        None = 0,
        RenderFrameworkCallback,
        RenderAttachedProperty,
        RenderReadOnlyProperty
    }

    public class DependencyPropertyResult
    {
        private readonly TypeSyntax type;
        private readonly IdentifierNameSyntax dpName;
        private readonly IdentifierNameSyntax dpKey;
        private readonly ClassDeclarationSyntax @class;
        private readonly VariableDeclaratorSyntax variable;

        public DependencyPropertyResult(TypeSyntax type, IdentifierNameSyntax dpName, IdentifierNameSyntax dpKey, ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {
            this.type = type;
            this.dpName = dpName;
            this.dpKey = dpKey;
            this.@class = @class;
            this.variable = variable;
        }

        private SeparatedSyntaxList<ArgumentSyntax> GetArgs(DpOptions options)
        {
            var frameworkMetadataArguments = new ArgumentSyntax[] { Argument(variable.Initializer?.Value ?? DefaultExpression(type)) };
            if (options == DpOptions.RenderFrameworkCallback)
                frameworkMetadataArguments = frameworkMetadataArguments.Concat(new[] { Argument(IdentifierName(Program.GetCallbackMethodName(variable))) }).ToArray();

            return SeparatedList(new[]
            {
                options == DpOptions.RenderAttachedProperty
                    ? Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(variable.Identifier.Text))) :
                    Argument(InvocationExpression(IdentifierName("nameof")).WithArgumentList(ArgumentList(SeparatedList(new[] { Argument(IdentifierName(variable.Identifier.Text))})))),
                Argument(TypeOfExpression(type)),
                Argument(TypeOfExpression(ParseTypeName(@class.Identifier.Text))),
                Argument(ObjectCreationExpression(ParseTypeName("FrameworkPropertyMetadata")).WithArgumentList(ArgumentList(SeparatedList(frameworkMetadataArguments))))
            });
        }

        private FieldDeclarationSyntax DeclareField(IdentifierNameSyntax identifier, string typeName, ExpressionSyntax expression, SeparatedSyntaxList<ArgumentSyntax> args, SyntaxKind access = SyntaxKind.PublicKeyword)
        {
            return FieldDeclaration(
                VariableDeclaration(ParseTypeName(typeName))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(identifier.Identifier)
                        .WithInitializer(EqualsValueClause(InvocationExpression(expression, ArgumentList(args))
                )))))
            .WithModifiers(TokenList(Token(access), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
            .NormalizeWhitespace();
        }

        private FieldDeclarationSyntax AddDependencyProperty(DpOptions options)
        {
            var memberaccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("DependencyProperty"), IdentifierName(options == DpOptions.RenderAttachedProperty ? "RegisterAttached" : "Register"));           

            var argumentList = GetArgs(options);
            //var registerCall = ExpressionStatement(InvocationExpression(memberaccess, ArgumentList(argumentList)));
            return DeclareField(dpName, "DependencyProperty", memberaccess, argumentList);            
        }

        private FieldDeclarationSyntax[] AddReadOnlyDependencyProperty()
        {
            var key = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("DependencyProperty"), IdentifierName("RegisterReadOnly"));
            var argumentList = GetArgs(DpOptions.None);
            var registerCall = ExpressionStatement(InvocationExpression(key, ArgumentList(argumentList)));
            var keyField = DeclareField(dpKey, "DependencyPropertyKey", key, argumentList, SyntaxKind.PrivateKeyword);

            var property = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(dpKey.Identifier.Text), IdentifierName("DependencyProperty"));
            var propretyField = FieldDeclaration(
                VariableDeclaration(ParseTypeName("DependencyProperty"))
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(dpName.Identifier)
                    .WithInitializer(EqualsValueClause(property)))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
                .NormalizeWhitespace();

            return new FieldDeclarationSyntax[] { keyField, propretyField };
        }

        private string Stringify(params FieldDeclarationSyntax[] fields)
        {
            return string.Join(Environment.NewLine, fields.Select(f => f.ToString()));
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, 
                AddDependencyProperty(DpOptions.RenderFrameworkCallback), 
                AddDependencyProperty(DpOptions.None), 
                AddDependencyProperty(DpOptions.RenderAttachedProperty),
                Stringify(AddReadOnlyDependencyProperty()));
        }

        public string ToString(DpOptions options)
        {
            if (options == DpOptions.RenderReadOnlyProperty)
                return Stringify(AddReadOnlyDependencyProperty());

            return AddDependencyProperty(options).ToString();
        }
    }
}
