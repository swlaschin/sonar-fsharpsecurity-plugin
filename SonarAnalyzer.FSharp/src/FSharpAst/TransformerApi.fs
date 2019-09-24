namespace FSharpAst

(*
Top level functions to transform source code into an AST
*)


open System
open System.IO
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text


module FileApi =
    let logger = Serilog.Log.Logger
    let loggerPrefix = "FSharpAst"

    // See: https://github.com/fsharp/FSharp.Compiler.Service/issues/847.
    let private dotnetCoreReferences () =
        let (</>) x y = Path.Combine(x, y)
        let fsharpCoreDir = Path.GetDirectoryName(typeof<FSharp.Collections.List<_>>.Assembly.Location)
        let runtimeDir = Path.GetDirectoryName(typeof<System.Object>.Assembly.Location)

        [| 
            yield fsharpCoreDir </> "FSharp.Core.dll"
            yield! Directory.EnumerateFiles(runtimeDir, "*.dll")
        |]
        |> Array.distinct
        |> Array.filter File.Exists
        |> Array.distinctBy Path.GetFileName
        |> Array.map (fun location -> "-r:" + location)

    let getProjectOptions (checker:FSharpChecker) file (source: string) =
        let sourceText = SourceText.ofString source
        let assumeDotNetFramework = false

        let (options, _diagnostics) =
            checker.GetProjectOptionsFromScript(file, sourceText, assumeDotNetFramework = assumeDotNetFramework)
            |> Async.RunSynchronously

        let otherOptions =
            if assumeDotNetFramework then options.OtherOptions
            else
                [| yield! options.OtherOptions |> Array.filter (fun x -> not (x.StartsWith("-r:")))
                   yield! dotnetCoreReferences() |]

        { options with OtherOptions = otherOptions }

    /// Parse the source text associated with a file. Return a Result<AssemblyContents,Errors)
    let parseFileAndSource (filename:string) sourceText : Result<FSharpAssemblyContents,FSharpErrorInfo []> =
        let checker = FSharpChecker.Create(keepAssemblyContents=true)
                
        async {
            let projOptions = getProjectOptions checker filename sourceText

            // do the check
            let! projectResults = checker.ParseAndCheckProject(projOptions)
            return
                if projectResults.HasCriticalErrors then
                    Error projectResults.Errors
                else
                    Ok projectResults.AssemblyContents
        }
        |> Async.Catch
        |> Async.RunSynchronously
        |> function
            | Choice1Of2 contents -> contents
            | Choice2Of2 ex ->
                let msg = sprintf "Unexpected exception parsing file: '%s' Exception: '%s'" filename ex.Message
                logger.Error("[{prefix}] {msg}", loggerPrefix, msg)
                failwith msg

    /// Parse a file. Return a Result<AssemblyContents,Errors)
    let parseFile (filename:string) : Result<FSharpAssemblyContents,FSharpErrorInfo []> =
        let sourceText = IO.File.ReadAllText filename
        parseFileAndSource filename sourceText

    /// Translate sourceText with an associated file to a Tast
    let translateFileAndSource config filename sourceText : Result<Tast.ImplementationFile, FSharpErrorInfo[]> =
        let dummyResult : Tast.ImplementationFile = {Name=filename; Decls=[]}

        let isImplementationFile =
            let ext = IO.Path.GetExtension(filename)
            [".fs"; ".fsx"] |> List.contains ext

        if IO.File.Exists filename |> not then
            logger.Warning("[{prefix}] File not found: '{filename}'", loggerPrefix,filename)
            Ok dummyResult
        elif not isImplementationFile then
            logger.Information("[{prefix}] Skipping non .fs/.fsx file: '{filename}'", loggerPrefix,filename)
            Ok dummyResult
        else
            match parseFileAndSource filename sourceText with
            | Error errs ->
                Error errs
            | Ok assemblyContents ->
                let transformer = FileTransformer(config)
                if assemblyContents.ImplementationFiles.IsEmpty then
                    logger.Warning("[{prefix}] No implementation for: '{filename}'", loggerPrefix,filename)
                    Ok dummyResult
                else
                    let ast = transformer.TransformFile(assemblyContents.ImplementationFiles |> Seq.head)
                    Ok ast

    /// Translate a file to a Tast
    let translateFile config filename : Result<Tast.ImplementationFile, FSharpErrorInfo[]> =
        let sourceText = IO.File.ReadAllText filename
        translateFileAndSource config filename sourceText


module TextApi =

    // create a dummy script file and return its name
    let createDummyFile (sourceText:string) =
        let filename = IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), "fsx")
        IO.File.WriteAllText(filename, sourceText)
        filename

    /// Parse text outside of a file. Return a Result<AssemblyContents,Errors)
    let parseText (sourceText:string) : Result<FSharpAssemblyContents,FSharpErrorInfo []> =
        //let filename =  "temp.fsx"
        let filename = createDummyFile sourceText
        FileApi.parseFileAndSource filename sourceText

    /// Translate text in memory
    let translateText config sourceText : Result<Tast.ImplementationFile, FSharpErrorInfo[]> =
        //let filename =  "temp.fsx"
        let filename = createDummyFile sourceText
        FileApi.translateFileAndSource config filename sourceText

    /// Translate to a Tast with no location (e.g. for unit tests)
    let translateTextNoLocation sourceText : Result<Tast.ImplementationFile, FSharpErrorInfo[]> =
        let config = {TransformerConfig.Default with UseEmptyLocation=true}
        translateText config sourceText


module ProjectApi =

    /// Parse a complete project. Return a Result<AssemblyContents,Errors)
    let parseProject (projOptions:FSharpProjectOptions) : Result<FSharpAssemblyContents,FSharpErrorInfo []> =
        let checker = FSharpChecker.Create(keepAssemblyContents=true)

        async {
            // do the check
            let! projectResults = checker.ParseAndCheckProject(projOptions)
            return
                if projectResults.HasCriticalErrors then
                    Error projectResults.Errors
                else
                    Ok projectResults.AssemblyContents
        } |> Async.RunSynchronously

    /// Get the FS project name from the ProjOptions
    let projectName (projOptions:FSharpProjectOptions) =
        IO.Path.GetFileNameWithoutExtension(projOptions.ProjectFileName)

    /// Translate a FS project name to a CS project name.
    let translatedProjectName fsProjectName  =
        fsProjectName // same for now

    /// Given a target directory to translate into, and a file in the source project,
    /// return the location of the translated file
    let translatedFilePath targetPath csProjectName (fsFilePath:string) =
        let nameOnly = IO.Path.GetFileNameWithoutExtension(fsFilePath)
        let fileName = sprintf "%s.cs" nameOnly
        IO.Path.Combine(targetPath,csProjectName,fileName)

    /// ensure the target directory exists
    let ensureDirectory targetPath projectName =
        let dir = IO.Path.Combine(targetPath,projectName)
        IO.Directory.CreateDirectory(dir) |> ignore

    /// Translate every file in a project into the target directory.
    let translateProject targetPath (projOptions:FSharpProjectOptions) : Result<Tast.Assembly, FSharpErrorInfo[]> =
        let fsProjectName = projectName projOptions
        let csProjectName = translatedProjectName fsProjectName
        ensureDirectory targetPath csProjectName

        let transformerConfig = TransformerConfig.Default

        match parseProject projOptions with
        | Error errs ->
            Error errs
        | Ok assemblyContents ->
            let fileAsts = assemblyContents.ImplementationFiles |> List.map (fun file ->
                //let targetFile = translatedFilePath targetPath csProjectName file.FileName
                //let tw = new StreamWriter(path=targetFile)
                //let writer = Writer(transformerConfig,tw)
                let translator = FileTransformer(transformerConfig)
                translator.TransformFile(file)
                )
            let assemblyAst : Tast.Assembly = {
                Files = fileAsts
                }
            assemblyAst |> Ok
