# Antlr4BuildTasks

[![Build](https://github.com/kaby76/Antlr4BuildTasks/workflows/.NET/badge.svg)](https://github.com/kaby76/Antlr4BuildTasks/actions?query=workflow%3A.NET)

This package is a third-party build tool for C# programs with Antlr4 grammars
using the "official" Antl4.Runtime.Standard package.
It is based on Harwell's excellent [Antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs),
which is published in NuGet as the [Antlr4.CodeGenerator](https://www.nuget.org/packages/Antlr4.CodeGenerator/)
package. Although Antlr4.CodeGenerator is fine, it is several versions behind the current
Antlr4 tool and runtime, and as far as I know, is not being maintained. Further, it is not compatible
with Antlr4.Runtime.Standard, which requires the parser to be generated by the Antlr4 Java tool.

This package is contains the build rules for compiling .g4's into parser code, a complete JRE
(version 11) and a copy of the Antlr4.Runtime.Standard assembly for a complete, seemless build
for Antlr4 grammars to C# at compilation time. Note, it does not contain a package code generator
that is equivalent to the Antlr4, which one would use to generate a parser on-the-fly in a C# program.
But, most people do not require this feature.

[Antlr4.Runtime.Standard](https://www.nuget.org/packages/Antlr4.Runtime.Standard/)
is the "official", maintained runtime for C#. It completely
replaces [Antlr4.Runtime](https://www.nuget.org/packages/Antlr4.Runtime/).

To use this package, add the Antlr4BuildTasks and Antlr4.Runtime.Standard packages
to your project. csproj file as shown below, otherwise you can use the "NuGet Package Manager":

    <ItemGroup>
        <PackageReference Include="Antlr4.Runtime.Standard" Version="4.10" />
        <PackageReference Include="Antlr4BuildTasks" Version="10.1">
          <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>
    
Then, you will need to tag each Antlr4 grammar file you want the Antlr tool to process. You can change the
"Build Action" property for the .g4 file from "None" to "ANTLR 4 grammar". Or, you can add the following lines
to your .csproj file for your "MyGrammar.g4" file:

    <ItemGroup>
        <Antlr4 Include="MyGrammar.g4" />
    </ItemGroup>

Note: `<PrivateAssets>all</PrivateAssets>` is added to the package reference
so that when you run `dotnet publish`, the Antlr4BuildTasks.dll is not included
in your app; it's only used to build your application.
Antlr4BuildTasks.dll is not currently digitally signed.
    
### Setting arguments to the Antlr tool

You can set the arguments to the Antlr Tool in Visual Studio by modifying the properties
for each grammar file. Or, you can modify the .csproj file to include the parameters you are
interested in setting:

    <ItemGroup>
        <Antlr4 Include="MyGrammar.g4">
            <Listener>false</Listener>
            <Visitor>false</Visitor>
            <GAtn>true</GAtn>
            <Package>foo</Package>
            <Error>true</Error>
        </Antlr4>
    </ItemGroup>


The &lt;Antlr4&gt; item supports the following parameters that are passed onto the Antlr tool:

* &lt;Listener&gt; -- A bool that specifies whether you want an
Antlr Listener class generated by the tool.
* &lt;Visitor&gt; -- A bool that specifies whether you want an
Antlr Visitor class generated by the tool.
* &lt;GAtn&gt; -- A bool that specifies whether you want
Antlr to generate the ATN Dot diagrams.
* &lt;Package&gt; -- An C# identifier that specifies the namespace to wrap
the generated classes in.
* &lt;Error&gt; -- Use this to specify whether you want the tool to
flag warnings as errors and stop a build.
* &lt;LibPath&gt; -- A string that specifies the path for token and grammar files
for the Antlr tool.
* &lt;Encoding&gt; -- A string that specifies the encoding of the input grammars.
* &lt;DOptions&gt; -- A list of &lt;option&gt;=&lt;value&gt;, passed to the Antlr tool. E.g.,
language=Java. Multiple options can be specified using a semi-colon separating each.

Antlr generates files that may produce a lot of compiler warnings. To ignore those,
add the following &lt;PropertyGroup&gt; to you .csproj file.

    <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
        <NoWarn>3021;1701;1702</NoWarn>
    </PropertyGroup>

### Conversion from Antlr4.CodeGenerator/Antlr4.Runtime to Antlr4BuildTasks/Antlr4.Runtime.Standard

In the .csproj file, change items from

     <ItemGroup>
         <Antlr4 Update="arithmetic.g4">
            etc etc etc
         </Antlr4>
     </ItemGroup>


to

    <ItemGroup>
        <Antlr4 Include="arithmetic.g4" />
    </ItemGroup>

Change package references from

    <ItemGroup>
        <PackageReference Include="Antlr4.CodeGenerator" Version="4.6.6">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Antlr4.Runtime" Version="4.6.6" />
    </ItemGroup>

to

    <ItemGroup>
        <PackageReference Include="Antlr4BuildTasks" Version="10.1" />
        <PackageReference Include="Antlr4.Runtime.Standard" Version="4.10" />
    </ItemGroup>


Antlr4BuildTasks examines PropertyGroup `AntlrProbePath`, a string of URI
separated by semicolon, to find the version
of the Antlr JAR file that you are looking for. Antlr4BuildTasks will look for a .jar
with version as specified in the Antlr4.Runtime.Standard PackageReference (e.g., 4.10).
_I recommend that you just use the PackageReference's defined above._

### Visual Studio IDE or Visual Studio Code integration

The package here has little to do with VS other than builds under VS can use Antlr4BuildTasks
for a seemless build. You will just need to add the package references and other items to
the .csproj file as outlined above.

If you are looking for a set of templates to create a console application that uses Antlr4,
then see [Antlr4Templates](https://github.com/kaby76/Antlr4Templates).

### Latest release v10.1 (13 Apr 2022)

* Update to Antlr 4.10. Fix JavaExec option.
