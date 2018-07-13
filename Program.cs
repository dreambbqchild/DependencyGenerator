using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    using static SyntaxFactory;    

    class Program
    {
		public static readonly IdentifierNameSyntax GetValue = IdentifierName("GetValue");
        public static readonly IdentifierNameSyntax SetValue = IdentifierName("SetValue");

        public static string GetCallbackMethodName(VariableDeclaratorSyntax variable)
        {
            return string.Concat(variable.Identifier.Text, "ChangedCallback");
        }

        private static string ReadPostData()
        {
            var contentLength = Convert.ToInt32(Environment.GetEnvironmentVariable("CONTENT_LENGTH"));
            var buffer = new char[contentLength];
            Console.In.Read(buffer, 0, contentLength);
            return new string(buffer);
        }		        

        private static IEnumerable<PropertySupport> ProcessVariable(ClassDeclarationSyntax @class, VariableDeclarationSyntax variableDeclaration)
        {
            foreach (var variable in variableDeclaration.Variables)
            {
                var dpName = IdentifierName(string.Concat(variable.Identifier.Text, "Property"));
                yield return new PropertySupport(dpName, @class, variableDeclaration.Type, variable);
            }
        }

        private static void ProcessClass(ClassDeclarationSyntax @class)
        {
            var results = Enumerable.Empty<PropertySupport>();
            foreach (var variableDeclaration in @class.DescendantNodes().OfType<VariableDeclarationSyntax>())
                results = results.Concat(ProcessVariable(@class, variableDeclaration));

            var finalResults = results.ToArray();

            foreach (var result in finalResults)
            { 
                Console.WriteLine(result.DotNetProperty);
                Console.WriteLine();
            }

            foreach (var result in finalResults)
            { 
                Console.WriteLine(result.AttachedPropertyAccessors);
                Console.WriteLine();
            }

            foreach (var result in finalResults)
            {
                Console.WriteLine(result.ChangedCallback);
                Console.WriteLine();
            }

            Console.WriteLine();

            foreach (var result in finalResults)
                Console.WriteLine(result.DependencyProperty.ToString(DpOptions.RenderFrameworkCallback));

            Console.WriteLine();

            foreach (var result in finalResults)
                Console.WriteLine(result.DependencyProperty.ToString(DpOptions.None));

            Console.WriteLine();

            foreach (var result in finalResults)
                Console.WriteLine(result.DependencyProperty.ToString(DpOptions.RenderAttachedProperty));
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
