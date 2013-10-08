﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace Cs2hx
{
    static class WriteMethod
    {
        public static void Go(HaxeWriter writer, MethodDeclarationSyntax method)
        {
			var methodSymbol = TypeState.Instance.GetModel(method).GetDeclaredSymbol(method);

            writer.WriteIndent();

            if ((method.Identifier.ValueText != "ToString" || !TypeState.Instance.DerivesFromObject) && method.Modifiers.Any(SyntaxKind.OverrideKeyword))
                writer.Write("override ");
            if (method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(SyntaxKind.ProtectedKeyword) || method.Modifiers.Any(SyntaxKind.InternalKeyword))
                writer.Write("public ");
            if (method.Modifiers.Any(SyntaxKind.PrivateKeyword))
                writer.Write("private ");
            if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
                writer.Write("static ");

            writer.Write("function ");
			var methodName = OverloadResolver.MethodName(methodSymbol);

			if (methodName == "ToString")
				methodName = "toString";

            writer.Write(methodName);

            if (method.TypeParameterList != null)
            {
                writer.Write("<");
                writer.Write(string.Join(", ", method.TypeParameterList.Parameters.Select(o => o.Identifier.ValueText)));
                writer.Write(">");
            }

            writer.Write("(");

			var firstParam = true;
            foreach (var parameter in method.ParameterList.Parameters)
            {
				if (parameter.Modifiers.Any(SyntaxKind.OutKeyword) || parameter.Modifiers.Any(SyntaxKind.RefKeyword))
					throw new Exception("Ref and Out are not supported.  " + Utility.Descriptor(method));

				if (firstParam)
					firstParam = false;
				else
                    writer.Write(", ");

                writer.Write(parameter.Identifier.ValueText);
				writer.Write(TypeProcessor.ConvertTypeWithColon(parameter.Type));

				if (parameter.Default != null)
				{
					writer.Write(" = ");
					Core.Write(writer, parameter.Default.Value);
				}
            }

            writer.Write(")");
            writer.Write(TypeProcessor.ConvertTypeWithColon(method.ReturnType));

            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                writer.WriteLine();
                writer.WriteOpenBrace();
				writer.WriteIndent();

				if (method.ReturnType.ToString() != "void")
					writer.Write("return "); //"return" the throw statement to work around haxe limitations

                writer.Write("throw new Exception(\"Abstract item called\");\r\n");
                writer.WriteCloseBrace();
            }
            else if (method.Body == null)
                writer.Write(";\r\n"); //interface methods wind up here
            else
            {
                writer.WriteLine();
                writer.WriteOpenBrace();
                foreach (var statement in method.Body.Statements)
					Core.Write(writer, statement);

				TriviaProcessor.ProcessTrivias(writer, method.Body.DescendantTrivia());

                writer.WriteCloseBrace();
            }
        }
    }
}