using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    public class VariableResult
    {
        public int GroupIndex { get; set; }
        public SyntaxNode Value { get; set; }
    }

    class Program
    {
		private static readonly IdentifierNameSyntax getValue = SyntaxFactory.IdentifierName("GetValue");
        private static readonly IdentifierNameSyntax setValue = SyntaxFactory.IdentifierName("SetValue");
			
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
            return SyntaxFactory.PropertyDeclaration(type, variable.Identifier)
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Whitespace(" "))))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Tab)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(type.WithoutTrailingTrivia(), SyntaxFactory.InvocationExpression(getValue, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(dpName)))))
                                                                                                     .WithLeadingTrivia(SyntaxFactory.Whitespace(" ")))
                                                                       .WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
                                                                       .WithTrailingTrivia(SyntaxFactory.Whitespace(" ")))),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Tab)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(setValue, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[] { SyntaxFactory.Argument(dpName), SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")).WithLeadingTrivia(SyntaxFactory.Whitespace(" ")) }))))
                                                                       .WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
                                                                       .WithTrailingTrivia(SyntaxFactory.Whitespace(" "))))
                            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
        }

        private static MethodDeclarationSyntax AddChangedMethod(ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {
            var variableName = string.Concat(@class.Identifier.Text[0].ToString().ToLowerInvariant(), @class.Identifier.Text.Substring(1));
            return SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), GetCallbackMethodName(variable))
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("d")).WithType(SyntaxFactory.ParseTypeName("DependencyObject")),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")).WithType(SyntaxFactory.ParseTypeName("DependencyPropertyChangedEventArgs"))
                    })))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                        .WithVariables(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, SyntaxFactory.IdentifierName("d"), SyntaxFactory.IdentifierName(@class.Identifier.Text))))
                    }))), SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, SyntaxFactory.IdentifierName(variableName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), SyntaxFactory.Block())))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .NormalizeWhitespace();
        }

        private static FieldDeclarationSyntax AddDependencyProperty(TypeSyntax type, IdentifierNameSyntax dpName, ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable, bool renderFrameworkCallback)
        {            
            var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("DependencyProperty"), SyntaxFactory.IdentifierName("Register"));

            var frameworkMetadataArguments = new ArgumentSyntax[] { SyntaxFactory.Argument(variable.Initializer?.Value ?? SyntaxFactory.DefaultExpression(type)) };
            if (renderFrameworkCallback)
                frameworkMetadataArguments = frameworkMetadataArguments.Concat(new []{ SyntaxFactory.Argument(SyntaxFactory.IdentifierName(GetCallbackMethodName(variable))) }).ToArray();

            var argumentList = SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof")).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variable.Identifier.Text))})))),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(type)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(@class.Identifier.Text))),
                SyntaxFactory.Argument(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("FrameworkPropertyMetadata")).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(frameworkMetadataArguments))))
            });

            var registerCall =
                SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(memberaccess,
                SyntaxFactory.ArgumentList(argumentList)));

            return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("DependencyProperty"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(dpName.Identifier.Text))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(memberaccess, SyntaxFactory.ArgumentList(argumentList))
                )))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
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
                var dpName = SyntaxFactory.IdentifierName(string.Concat(variable.Identifier.Text, "Property"));
                yield return new VariableResult() { GroupIndex = 0, Value = (SyntaxNode)WrapProperty(variableDeclaration.Type, dpName, variable) };
                yield return new VariableResult() { GroupIndex = 1, Value = (SyntaxNode)AddChangedMethod(@class, variable) };
                yield return new VariableResult() { GroupIndex = 2, Value = (SyntaxNode)AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable, true) };
                yield return new VariableResult() { GroupIndex = 3, Value = (SyntaxNode)AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable, false) };
            }
        }

        private static void ProcessClass(ClassDeclarationSyntax @class)
        {
            var results = Enumerable.Empty<VariableResult>();
            foreach (var variableDeclaration in @class.DescendantNodes().OfType<VariableDeclarationSyntax>())
                results = results.Concat(ProcessVariable(@class, variableDeclaration));

            foreach (var group in results.GroupBy(r => r.GroupIndex))
            {
                if(group.Key >= 3)
                    Console.WriteLine();

                foreach (var item in group)
                {
                    Console.WriteLine(item.Value);
                    if (group.Key < 2)
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
