using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
class Program {
 static int Main(string[] args) {
  int count=0,errors=0,editorImports=0;
  var options=new CSharpParseOptions(LanguageVersion.CSharp8,preprocessorSymbols:new[]{"UNITY_2020_3","UNITY_2020_2_OR_NEWER","UNITY_2019_1_OR_NEWER","UNITY_2018_1_OR_NEWER","UNITY_STANDALONE_WIN","UNITY_STANDALONE"});
  foreach(string file in Directory.GetFiles(args[0],"*.cs",SearchOption.AllDirectories)) {
   var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),options,file);
   foreach(var diagnostic in tree.GetDiagnostics()) if(diagnostic.Severity==DiagnosticSeverity.Error){Console.WriteLine(diagnostic);errors++;}
   if(!file.Replace('\\','/').Contains("/Editor/"))
    foreach(var u in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
     if(u.Name.ToString().StartsWith("UnityEditor")){Console.WriteLine("Active Player editor dependency: "+file+": "+u);editorImports++;}
   count++;
  }
  Console.WriteLine("Parsed "+count+" C# files with C# 8 grammar and Player symbols; "+errors+" syntax errors; "+editorImports+" unconditional Player editor imports. Unity API compilation NOT TESTED.");
  return errors==0&&editorImports==0?0:1;
 }
}
