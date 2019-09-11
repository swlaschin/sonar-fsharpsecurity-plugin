module Program

open System.IO
open Argu
open System.Reflection
open SonarAnalyzer.FSharp

open Serilog
open Serilog.Events

// =========================================
// Constants
// =========================================

/// Where configuration files live.
/// This is always under the current directory,
let SONARQUBE_CONF_DIR= @".sonarqube\conf"

/// The target directory to write files to. This could be under the current directory,
/// or a child of the directory contained a specified project
let SONARQUBE_OUT_DIR = @".sonarqube\out"

let DIAGNOSTICS_FILE = "sonarDiagnostics.xml"
let ANALYSIS_CONFIG_FILE = "sonarAnalysisConfig.xml"

// =========================================
// mini domain to show the allowable options
// =========================================

type ConfigSource =
    | SpecifiedFile of filename:string
    | DefaultDir

type OutputDir =
    | SpecifiedOutputDir of filename:string
    | DefaultOutputDir

type AnalysisOptions =
    | File of filename:string
    | MultipleFiles of filenames:string list
    | Directory of dirname:string
    | Config of source:ConfigSource



// =========================================
// Logging
// =========================================

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
let loggerPrefix = "FsSonarRunner"


// =========================================
// Utilities
// =========================================

// ensure the directory exists
let ensure dirname = Directory.CreateDirectory(dirname) |> ignore; dirname

let getConfigFilename (input:ConfigSource) =

    match input with
    | SpecifiedFile filename ->
        // a specific filename was requested
        filename
    | DefaultDir ->
        let defaultDir = ensure SONARQUBE_CONF_DIR
        Path.Combine(defaultDir,ANALYSIS_CONFIG_FILE)


// give a list of files, include only ".fs" ones that exist
let filterOnlyFsFiles (filenames:string list) =
    filenames
    |> List.filter (fun fn ->
        let isFsFile = Path.GetExtension(fn) = ".fs"
        if not isFsFile then logger.Warning("[{prefix}] Skipping non-fs file {filename}", loggerPrefix,fn)
        isFsFile
        )
    |> List.filter (fun fn ->
        let fileExists = File.Exists(fn)
        if not fileExists then logger.Warning("[{prefix}] Skipping non-existent file {filename}", loggerPrefix,fn)
        fileExists
        )

/// run a function inside a stopwatch
let withStopwatchDo elapsedHandler f =
    let stopwatch = System.Diagnostics.Stopwatch()
    stopwatch.Start()
    let result = f()
    stopwatch.Stop()
    elapsedHandler result stopwatch.Elapsed

    // could add try/catch exception here?


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

// =========================================
// Analyze
// =========================================

/// Given some analysisOptions and an OutputTarget, construct the directory to write the results to.
let getAnalysisOutputDir (output:OutputDir) (analysisOptions:AnalysisOptions) =

    match output with
    | SpecifiedOutputDir dirname ->
        // a specific directory was requested
        dirname
    | DefaultOutputDir ->
        match analysisOptions with
        | File filename ->
            // for one file, use the parent directory of the file
            FileInfo(filename).Directory.FullName |> ensure
        | MultipleFiles filenames ->
            // for multiple files, use the parent directory of the first file
            let filename = filenames.[0]
            FileInfo(filename).Directory.FullName
        | Directory dirname ->
            // for a directory, use as is
            dirname |> ensure
        | Config (SpecifiedFile filename) ->
            // use the parent directory of the config file
            FileInfo(filename).Directory.FullName |> ensure
        | Config (DefaultDir) ->
            // use the default output directory
            SONARQUBE_OUT_DIR

let makeAnalysisConfig filelist : AnalysisConfig.Root =
    let files : AnalysisConfig.File list = filelist |> List.map (fun filename -> {Filename = filename} )
    {
        Settings = []
        RuleSelection = AnalysisConfig.AllRules
        FileSelection = AnalysisConfig.SelectedFiles files
    }

/// Analyze a file or a directory with the given options
let analyze (analysisOptions:AnalysisOptions) output =

    let outputdir = getAnalysisOutputDir output analysisOptions
    let outputDiagnosticsFile = Path.Combine(outputdir, DIAGNOSTICS_FILE)
    let outputConfigFilename = Path.Combine(outputdir, ANALYSIS_CONFIG_FILE)

    // run this function inside a stopwatch
    fun () ->
        let config =
            match analysisOptions with
            | File filename ->
                logger.Information("[{prefix}] Analyze {filename}. Output={output}", loggerPrefix, filename, outputDiagnosticsFile)
                let fsFiles = filterOnlyFsFiles [filename]
                makeAnalysisConfig fsFiles
            | MultipleFiles filenames ->
                logger.Information("[{prefix}] Analyze multiple files. Output={output}", loggerPrefix, outputDiagnosticsFile)
                let fsFiles = filterOnlyFsFiles filenames
                makeAnalysisConfig fsFiles
            | Directory dirname ->
                logger.Information("[{prefix}] Analyze directory {dirname}. Output={output}", loggerPrefix, dirname, outputDiagnosticsFile)
                let fsFiles = Directory.EnumerateFiles(dirname,"*.fs") |> Seq.toList
                for filename in fsFiles do
                    logger.Information("[{prefix}] ...Adding file {filename}.", loggerPrefix, filename)
                makeAnalysisConfig fsFiles
            | Config input ->
                let inputConfigFilename = getConfigFilename input
                logger.Information("[{prefix}] Analyze config {configFilename}. Output={output}", loggerPrefix, inputConfigFilename, outputDiagnosticsFile)
                ImportExportAnalysisConfig.import inputConfigFilename

        logger.Information("[{prefix}] Starting analysis", loggerPrefix)
        let diagnostics = RuleRunner.analyzeConfig config

        diagnostics |> OutputDiagnostics.outputTo outputDiagnosticsFile
        config |> ImportExportAnalysisConfig.export outputConfigFilename

        diagnostics

    |> withStopwatchDo (fun diagnostics elapsedTime ->
        logger.Information("[{prefix}] Done. ElapsedTime={elapsedTime}. {diagnosticsCount} diagnostics found.", loggerPrefix, elapsedTime, diagnostics.Length)
        )

// =========================================
// Export
// =========================================

/// Given an OutputTarget, construct the
/// directory to export the rules to.
let getExportRulesDir (output:OutputDir)  =

    match output with
    | SpecifiedOutputDir dirname ->
        // a specific directory was requested
        dirname
    | DefaultOutputDir ->
        // the default is the .sonarqube directory
        SONARQUBE_CONF_DIR
    |> ensure

let exportRules output =

    // run this function inside a stopwatch
    fun () ->
        let outputDir = getExportRulesDir output
        ExportRuleDefinitions.write(outputDir)
        ExportQualityProfile.write(outputDir)

    |> withStopwatchDo (fun _ elapsedTime ->
        logger.Information("Done. ElapsedTime={elapsedTime}. ", elapsedTime)
        )


// =========================================
// CLI
// =========================================

type CLIArguments =
    | [<AltCommandLine("-v")>] Version
    | [<AltCommandLine("-f")>] File of path:string
    | [<AltCommandLine("-d")>] Directory of path:string
    | [<AltCommandLine("-p")>] Project of path:string
    | [<AltCommandLine("-c")>] Config
    | [<AltCommandLine("-ci")>] ConfigInput of path:string
    | [<AltCommandLine("-od")>] OutputDir of path:string
    | [<AltCommandLine("-e")>] Export
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Version _ -> "Show the version."
            | File _ -> "Analyze the specified file."
            | Directory _ -> "Analyze the specified directory"
            | Project _ -> "Analyze the specified project."
            | Config -> "Analyze the files and rules specified in the config file."
            | ConfigInput _ -> "If the Config option is used, optionally specify the location of the config file.. If missing, use the default config (in .sonarqube directory)"
            | OutputDir _ -> "The directory to output the diagnostics to. If missing, use the default location (the same directory that was analyzed)"
            | Export _ -> "Export the rules and profile to the specified directory. This is only used when creating resources for the java plugin"


let parser = ArgumentParser.Create<CLIArguments>(programName = "FsSonarRunner.exe")

let printUsage() = parser.PrintUsage() |> printfn "%s"

[<EntryPoint>]
let main argv =
    try
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let output =
            if results.Contains OutputDir then
                let dirname = results.GetResult OutputDir
                SpecifiedOutputDir dirname
            else
                DefaultOutputDir

        let configInput =
            if results.Contains ConfigInput then
                let filename = results.GetResult ConfigInput
                SpecifiedFile filename
            else
                DefaultDir

        if results.Contains Version then
            printVersion()
        elif results.Contains Config then
            let options = AnalysisOptions.Config configInput
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
            exportRules output
        else
            printUsage()
    with e ->
        printfn "%s" e.Message
    0


