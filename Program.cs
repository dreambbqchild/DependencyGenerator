using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;

    public class VariableResult
    {
        public int GroupIndex { get; set; }
        public SyntaxNode Value { get; set; }
    }

    class Program
    {
        [Flags]
        enum DpOptions
        {
            None = 0,
            RenderFrameworkCallback = (1 << 0),
            RenderAttachedProperty = (2 << 1)
        }

		private static readonly IdentifierNameSyntax getValue = IdentifierName("GetValue");
        private static readonly IdentifierNameSyntax setValue = IdentifierName("SetValue");
			
        private static string ReadPostData()
        {
            var contentLength = Convert.ToInt32(Environment.GetEnvironmentVariable("CONTENT_LENGTH"));
            var buffer = new char[contentLength];
            Console.In.Read(buffer, 0, contentLength);
            return new string(buffer);
        }

		private static string GetCallbackMethodName(VariableDeclaratorSyntax variable)
        {
            return string.Concat(variable.Identifier.Text, "ChangedCallback");
        }

        private static PropertyDeclarationSyntax WrapProperty(TypeSyntax type, IdentifierNameSyntax dpName, VariableDeclaratorSyntax variable)
        {
            return PropertyDeclaration(type, variable.Identifier)
                .WithTrailingTrivia(CarriageReturnLineFeed)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(Whitespace(" "))))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithLeadingTrivia(CarriageReturnLineFeed, Tab)
                        .WithBody(Block(ReturnStatement(CastExpression(type.WithoutTrailingTrivia(), InvocationExpression(getValue, ArgumentList(SingletonSeparatedList(Argument(dpName)))))
                                                                                                    .WithLeadingTrivia(Whitespace(" ")))
                                                                    .WithLeadingTrivia(Whitespace(" "))
                                                                    .WithTrailingTrivia(Whitespace(" ")))),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithLeadingTrivia(CarriageReturnLineFeed, Tab)
                        .WithBody(Block(ExpressionStatement(InvocationExpression(setValue, ArgumentList(SeparatedList(new ArgumentSyntax[] { Argument(dpName), Argument(IdentifierName("value")).WithLeadingTrivia(Whitespace(" ")) }))))
                                                                    .WithLeadingTrivia(Whitespace(" "))
                                                                    .WithTrailingTrivia(Whitespace(" "))))
                        .WithTrailingTrivia(CarriageReturnLineFeed));
        }

        private static MethodDeclarationSyntax CreateAttachedSetter(TypeSyntax type, VariableDeclaratorSyntax variable)
        {
            var name = string.Concat("Set", variable.Identifier.ValueText);
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), name)
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("element")).WithType(ParseTypeName("UIElement")),
                    Parameter(Identifier("value")).WithType(type)
                })))
                .WithBody(Block(ExpressionStatement(
                    InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("element"), IdentifierName("SetValue")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[] 
                    {
                        Argument(IdentifierName(string.Concat(variable.Identifier.ValueText, "Property"))),
                        Argument(IdentifierName("value")),
                    }))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace();
        }

        private static MethodDeclarationSyntax CreateAttachedGetter(TypeSyntax type, VariableDeclaratorSyntax variable)
        {
            var name = string.Concat("Get", variable.Identifier.ValueText);
            return MethodDeclaration(type, name)
                .WithParameterList(ParameterList(SeparatedList(new[]
                {
                    Parameter(Identifier("element")).WithType(ParseTypeName("UIElement"))
                })))
                .WithBody(Block(ReturnStatement(CastExpression(type, InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("element"), IdentifierName("GetValue")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]
                    {
                        Argument(IdentifierName(string.Concat(variable.Identifier.ValueText, "Property")))
                    })))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace();
        }

        private static MethodDeclarationSyntax AddChangedMethod(ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {
            var variableName = string.Concat(@class.Identifier.Text[0].ToString().ToLowerInvariant(), @class.Identifier.Text.Substring(1));
            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), GetCallbackMethodName(variable))
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

        private static FieldDeclarationSyntax AddDependencyProperty(TypeSyntax type, IdentifierNameSyntax dpName, ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable, DpOptions options)
        {            
            var memberaccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("DependencyProperty"), IdentifierName((options & DpOptions.RenderAttachedProperty) == DpOptions.RenderAttachedProperty ? "RegisterAttached" : "Register"));

            var frameworkMetadataArguments = new ArgumentSyntax[] { Argument(variable.Initializer?.Value ?? DefaultExpression(type)) };
            if ((options & DpOptions.RenderFrameworkCallback) == DpOptions.RenderFrameworkCallback)
                frameworkMetadataArguments = frameworkMetadataArguments.Concat(new []{ Argument(IdentifierName(GetCallbackMethodName(variable))) }).ToArray();

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

        private static void PrintNode<TNode>(TNode node)
             where TNode : SyntaxNode
        {
            Console.WriteLine(node);
            Console.WriteLine();
        }

        private static IEnumerable<VariableResult> ProcessVariable(ClassDeclarationSyntax @class, VariableDeclarationSyntax variableDeclaration)
        {
            foreach (var variable in variableDeclaration.Variables)
            {
                var dpName = IdentifierName(string.Concat(variable.Identifier.Text, "Property"));
                yield return new VariableResult() { GroupIndex = 0, Value = WrapProperty(variableDeclaration.Type, dpName, variable) };
                yield return new VariableResult() { GroupIndex = 1, Value = AddChangedMethod(@class, variable) };
                yield return new VariableResult() { GroupIndex = 2, Value = CreateAttachedSetter(variableDeclaration.Type, variable) };
                yield return new VariableResult() { GroupIndex = 3, Value = CreateAttachedGetter(variableDeclaration.Type, variable) };
                yield return new VariableResult() { GroupIndex = 4, Value = AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable, DpOptions.RenderFrameworkCallback) };
                yield return new VariableResult() { GroupIndex = 5, Value = AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable, DpOptions.None) };
                yield return new VariableResult() { GroupIndex = 6, Value = AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable, DpOptions.RenderAttachedProperty) };
            }
        }

        private static void ProcessClass(ClassDeclarationSyntax @class)
        {
            var results = Enumerable.Empty<VariableResult>();
            foreach (var variableDeclaration in @class.DescendantNodes().OfType<VariableDeclarationSyntax>())
                results = results.Concat(ProcessVariable(@class, variableDeclaration));

            foreach (var group in results.GroupBy(r => r.GroupIndex))
            {
                if(group.Key >= 4)
                    Console.WriteLine();

                foreach (var item in group)
                {
                    Console.WriteLine(item.Value);
                    if (group.Key < 4)
                        Console.WriteLine();
                }
            }
        }
		
        static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("REQUEST_METHOD") != "POST")
            {
                Console.WriteLine("Not a Post");
                return;
            }

            var tree = CSharpSyntaxTree.ParseText(ReadPostData());
			var root = tree.GetRoot();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
			
			foreach(var @class in classes)
				ProcessClass(@class);
        }
    }
}
