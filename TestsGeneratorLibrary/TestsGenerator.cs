﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGeneratorLibrary
{
    public class TestsGenerator
    {
        public List<TestFile> CreateTests(string sourceCode)
        {
            List<TestFile> list = new List<TestFile>();
            SyntaxNode root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (ClassDeclarationSyntax cds in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                ClassDeclarationSyntax testClass = CreateTestClass(cds.Identifier.ValueText);
                var methods = cds.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(node => node.Modifiers.Any(n => n.IsKind(SyntaxKind.PublicKeyword))).ToList();
                methods.Sort((method1, method2) => string.Compare(method1.Identifier.Text, method2.Identifier.Text, StringComparison.Ordinal));
                var methodIndex = 0;
                for (var i = 0; i < methods.Count; ++i)
                {
                    if (i != 0 && methods[i].Identifier.Text == methods[i - 1].Identifier.Text)
                    {
                        methodIndex++;
                    }
                    else if (i != methods.Count - 1 && methods[i].Identifier.Text == methods[i + 1].Identifier.Text)
                    {
                        methodIndex = 1;
                    }
                    else
                    {
                        methodIndex = -1;
                    }
                    var methodName = methods[i].Identifier.Text + (methodIndex != -1 ? $"{methodIndex}" : "");
                    testClass = testClass.AddMembers(CreateTestMethod(methodName));

                }
                CompilationUnitSyntax unit = CompilationUnit()
                    .WithUsings(new SyntaxList<UsingDirectiveSyntax>(usings)
                        .Add(UsingDirective(QualifiedName(IdentifierName("NUnit"), IdentifierName("Framework")))))
                    .AddMembers(NamespaceDeclaration(ParseName("tests")).AddMembers(testClass));
                list.Add(new TestFile($"{cds.Identifier.ValueText}Tests.cs", unit.NormalizeWhitespace().ToFullString()));
            }
            return list;
        }

        private ClassDeclarationSyntax CreateTestClass(string className)
        {
            AttributeSyntax attr = Attribute(ParseName("TestFixture"));
            ClassDeclarationSyntax testClass = ClassDeclaration(className + "Test").
                                               AddModifiers(Token(SyntaxKind.PublicKeyword)).
                                               AddAttributeLists(AttributeList().AddAttributes(attr));
            return testClass;
        }

        private MethodDeclarationSyntax CreateTestMethod(string methodName)
        {
            AttributeSyntax attr = Attribute(ParseName("Test"));
            MethodDeclarationSyntax testMethod = MethodDeclaration(ParseTypeName("void"), methodName + "Test").
                                                 AddModifiers(Token(SyntaxKind.PublicKeyword)).
                                                 AddBodyStatements(EmptyTestSyntax()).
                                                 AddAttributeLists(AttributeList().AddAttributes(attr));
            return testMethod;
        }

        private StatementSyntax[] EmptyTestSyntax()
        {
            StatementSyntax[] code = { ParseStatement("Assert.Fail(\"autogenerated\");") };
            return code;
        }
    }
}
