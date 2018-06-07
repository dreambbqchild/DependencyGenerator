using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
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
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.CastExpression(type, SyntaxFactory.InvocationExpression(getValue, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(dpName)))))))),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(setValue, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new ArgumentSyntax[] { SyntaxFactory.Argument(dpName), SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")) })))))));
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
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
        }

        private static FieldDeclarationSyntax AddDependencyProperty(TypeSyntax type, IdentifierNameSyntax dpName, ClassDeclarationSyntax @class, VariableDeclaratorSyntax variable)
        {            
            var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("DependencyProperty"), SyntaxFactory.IdentifierName("Register"));
            var argumentList = SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof")).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variable.Identifier.Text))})))),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(type)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(@class.Identifier.Text))),
                SyntaxFactory.Argument(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("FrameworkPropertyMetadata")).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] { SyntaxFactory.Argument(variable.Initializer?.Value ?? SyntaxFactory.DefaultExpression(type)), SyntaxFactory.Argument(SyntaxFactory.IdentifierName(GetCallbackMethodName(variable))) }))))
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
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))).NormalizeWhitespace();
        }

        private static void PrintNode<TNode>(TNode node)
             where TNode : SyntaxNode
        {
            Console.WriteLine(node.NormalizeWhitespace());
            Console.WriteLine();
        }

        private static void ProcessVariable(ClassDeclarationSyntax @class, VariableDeclarationSyntax variableDeclaration)
        {
            foreach (var variable in variableDeclaration.Variables)
            {
                var dpName = SyntaxFactory.IdentifierName(string.Concat(variable.Identifier.Text, "Property"));
				Console.WriteLine(string.Concat("--- ", variable.Identifier.Text, " ---"));
				Console.WriteLine();
                PrintNode(WrapProperty(variableDeclaration.Type, dpName, variable));
                PrintNode(AddChangedMethod(@class, variable));
                PrintNode(AddDependencyProperty(variableDeclaration.Type, dpName, @class, variable));
            }
        }

        private static void ProcessClass(ClassDeclarationSyntax @class)
        {
            foreach (var variableDeclaration in @class.DescendantNodes().OfType<VariableDeclarationSyntax>())
                ProcessVariable(@class, variableDeclaration);
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
