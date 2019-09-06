namespace Microsoft.FSharp.Build

open System

[<Class>]
type [<Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")>] Fsc =
    inherit Microsoft.Build.Framework.ToolTask
    static member Create : unit -> Fsc

    /// --baseaddress
    member BaseAddress : string with set
    /// --codepage <int>: Specify the codepage to use when opening source files
    member CodePage : string with set
    /// -g: Produce debug file. Disables optimizations if a -O flag is not given.
    member DebugSymbols : bool with set
    /// --debug <none/portable/embedded/pdbonly/full>: Emit debugging information
    member DebugType  : string with set

    member DelaySign : bool with set

    /// --nowarn <string>: Do not report the given specific warning.
    member DisabledWarnings : string with set

    /// --define <string>: Define the given conditional compilation symbol.
    member DefineConstants: Microsoft.Build.Framework.ITaskItem[] with set

    /// --doc <string>: Write the xmldoc of the assembly to the given file.
    member DocumentationFile : string with set

    member DotnetFscCompilerPath : string with set

    member EmbedAllSources : bool with set

    member Embed : string with set

    /// --generate-interface-file <string>:
    ///     Print the inferred interface of the
    ///     assembly to a file.
    member GenerateInterfaceFile : string with set

    /// --keyfile <string>:
    ///     Sign the assembly the given keypair file, as produced
    ///     by the .NET Framework SDK 'sn.exe' tool. This produces
    ///     an assembly with a strong name. This is only relevant if producing
    ///     an assembly to be shared amongst programs from different
    ///     directories, e.g. to be installed in the Global Assembly Cache.
    member KeyFile : string with set

    member LCID : string with set

    /// --noframework
    member NoFramework : bool with set

    /// --optimize
    member Optimize : bool with set

    /// --tailcalls
    member Tailcalls : bool with set

    /// REVIEW: decide whether to keep this, for now is handy way to deal with as-yet-unimplemented features
    member OtherFlags : string with set

    /// -o <string>: Name the output file.
    member OutputAssembly : string with set

    /// --pdb <string>:
    ///     Name the debug output file.
    member PdbFile : string with set

    /// --platform <string>: Limit which platforms this code can run on:
    ///            x86
    ///            x64
    ///            Itanium
    ///            anycpu
    ///            anycpu32bitpreferred
    member Platform : string with set

    /// indicator whether anycpu32bitpreferred is applicable or not
    member Prefer32Bit : bool with set

    member PreferredUILang : string with set

    member ProvideCommandLineArgs : bool with set

    member PublicSign : bool with set

    /// -r <string>: Reference an F# or .NET assembly.
    member References : Microsoft.Build.Framework.ITaskItem[] with set

    /// --lib
    member ReferencePath : string with set

    /// --resource <string>: Embed the specified managed resources (.resource).
    ///   Produce .resource files from .resx files using resgen.exe or resxc.exe.
    member Resources : Microsoft.Build.Framework.ITaskItem[] with set

    member SkipCompilerExecution : bool with set

    /// SourceLink
    member SourceLink : string with set

    /// source files
    member Sources : Microsoft.Build.Framework.ITaskItem[] with set

    member TargetProfile : string with set

    /// --target exe: Produce an executable with a console
    /// --target winexe: Produce an executable which does not have a
    ///      stdin/stdout/stderr
    /// --target library: Produce a DLL
    /// --target module: Produce a module that can be added to another assembly
    member TargetType : string with set

    member TreatWarningsAsErrors : bool with set

    /// For targeting other folders for "fsc.exe" (or ToolExe if different)
    member ToolPath : string with set

    /// When set to true, generate resource names in the same way as C# with root namespace and folder names
    member UseStandardResourceNames : bool with set

    /// --version-file <string>:
    member VersionFile : string with set

    /// For specifying a win32 native resource file (.res)
    member Win32ResourceFile : string with set

    /// For specifying a win32 manifest file
    member Win32ManifestFile : string with set

    /// For specifying the warning level (0-4)
    member WarningLevel : string with set

    member WarningsAsErrors : string with set

    member VisualStudioStyleErrors : bool with set

    member Utf8Output : bool with set

    member SubsystemVersion : string with set

    member HighEntropyVA : bool with set
