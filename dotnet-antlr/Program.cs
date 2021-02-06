﻿namespace dotnet_antlr
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    class Program
    {
        static string version = "1.4";

        public enum TargetType
        {
            Cpp,
            CSharp,
            Dart,
            Go,
            Java,
            JavaScript,
            Php,
            Python2,
            Python3,
            Swift,
        }

        public enum EncodingType
        {
            Native,
            Unix,
            Windows,
            Mac,
        }

        public static EncodingType GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return EncodingType.Unix;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return EncodingType.Windows;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return EncodingType.Mac;
            }

            throw new Exception("Cannot determine operating system!");
        }

        class Options
        {
           // public Options() { }

            [Option('a', "antlr4cs", Required=false, Default=false, HelpText = "Generate code for Antlr4cs runtime.")]
            public bool Antlr4cs { get; set; }

            [Option('c', "case-fold", Required=false, HelpText="Fold case of lexer. True = upper, false = lower.")]
            public bool? CaseFold { get; set; }

            [Option('e', "encoding", Required = false, Default = EncodingType.Native, HelpText = "End of line encoding.")]
            public EncodingType Encoding { get; set; }

            [Option('f', "file", Required=false, HelpText="The name of an input file to parse.")]
            public string InputFile { get; set; }

            [Option('g', "grammar-files", Required=false, HelpText="A list of semi-colon separated grammar files.")]
            public string GrammarFiles { get; set; }

            [Option('n', "namespace", Required=false, HelpText="The namespace for all generated files.")]
            public string DefaultNamespace { get; set; }

            [Option('o', "output-directory", Required=false, HelpText="The output directory for the project.")]
            public string OutputDirectory { get; set; }

            [Option('p', "package", Required=false, HelpText="PackageReference's to include, in name/version pairs.")]
            public string Packages { get; set; }

            [Option('x', "profile", Required = false, Default = false, HelpText = "Add in Antlr profiling code.")]
            public bool Profiling { get; set; }
            
            [Option('s', "start-rule", Required=false, HelpText="Start rule name.")]
            public string StartRule { get; set; }

            [Option('t', "target", Required = false, Default=TargetType.CSharp, HelpText = "The target language for the project.")]
            public TargetType Target { get; set; }
        }

        static void Main(string[] args)
        {
            try
            {
                MainInternal(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        static int MainInternal(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            List<string> grammarFiles = new List<string>();
            string @namespace = null;
            Dictionary<string, string> packages = new Dictionary<string, string>();
            string startRule = null;
            string outputDirectory = null;
            TargetType target = TargetType.CSharp;
            EncodingType encoding = GetOperatingSystem();
            bool? case_fold = null;
            bool antlr4cs = false;
            bool profiling = false;
            bool stop = false;
            result.WithNotParsed(o => { stop = true; });
            if (stop) return 0;
            result.WithParsed(o =>
            {
                target = o.Target;
                profiling = o.Profiling;
                antlr4cs = o.Antlr4cs;
                encoding = o.Encoding == EncodingType.Native ? GetOperatingSystem() : o.Encoding;
                if (antlr4cs) @namespace = "Test";
                if (o.CaseFold != null) case_fold = o.CaseFold;
                if (o.DefaultNamespace != null) @namespace = o.DefaultNamespace;
                if (o.GrammarFiles != null) grammarFiles = o.GrammarFiles?.Split(",").ToList();
                else
                {
                    var g = new Domemtech.Globbing.Glob();
                    var files = g.Contents("./*.g4");
                    foreach (var f in files)
                    {
                        if (f is System.IO.FileInfo fi)
                        {
                            grammarFiles.Add(fi.Name);
                        }
                    }
                }
                if (o.StartRule != null) startRule = o.StartRule;
                else
                {
                    if (o.GrammarFiles == null && grammarFiles.Count == 0)
                    {
                        startRule = "file";
                    }
                    else startRule = null;
                }
                if (o.OutputDirectory != null) outputDirectory = o.OutputDirectory;
                else
                {
                    outputDirectory = "Generated/";
                }
            });

            if ((grammarFiles != null && grammarFiles.Count > 0) && startRule == null)
            {
                throw new Exception("You must specify a start rule.");
            }

            var path = Environment.CurrentDirectory;
            path = path + Path.DirectorySeparatorChar + outputDirectory;
            outputDirectory = System.IO.Path.GetFullPath(path);
            try
            {
                // Create a directory containing a C# project with grammars.
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception)
            {
                throw;
            }

            AddBuildFile(encoding, antlr4cs, target, @namespace, grammarFiles, outputDirectory);
            AddGrammars(encoding, grammarFiles, outputDirectory);
            AddMain(encoding, profiling, antlr4cs, case_fold, target, grammarFiles, @namespace, startRule, outputDirectory);
            AddErrorListener(encoding, antlr4cs, target, @namespace, outputDirectory);
            AddCaseFold(encoding, case_fold, target, @namespace, outputDirectory);
            AddTreeOutput(encoding, antlr4cs, target, @namespace, outputDirectory);
            AddSourceFiles(encoding, antlr4cs, target, @namespace, outputDirectory);

            return 0;
        }

        private static void AddCaseFold(EncodingType encoding, bool? case_fold, TargetType target, string @namespace, string outputDirectory)
        {
            if (case_fold == null) return;
            StringBuilder sb = new StringBuilder();
            if (target == TargetType.CSharp)
            {
                sb.AppendLine(@"
// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("namespace " + @namespace + @"
{");
                sb.Append(@"
/* Copyright (c) 2012-2017 The ANTLR Project. All rights reserved.
 * Use of this file is governed by the BSD 3-clause license that
 * can be found in the LICENSE.txt file in the project root.
 */
using System;
using Antlr4.Runtime.Misc;

namespace Antlr4.Runtime
{
    /// <summary>
    /// This class supports case-insensitive lexing by wrapping an existing
    /// <see cref=""ICharStream""/> and forcing the lexer to see either upper or
    /// lowercase characters. Grammar literals should then be either upper or
    /// lower case such as 'BEGIN' or 'begin'. The text of the character
    /// stream is unaffected. Example: input 'BeGiN' would match lexer rule
    /// 'BEGIN' if constructor parameter upper=true but getText() would return
    /// 'BeGiN'.
    /// </summary>
    public class CaseChangingCharStream : ICharStream
    {
        private ICharStream stream;
        private bool upper;

        /// <summary>
        /// Constructs a new CaseChangingCharStream wrapping the given <paramref name=""stream""/> forcing
        /// all characters to upper case or lower case.
        /// </summary>
        /// <param name=""stream"">The stream to wrap.</param>
        /// <param name=""upper"">If true force each symbol to upper case, otherwise force to lower.</param>
        public CaseChangingCharStream(ICharStream stream, bool upper)
        {
            this.stream = stream;
            this.upper = upper;
        }

        public int Index
        {
            get
            {
                return stream.Index;
            }
        }

        public int Size
        {
            get
            {
                return stream.Size;
            }
        }

        public string SourceName
        {
            get
            {
                return stream.SourceName;
            }
        }

        public void Consume()
        {
            stream.Consume();
        }

        [return: NotNull]
        public string GetText(Interval interval)
        {
            return stream.GetText(interval);
        }

        public int LA(int i)
        {
            int c = stream.LA(i);

            if (c <= 0)
            {
                return c;
            }

            char o = (char)c;

            if (upper)
            {
                return (int)char.ToUpperInvariant(o);
            }

            return (int)char.ToLowerInvariant(o);
        }

        public int Mark()
        {
            return stream.Mark();
        }

        public void Release(int marker)
        {
            stream.Release(marker);
        }

        public void Seek(int index)
        {
            stream.Seek(index);
        }
    }
}");
                if (@namespace != null) sb.AppendLine("}");
                string fn = outputDirectory + "CaseChangingCharStream.cs";
                System.IO.File.WriteAllText(fn, sb.ToString());
            }
            else if (target == TargetType.Java)
            {
                sb.Append(@"
// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version + @"
package " + @namespace + @";

import org.antlr.v4.runtime.*;

public class ErrorListener extends ConsoleErrorListener
{
    public boolean had_error = false;
    
    @Override
    public void syntaxError(Recognizer<?, ?> recognizer,
        Object offendingSymbol,
        int line,
        int charPositionInLine,
        String msg,
        RecognitionException e)
    {
        had_error = true;
        super.syntaxError(recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}
");
                string fn = outputDirectory + "ErrorListener.java";
                System.IO.File.WriteAllText(fn, sb.ToString());
            }
        }

        static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private static void AddSourceFiles(EncodingType encoding, bool antlr4cs, TargetType target, string @namespace, string outputDirectory)
        {
            var file_pattern = target switch
            {
                TargetType.CSharp => "./CSharp/**/*.cs",
                TargetType.Java => "./Java/**/*.java",
                TargetType.JavaScript => "./JavaScript/**/*.js",
                _ => throw new NotImplementedException(),
            };
            DirectoryInfo dir = new DirectoryInfo("./CSharp");
            string sourceDirName = target switch
            {
                TargetType.CSharp => "CSharp",
                TargetType.Java => "Java",
                TargetType.JavaScript => "JavaScript",
                _ => throw new NotImplementedException(),
            };
            if (dir.Exists)
                DirectoryCopy(sourceDirName, outputDirectory, true);
            // Copy all files to generated directory.
            //var g = new Domemtech.Globbing.Glob();
            //var files = g.Contents(file_pattern);
            //foreach (var f in files)
            //{
            //    if (f is System.IO.FileInfo fi)
            //    {
            //        var from = fi.FullName;
            //        var dir = fi.DirectoryName;
            //        if (dir.StartsWith(outputDirectory)) continue;
            //        var n = System.IO.Path.GetFileName(from);
            //        var fn = outputDirectory + n;
            //        var i = System.IO.File.ReadAllText(from);
            //        System.IO.File.WriteAllText(fn, i);
            //    }
            //}
        }

        private static void AddGrammars(EncodingType encoding, List<string> grammarFiles, string outputDirectory)
        {
            if (grammarFiles != null && grammarFiles.Any())
            {
                // Copy all files to generated directory.
                foreach (var g in grammarFiles)
                {
                    var i = System.IO.File.ReadAllText(g);
                    var n = System.IO.Path.GetFileName(g);
                    var fn = outputDirectory + n;
                    System.IO.File.WriteAllText(fn, Localize(encoding, i));
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(@"
// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version + @"

grammar Arithmetic;

file : expression (SEMI expression)* EOF;
expression : expression POW expression | expression (TIMES | DIV) expression | expression (PLUS | MINUS) expression | LPAREN expression RPAREN | (PLUS | MINUS)* atom ;
atom : scientific | variable ;
scientific : SCIENTIFIC_NUMBER ;
variable : VARIABLE ;

VARIABLE : VALID_ID_START VALID_ID_CHAR* ;
SCIENTIFIC_NUMBER : NUMBER (E SIGN? UNSIGNED_INTEGER)? ;
LPAREN : '(' ;
RPAREN : ')' ;
PLUS : '+' ;
MINUS : '-' ;
TIMES : '*' ;
DIV : '/' ;
GT : '>' ;
LT : '<' ;
EQ : '=' ;
POINT : '.' ;
POW : '^' ;
SEMI : ';' ;
WS : [ \r\n\t] + -> channel(HIDDEN) ;

fragment VALID_ID_START : ('a' .. 'z') | ('A' .. 'Z') | '_' ;
fragment VALID_ID_CHAR : VALID_ID_START | ('0' .. '9') ;
fragment NUMBER : ('0' .. '9') + ('.' ('0' .. '9') +)? ;
fragment UNSIGNED_INTEGER : ('0' .. '9')+ ;
fragment E : 'E' | 'e' ;
fragment SIGN : ('+' | '-') ;
");
                var fn = outputDirectory + "Arithmetic.g4";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
        }

        private static void AddBuildFile(EncodingType encoding, bool antlr4cs, TargetType target, string @namespace, List<string> grammarFiles, string outputDirectory)
        {
            StringBuilder sb = new StringBuilder();
            if (target == TargetType.CSharp)
            {
                sb.AppendLine(@"<!-- Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version + @" -->
<Project Sdk=""Microsoft.NET.Sdk"" >
  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  ");

                if (!antlr4cs)
                {
                    sb.AppendLine("<ItemGroup>");
                    if (grammarFiles != null && grammarFiles.Any())
                    {
                        foreach (var grammar in grammarFiles)
                        {
                            if (@namespace == null)
                                sb.AppendLine("<Antlr4 Include=\"" + Path.GetFileName(grammar) + "\" />");
                            else
                            {
                                sb.AppendLine("<Antlr4 Include=\"" + Path.GetFileName(grammar) + "\">");
                                sb.AppendLine("<Package>" + @namespace + "</Package>");
                                sb.AppendLine("</Antlr4>");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine(@"<Antlr4 Include=""Arithmetic.g4"" />");
                    }
                    sb.AppendLine("</ItemGroup>");
                }

                if (!antlr4cs)
                {
                    sb.AppendLine(@"
  <ItemGroup>
    <PackageReference Include=""Antlr4.Runtime.Standard"" Version =""4.9.1"" />
    <PackageReference Include=""Antlr4BuildTasks"" Version = ""8.13"" PrivateAssets=""all"" />
  </ItemGroup>");
                }
                else
                {
                    sb.AppendLine(@"
  <ItemGroup>
    <PackageReference Include=""Antlr4"" Version=""4.6.6"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Antlr4.Runtime"" Version=""4.6.6"" />
  </ItemGroup>");
                }
                    sb.AppendLine(@"
  <PropertyGroup>
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"" >
    <NoWarn>1701;1702;3021</NoWarn>
  </PropertyGroup>
</Project>");
                var fn = outputDirectory + "Test.csproj";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.Java)
            {
                sb.AppendLine(@"#!/bin/sh
java -jar ~/Downloads/antlr-4.9.1-complete.jar *.g4
javac -classpath ~/Downloads/antlr-4.9.1-complete.jar:.. *.java
");
                var fn = outputDirectory + "build.sh";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
                sb.AppendLine(@"#!/bin/sh
java -classpath ~/Downloads/antlr-4.9.1-complete.jar:. Program
");
                fn = outputDirectory + "run.sh";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.JavaScript)
            {
                sb.AppendLine(@"#!/bin/sh
rm -rf node_modules
rm -f package-lock.json
npm i antlr4@4.9.1
npm i typescript-string-operations@1.4.1
npm i fs-extra
java -jar ~/Downloads/antlr-4.9.1-complete.jar -Dlanguage=JavaScript *.g4
cat - << EOF
To run:
echo string | node.exe program.js -tree
node.exe program.js -tree -input string
node.exe program.js -tree -file path
EOF
");
                var fn = outputDirectory + "build.sh";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
                sb = new StringBuilder();
                sb.AppendLine(@"
{
  ""name"": ""i"",
  ""version"": ""1.0.0"",
  ""description"": """",
  ""main"": ""index.js"",
  ""scripts"": {
    ""test"": ""echo \""Error: no test specified\"" && exit 1""
  },
  ""author"": """",
  ""license"": ""ISC"",
  ""dependencies"": {
    ""antlr4"": ""^4.9.1"",
    ""fs-extra"": ""^9.1.0"",
    ""typescript-string-operations"": ""^1.4.1""
  },
  ""type"": ""module""
}
");
                fn = outputDirectory + "package.json";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
        }

        private static void AddTreeOutput(EncodingType encoding, bool antlr4cs, TargetType target, string @namespace, string outputDirectory)
        {
            StringBuilder sb = new StringBuilder();
            if (target == TargetType.CSharp && !antlr4cs)
            {
                sb.AppendLine(@"
// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("namespace " + @namespace + @"
{");
                sb.Append(@"
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class TreeOutput
{
    private static int changed = 0;
    private static bool first_time = true;

    public static StringBuilder OutputTree(IParseTree tree, Lexer lexer, Parser parser, CommonTokenStream stream)
    {
        changed = 0;
        first_time = true;
        var sb = new StringBuilder();
        ParenthesizedAST(tree, sb, lexer, parser, stream);
        return sb;
    }

    private static void ParenthesizedAST(IParseTree tree, StringBuilder sb, Lexer lexer, Parser parser, CommonTokenStream stream, int level = 0)
    {
        if (tree as TerminalNodeImpl != null)
        {
            TerminalNodeImpl tok = tree as TerminalNodeImpl;
            Interval interval = tok.SourceInterval;
            IList<IToken> inter = null;
            if (tok.Symbol.TokenIndex >= 0)
                inter = stream?.GetHiddenTokensToLeft(tok.Symbol.TokenIndex);
            if (inter != null)
                foreach (var t in inter)
                {
                    var ty = tok.Symbol.Type;
                    var name = lexer.Vocabulary.GetSymbolicName(ty);
                    StartLine(sb, level);
                    sb.AppendLine(""("" + name + "" text = "" + PerformEscapes(t.Text) + "" "" + lexer.ChannelNames[t.Channel]);
                }
            {
                var ty = tok.Symbol.Type;
                var name = lexer.Vocabulary.GetSymbolicName(ty);
                StartLine(sb, level);
                sb.AppendLine(""( "" + name + "" i ="" + tree.SourceInterval.a
                    + "" txt ="" + PerformEscapes(tree.GetText())
                    + "" tt ="" + tok.Symbol.Type
                    + "" "" + lexer.ChannelNames[tok.Symbol.Channel]);
            }
        }
        else
        {
            var x = tree as RuleContext;
            var ri = x.RuleIndex;
            var name = parser.RuleNames[ri];
            StartLine(sb, level);
            sb.Append(""( "" + name);
            sb.AppendLine();
        }
        for (int i = 0; i<tree.ChildCount; ++i)
        {
            var c = tree.GetChild(i);
            ParenthesizedAST(c, sb, lexer, parser, stream, level + 1);
        }
        if (level == 0)
        {
            for (int k = 0; k < 1 + changed - level; ++k) sb.Append("") "");
            sb.AppendLine();
            changed = 0;
        }
    }

    private static void StartLine(StringBuilder sb, int level = 0)
    {
        if (changed - level >= 0)
        {
            if (!first_time)
            {
                for (int j = 0; j < level; ++j) sb.Append(""  "");
                for (int k = 0; k < 1 + changed - level; ++k) sb.Append("") "");
                sb.AppendLine();
            }
            changed = 0;
            first_time = false;
        }
        changed = level;
        for (int j = 0; j < level; ++j) sb.Append(""  "");
    }

    private static string ToLiteral(string input)
    {
        using (var writer = new StringWriter())
        {
            var literal = input;
            literal = literal.Replace(""\\"", ""\\\\"");
            return literal;
        }
    }

    public static string PerformEscapes(string s)
    {
        StringBuilder new_s = new StringBuilder();
        new_s.Append(ToLiteral(s));
        return new_s.ToString();
    }
}
");
                if (@namespace != null) sb.AppendLine("}");
                string fn = outputDirectory + "TreeOutput.cs";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.Java)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("package " + @namespace + @";");
                sb.Append(@"

import java.util.*;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.Arrays;
import java.util.HashMap;
import org.antlr.v4.gui.TreeViewer;
import org.antlr.v4.runtime.ANTLRInputStream;
import org.antlr.v4.runtime.Lexer;
import org.antlr.v4.runtime.CharStream;
import org.antlr.v4.runtime.CharStreams;
import org.antlr.v4.runtime.CodePointCharStream;
import org.antlr.v4.runtime.CommonTokenStream;
import org.antlr.v4.runtime.tree.ParseTree;
import org.antlr.v4.runtime.tree.TerminalNodeImpl;
import org.antlr.v4.runtime.*;
import java.util.regex.Pattern;
import java.io.File;
import java.lang.reflect.*;

public class TreeOutput
{
    private static int changed = 0;
    private static boolean first_time = true;

    public static StringBuilder OutputTree(ParseTree tree, CommonTokenStream stream)
    {
        var sb = new StringBuilder();
        ParenthesizedAST(tree, sb, stream, 0);
        return sb;
    }

    private static void ParenthesizedAST(ParseTree tree, StringBuilder sb, CommonTokenStream stream, int level)
    {
        // Antlr always names a non-terminal with first letter lowercase,
        // but renames it when creating the type in C#. So, remove the prefix,
        // lowercase the first letter, and remove the trailing ""Context"" part of
        // the name. Saves big time on output!
        if (tree instanceof TerminalNodeImpl)
        {
            TerminalNodeImpl tok = (TerminalNodeImpl)tree;
            var interval = tok.getSourceInterval();
            var inter = stream.getHiddenTokensToLeft(tok.symbol.getTokenIndex());
            if (inter != null)
                for (var t : inter)
                {
                    StartLine(sb, tree, stream, level);
                    sb.append(""( HIDDEN text="" + PerformEscapes(t.getText()));
                    sb.append(System.lineSeparator());
                }
                StartLine(sb, tree, stream, level);
                Lexer xxx = (Lexer)stream.getTokenSource();
                String[] yyy = xxx.getChannelNames();
                sb.append(""( "" + yyy[tok.getSymbol().getChannel()]
                    + "" i ="" + tree.getSourceInterval().a
                    + "" txt ="" + PerformEscapes(tree.getText())
                    + "" tt ="" + tok.getSymbol().getType());
                    sb.append(System.lineSeparator());
            }
            else
            {
                var fixed_name = tree.getClass().getName().toString();
                fixed_name = fixed_name.replaceAll("" ^[^$]*[$]"", """");
                fixed_name = fixed_name.substring(0, fixed_name.length() - ""Context"".length());
                fixed_name = Character.toString(fixed_name.charAt(0)).toLowerCase()
                        + fixed_name.substring(1);
                StartLine(sb, tree, stream, level);
                sb.append(""( "" + fixed_name);
                sb.append(System.lineSeparator());
            }
            for (int i = 0; i < tree.getChildCount(); ++i)
            {
                var c = tree.getChild(i);
                ParenthesizedAST(c, sb, stream, level + 1);
            }
            if (level == 0)
            {
                for (int k = 0; k < 1 + changed - level; ++k) sb.append("") "");
                sb.append(System.lineSeparator());
                changed = 0;
            }
        }

        private static void StartLine(StringBuilder sb, ParseTree tree, CommonTokenStream stream, int level)
        {
            if (changed - level >= 0)
            {
                if (!first_time)
                {
                    for (int j = 0; j < level; ++j) sb.append(""  "");
                    for (int k = 0; k < 1 + changed - level; ++k) sb.append("") "");
                    sb.append(System.lineSeparator());
                }
                changed = 0;
                first_time = false;
            }
            changed = level;
            for (int j = 0; j < level; ++j) sb.append(""  "");
        }

        private static String ToLiteral(String input)
        {
            var literal = input;
            literal = literal.replace(""\\"", ""\\\\"");
            literal = input.replace(""\b"", ""\\b"");
            literal = literal.replace(""\n"", ""\\n"");
            literal = literal.replace(""\t"", ""\\t"");
            literal = literal.replace(""\r"", ""\\r"");
            literal = literal.replace(""\f"", ""\\f"");
            literal = literal.replace(""\"""", ""\\\"""");
            literal = literal.replace(String.format(""\"" +{0}\t\"""", ""\n""), """");
            return literal;
        }

        public static String PerformEscapes(String s)
        {
            StringBuilder new_s = new StringBuilder();
            new_s.append(ToLiteral(s));
            return new_s.toString();
        }
    }
");

                string fn = outputDirectory + "TreeOutput.java";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.JavaScript)
            {
            }
        }

        private static void AddErrorListener(EncodingType encoding, bool antlr4cs, TargetType target, string @namespace, string outputDirectory)
        {
            StringBuilder sb = new StringBuilder();
            if (target == TargetType.CSharp && !antlr4cs)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("namespace " + @namespace + @"
{");
                sb.Append(@"
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ErrorListener<S> : ConsoleErrorListener<S>
{
    public bool had_error;

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, S offendingSymbol, int line,
        int col, string msg, RecognitionException e)
    {
        had_error = true;
        base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e);
    }
}
");
                if (@namespace != null) sb.AppendLine("}");
                string fn = outputDirectory + "ErrorListener.cs";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.Java)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("package " + @namespace + @";");
                sb.Append(@"
import org.antlr.v4.runtime.*;

public class ErrorListener extends ConsoleErrorListener
{
    public boolean had_error = false;
    
    @Override
    public void syntaxError(Recognizer<?, ?> recognizer,
        Object offendingSymbol,
        int line,
        int charPositionInLine,
        String msg,
        RecognitionException e)
    {
        had_error = true;
        super.syntaxError(recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}
");
                string fn = outputDirectory + "ErrorListener.java";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.JavaScript)
            {
            }
        }

        private static void AddMain(EncodingType encoding, bool profiling, bool antlr4cs, bool? case_fold, TargetType target, List<string> grammarFiles, string @namespace, string startRule, string outputDirectory)
        {
            StringBuilder sb = new StringBuilder();
            var lexer_name = "";
            var parser_name = "";
            // lexer and parser are set if the grammar is partitioned.
            // rest is set if there are grammar is combined.
            var lexer = grammarFiles?.Where(d => d.EndsWith("Lexer.g4")).ToList();
            var parser = grammarFiles?.Where(d => d.EndsWith("Parser.g4")).ToList();
            var rest = grammarFiles?.Where(d => !d.EndsWith("Parser.g4") && !d.EndsWith("Lexer.g4")).ToList();
            if ((rest == null || rest.Count == 0)
                && (lexer == null || lexer.Count == 0)
                && (parser == null || parser.Count == 0))
            {
                // I have no clue what your grammars are.
                lexer_name = "ArithmeticLexer";
                parser_name = "ArithmeticParser";
            }
            else if (lexer.Count == 1)
            {
                lexer_name = Path.GetFileName(lexer.First().Replace(".g4", ""));
                parser_name = Path.GetFileName(parser.First().Replace(".g4", ""));
            }
            else if (lexer.Count == 0)
            {
                // Combined.
                if (rest.Count == 1)
                {
                    var combined_name = Path.GetFileName(rest.First()).Replace(".g4", "");
                    lexer_name = combined_name + "Lexer";
                    parser_name = combined_name + "Parser";
                }
            }
            if (target == TargetType.CSharp)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("namespace " + @namespace + @"
{");
                sb.Append(@"
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

public class Program
{
    static void Main(string[] args)
    {
        bool show_tree = false;
        bool show_tokens = false;
        string file_name = null;
        string input = null;
        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i].Equals(""-tokens""))
            {
                show_tokens = true;
                continue;
            }
            else if (args[i].Equals(""-tree""))
            {
                show_tree = true;
                continue;
            }
            else if (args[i].Equals(""-input""))
                input = args[i];
            else if (args[i].Equals(""-file""))
                file_name = args[++i];
        }
        ICharStream str = null;
        if (input == null && file_name == null)
        {
            StringBuilder sb = new StringBuilder();
            int ch;
            while ((ch = System.Console.Read()) != -1)
            {
                sb.Append((char)ch);
            }
            input = sb.ToString();
            ");
                if (!antlr4cs)
                    sb.Append(
            @"str = CharStreams.fromString(input);");
                else
                    sb.Append(
            @"str = new Antlr4.Runtime.AntlrInputStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(input ?? """")));");
                sb.Append(@"
        } else if (input != null)
        {
            ");
                if (!antlr4cs)
                    sb.Append(
            @"str = CharStreams.fromString(input);");
                else
                    sb.Append(
            @"str = new Antlr4.Runtime.AntlrInputStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(input ?? """")));");
                sb.Append(@"
        } else if (file_name != null)
        {
            ");
                if (!antlr4cs)
                    sb.Append(
            @"str = CharStreams.fromPath(file_name);");
                else
                    sb.Append(
            @"FileStream fs = new FileStream(file_name, FileMode.Open);
                str = new Antlr4.Runtime.AntlrInputStream(fs);");
                sb.Append(@"
        }
        ");
                if (case_fold != null)
                {
                    sb.Append(@"str = new CaseChangingCharStream(str, "
                        + ((bool)case_fold ? "true" : "false") + @");
        ");
                }
                sb.Append(@"var lexer = new " + lexer_name + @"(str);
        if (show_tokens)
        {
            StringBuilder new_s = new StringBuilder();
            for (int i = 0; ; ++i)
            {
                var ro_token = lexer.NextToken();
                var token = (CommonToken)ro_token;
                token.TokenIndex = i;
                new_s.AppendLine(token.ToString());
                if (token.Type == Antlr4.Runtime.TokenConstants." + (antlr4cs ? "Eof" : "EOF") + @")
                    break;
            }
            System.Console.Error.WriteLine(new_s.ToString());
        }
        lexer.Reset();
        var tokens = new CommonTokenStream(lexer);
        var parser = new " + parser_name + @"(tokens);
        ");
                if (!antlr4cs)
                {
                    sb.Append(@"var listener_lexer = new ErrorListener<int>();
        var listener_parser = new ErrorListener<IToken>();
        lexer.AddErrorListener(listener_lexer);
        parser.AddErrorListener(listener_parser);");
                    if (profiling)
                    {
                        sb.Append(@"
        parser.Profile = true;");
                    }
                    sb.Append(@"
        var tree = parser." + startRule + @"();
        if (listener_lexer.had_error || listener_parser.had_error)
        {
            System.Console.Error.WriteLine(""parse failed."");
        }
        else
        {
            System.Console.Error.WriteLine(""parse succeeded."");
        }
        if (show_tree)
        {
            System.Console.Error.WriteLine(tree.ToStringTree());
        }");
                    if (profiling)
                    {
                        sb.Append(@"System.Console.Out.WriteLine(String.Join("", "", parser.ParseInfo.getDecisionInfo().Select(d => d.ToString())));
        ");
                    }
                    sb.Append(@"
        System.Environment.Exit(listener_lexer.had_error || listener_parser.had_error ? 1 : 0);");
                }
                else
                {
                    if (profiling)
                    {
                        sb.Append(@"
        parser.Profile = true;");
                    }
                    sb.Append(@"
        var tree = parser." + startRule + @"();
        if (parser.NumberOfSyntaxErrors != 0)
        {
            System.Console.Error.WriteLine(""parse failed."");
        }
        else
        {
            System.Console.Error.WriteLine(""parse succeeded."");
        }
        if (show_tree)
        {
            System.Console.Error.WriteLine(tree.ToStringTree());
        }
");
                    if (profiling)
                    {
                        sb.Append(@"System.Console.Out.WriteLine(String.Join("", "", parser.ParseInfo.getDecisionInfo().Select(d => d.ToString())));
        ");
                    }
                    sb.Append(@"
        System.Environment.Exit(parser.NumberOfSyntaxErrors != 0 ? 1 : 0);");
                }
                sb.Append(@"
    }
}
");
                if (@namespace != null) sb.AppendLine("}");
                // Test to find an appropriate file name to place this into.
                string fn = outputDirectory + "Program.cs";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.Java)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                if (@namespace != null) sb.AppendLine("package " + @namespace + @";");
                sb.Append(@"

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.Arrays;
import java.util.Date;
import java.util.HashMap;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.tree.ParseTree;

public class Program {
    public static void main(String[] args) throws  FileNotFoundException, IOException
    {
        boolean show_tree = false;
        boolean show_tokens = false;
        String file_name = null;
        String input = null;
        for (int i = 0; i < args.length; ++i)
        {
            if (args[i].equals(""-tokens""))
            {
                show_tokens = true;
                continue;
            }
            else if (args[i].equals(""-tree""))
            {
                show_tree = true;
                continue;
            }
            else if (args[i].equals(""-input""))
                input = args[i];
            else if (args[i].equals(""-file""))
                file_name = args[++i];
        }
        CharStream str = null;
        if (input == null && file_name == null)
        {
            StringBuilder sb = new StringBuilder();
            int ch;
            while ((ch = System.in.read()) != -1)
            {
                sb.append((char)ch);
            }
            input = sb.toString();
            str = CharStreams.fromString(input);
        } else if (input != null)
        {
            str = CharStreams.fromString(input);
        } else if (file_name != null)
        {
            str = CharStreams.fromFileName(file_name);
        }
        " + lexer_name + " lexer = new " + lexer_name + @"(str);
        if (show_tokens)
        {
            StringBuilder new_s = new StringBuilder();
            for (int i = 0; ; ++i)
            {
                var ro_token = lexer.nextToken();
                var token = (CommonToken)ro_token;
                token.setTokenIndex(i);
                new_s.append(token.toString());
                new_s.append(System.getProperty(""line.separator""));
                if (token.getType() == IntStream.EOF)
                    break;
            }
            System.out.println(new_s.toString());
        }
        lexer.reset();
        var tokens = new CommonTokenStream(lexer);
        " + parser_name + " parser = new " + parser_name + @"(tokens);
        ErrorListener lexer_listener = new ErrorListener();
        ErrorListener listener = new ErrorListener();
        parser.removeParseListeners();
        parser.addErrorListener(listener);
        lexer.addErrorListener(lexer_listener);");
                if (profiling)
                {
                    sb.Append(@"
        parser.setProfile(true);");
                }
                sb.Append(@"
        ParseTree tree = parser." + startRule + @"();
        if (listener.had_error || lexer_listener.had_error)
            System.out.println(""error in parse."");
        else
            System.out.println(""parse completed."");
        if (show_tree)
        {
            System.out.println(tree.toStringTree());
        }
        ");
                if (profiling)
                {
                    sb.Append(@"ProfileParser(parser);
        ");
                }
                sb.Append(@"java.lang.System.exit(listener.had_error || lexer_listener.had_error ? 1 : 0);
    }

    private static void ProfileParser(Parser parser)
    {
        var atn = parser.getATN();
        var di_list = parser.getParseInfo().getDecisionInfo();
        for (int i = 0; i < di_list.length; ++i)
        {
            var decisionInfo = di_list[i];
            if (i != 0) System.out.print("", "");
            System.out.print(decisionInfo.toString());
        }
        System.out.println();
    }
}
");
                sb.Append(@"System.Console.Out.WriteLine(String.Join("", "", parser.ParseInfo.getDecisionInfo().Select(d => d.ToString())));
        ");

                // Test to find an appropriate file name to place this into.
                string fn = outputDirectory + "Program.java";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
            else if (target == TargetType.JavaScript)
            {
                sb.AppendLine(@"// Template generated code from Antlr4BuildTasks.dotnet-antlr v " + version);
                sb.Append(@"
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const antlr4 = require('antlr4');
import " + lexer_name + @" from './" + lexer_name + @".js';
import " + parser_name + @" from './" + parser_name + @".js';
const strops = require('typescript-string-operations');
let fs = require('fs-extra')

function getChar() {
	let buffer = Buffer.alloc(1);
	var xx = fs.readSync(0, buffer, 0, 1);
	if (buffer[0] == 0x0a) {
		return '';
	}
    return buffer.toString('utf8');
}

class MyErrorListener extends antlr4.error.ErrorListener {
	syntaxError(recognizer, offendingSymbol, line, column, msg, err) {
		num_errors++;
		console.error(`${offendingSymbol} line ${line}, col ${column}: ${msg}`);
	}
}

var show_tokens = false;
var show_tree = false;
var input = null;
var file_name = null;
for (let i = 2; i < process.argv.length; ++i)
{
    switch (process.argv[i]) {
        case '-tokens':
            var show_tokens = true;
            break;
        case '-tree':
            var show_tree = true;
            break;
        case '-input':
            var input = process.argv[++i];
            break;
        case '-file':
            var file_name = process.argv[++i];
            break;
        default:
            console.log('unknown '.concat(process.argv[i]));
    }
}
var str = null;
if (input == null && file_name == null)
{
    var sb = new strops.StringBuilder();
    var ch;
    while ((ch = getChar()) != '')
    {
        sb.Append(ch);
    }
    var input = sb.ToString();
    str = antlr4.CharStreams.fromString(input);
} else if (input != null)
{
    str = antlr4.CharStreams.fromString(input);
} else if (file_name != null)
{
    str = antlr4.CharStreams.fromPathSync(file_name, 'utf8');
}
var num_errors = 0;
const lexer = new " + lexer_name + @"(str);
lexer.strictMode = false;
const tokens = new antlr4.CommonTokenStream(lexer);
const parser = new " + parser_name + @"(tokens);
lexer.removeErrorListeners();
parser.removeErrorListeners();
parser.addErrorListener(new MyErrorListener());
lexer.addErrorListener(new MyErrorListener());
const tree = parser." + startRule + @"();
if (show_tree)
{
    console.log(tree.toStringTree(parser.ruleNames));
}
if (num_errors > 0)
{
    console.log('error in parse.');
    process.exitCode = 1;
}
else
{
    console.log('parse completed.');
    process.exitCode = 0;
}
");

                // Test to find an appropriate file name to place this into.
                string fn = outputDirectory + "program.js";
                System.IO.File.WriteAllText(fn, Localize(encoding, sb.ToString()));
            }
        }

        static string Localize(EncodingType encoding, string code)
        {
            var is_win = code.Contains("\r\n");
            var is_mac = code.Contains("\n\r");
            var is_uni = code.Contains("\n") && !(is_win || is_mac);
            if (encoding == EncodingType.Windows)
            {
                if (is_win) return code;
                else if (is_mac) return code.Replace("\n\r", "\r\n");
                else if (is_uni) return code.Replace("\n", "\r\n");
                else return code;
            }
            if (encoding == EncodingType.Mac)
            {
                if (is_win) return code.Replace("\r\n", "\n\r");
                else if (is_mac) return code;
                else if (is_uni) return code.Replace("\n", "\n\r");
                else return code;
            }
            if (encoding == EncodingType.Unix)
            {
                if (is_win) return code.Replace("\r\n", "\n");
                else if (is_mac) return code.Replace("\n\r", "\n");
                else if (is_uni) return code;
                else return code;
            }
            return code;
        }
    }
}