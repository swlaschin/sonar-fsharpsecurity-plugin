module Program

open System.IO
open Argu
open System.Reflection
open SonarAnalyzer.FSharp

open Serilog
open Serilog.Events
open OutputDiagnostics

// set up logging
do Serilog.Log.Logger <-
    Serilog.LoggerConfiguration()
        .Enrich.FromLogContext()
        .MinimumLevel.Debug()
        .WriteTo.Console(
            LogEventLevel.Verbose,
            //"{NewLine}{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
            "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

let logger = Serilog.Log.Logger

// where configuration files live
let SONARQUBE_CONF= @".sonarqube\conf"

// the target to write files to
let SONARQUBE_OUT = @".sonarqube\out"

// =========================================
// mini domain to show constrain allowable options
// =========================================

type AnalysisOptions =
    | File of filename:string
    | MultipleFiles of filenames:string list
    | Directory of dirname:string
    | Config of filename:string

type OutputTarget =
    | OutputFile of filename:string
    | OutputDefault


// =========================================
// utilities
// =========================================

let getOutputFilename output (options:AnalysisOptions) =
    // ensure the directory exists
    let ensure dirname = Directory.CreateDirectory(dirname) |> ignore; dirname

    match output with
    | OutputFile filename ->
        // a specific name was requested
        filename
    | OutputDefault ->
        match options with
        | File filename ->
            let dirname = FileInfo(filename).Directory.FullName
            let outdir = Path.Combine(dirname,SONARQUBE_OUT) |> ensure
            let outfile = Path.GetFileNameWithoutExtension(filename) |> sprintf "%s.sonarAnalysis.json"
            Path.Combine(outdir,outfile)
        | MultipleFiles filenames ->
            let filename = filenames.[0]
            let dirname = FileInfo(filename).Directory.FullName
            let outdir = Path.Combine(dirname,SONARQUBE_OUT) |> ensure
            let outfile = filename  |> sprintf "%s-and-more.sonarAnalysis.json"
            Path.Combine(outdir,outfile)
        | Directory dirname ->
            let outdir = Path.Combine(dirname,SONARQUBE_OUT) |> ensure
            let outfile = System.IO.DirectoryInfo(dirname).Name |> sprintf "%s.sonarAnalysis.json"
            Path.Combine(outdir,outfile)
        | Config filename ->
            let dirname = FileInfo(filename).Directory.FullName
            let outdir = Path.Combine(dirname,SONARQUBE_OUT) |> ensure
            let outfile = Path.GetFileNameWithoutExtension(filename) |> sprintf "%s.sonarAnalysis.json"
            Path.Combine(outdir,outfile)

// give a list of files, include only ".fs" ones that exist
let filterOnlyFsFiles (filenames:string list) =
    filenames
    |> List.filter (fun fn ->
        let isFsFile = Path.GetExtension(fn) = ".fs"
        if not isFsFile then logger.Warning("Skipping non-fs file {filename}", fn)
        isFsFile
        )
    |> List.filter (fun fn ->
        let fileExists = File.Exists(fn)
        if not fileExists then logger.Warning("Skipping non-existent file {filename}", fn)
        fileExists
        )


// =========================================
// API to call
// =========================================

let printVersion() =
    //TODO get version of SonarAnalyzer.FSharpas well
    let location = Assembly.GetExecutingAssembly().Location
    let currentAssemblyVersion = AssemblyName.GetAssemblyName(location).Version
    let coreAssemblyVersion = AssemblyName.GetAssemblyName("SonarAnalyzer.FSharp").Version

    printfn "Assembly Versions:"
    printfn "FsSonarRunner: %s" (currentAssemblyVersion.ToString())
    printfn "SonarAnalyzer.FSharp: %s" (coreAssemblyVersion.ToString())

let analyze (options:AnalysisOptions) output =
    let outputFilename = getOutputFilename output options

    let stopwatch = System.Diagnostics.Stopwatch()
    stopwatch.Start()

    let diagnostics =
        match options with
        | File filename ->
            logger.Information("Analyze {filename}. Output={output}", filename, outputFilename)
            let fsFiles = filterOnlyFsFiles [filename]
            RuleRunner.analyzeFilesWithAllRules fsFiles
        | MultipleFiles filenames ->
            logger.Information("Analyze multiple files. Output={output}", outputFilename)
            let fsFiles = filterOnlyFsFiles filenames
            RuleRunner.analyzeFilesWithAllRules fsFiles
        | Directory dirname ->
            logger.Information("Analyze directory {dirname}. Output={output}", dirname, outputFilename)
            let fsFiles = Directory.EnumerateFiles(dirname,"*.fs")
            for filename in fsFiles do
                logger.Information("...Adding file {filename}.", filename)
            RuleRunner.analyzeFilesWithAllRules fsFiles
        | Config xmlFilename ->
            let config = XmlAnalysisConfig.toDomainConfig xmlFilename
            RuleRunner.analyzeConfig config

    diagnostics |> OutputDiagnostics.outputTo outputFilename

    let elapsedTime = stopwatch.Elapsed
    logger.Information("Done. ElapsedTime={elapsedTime}. {diagnosticsCount} diagnostics found.", elapsedTime, diagnostics.Length)

let exportRules() =
    RuleDescriptorFiles.write(SONARQUBE_CONF)


// =========================================
// CLI
// =========================================

type CLIArguments =
    | [<AltCommandLine("-v")>] Version
    | [<AltCommandLine("-c")>] Config of path:string
    | [<AltCommandLine("-f")>] File of path:string
    | [<AltCommandLine("-d")>] Directory of path:string
    | [<AltCommandLine("-p")>] Project of path:string
    | [<AltCommandLine("-e")>] Export
    | [<AltCommandLine("-o")>] Output of path:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version _ -> "show the version."
            | Config _ -> "use a Sonar-format XML as input to specify which rules and which files to analyze."
            | File _ -> "analyze the specified file."
            | Directory _ -> "analyze the specified directory"
            | Project _ -> "analyze the specified project."
            | Export _ -> "export the rules as XML descriptor files"
            | Output _ -> "The file to output the results too. If missing, output to stdout"


let parser = ArgumentParser.Create<CLIArguments>(programName = "FsSonarRunner.exe")

let printUsage() = parser.PrintUsage() |> printfn "%s"

[<EntryPoint>]
let main argv =
    try
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let output =
            if results.Contains Output then
                let filename = results.GetResult Output
                OutputTarget.OutputFile filename
            else
                OutputTarget.OutputDefault

        if results.Contains Version then
            printVersion()
        elif results.Contains Config then
            let configFile = results.GetResult Config
            let options = AnalysisOptions.Config configFile
            analyze options output
        elif results.Contains File then
            let filenames = results.GetResults File
            let options =
                if filenames.Length = 1 then
                    AnalysisOptions.File filenames.[0]
                else
                    AnalysisOptions.MultipleFiles filenames
            analyze options output
        elif results.Contains Directory then
            let dirname = results.GetResult Directory
            let options = AnalysisOptions.Directory dirname
            analyze options output
        elif results.Contains Project then
            //let projname = results.GetResult Project
            //let options = AnalysisOptions.Project projname
            //analyze options output
            logger.Error "Analyse project not implemented"
        elif results.Contains Export then
            exportRules()
        else
            printUsage()
    with e ->
        printfn "%s" e.Message
    0


