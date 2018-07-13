using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dpGenerator
{
    public class PropertySupport
    {
        public readonly DotNetPropertyResult DotNetProperty;
        public readonly AttachedPropertyAccessorsResult AttachedPropertyAccessors;
        public readonly ChangedCallbackResult ChangedCallback;
        public readonly DependencyPropertyResult DependencyProperty;

        public PropertySupport(IdentifierNameSyntax dpName, ClassDeclarationSyntax @class, TypeSyntax type, VariableDeclaratorSyntax variable)
        {
            DotNetProperty = new DotNetPropertyResult(type, dpName, variable);
            AttachedPropertyAccessors = new AttachedPropertyAccessorsResult(type, variable);
            ChangedCallback = new ChangedCallbackResult(@class, variable);
            DependencyProperty = new DependencyPropertyResult(type, dpName, @class, variable);            
        }
    }
}
