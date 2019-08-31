module ImportExportAnalysisConfig

open SonarAnalyzer.FSharp

let logger = Serilog.Log.Logger

/// export the config to a file
let export xmlFilename (root:AnalysisConfig.Root) =
    let dto = AnalysisConfigDto.fromDomain root
    Utilities.serializeToXmlFile xmlFilename dto

/// import the config from a file
let import xmlFilename : AnalysisConfig.Root =
    let dto = Utilities.deserializeFromXmlFile xmlFilename
    AnalysisConfigDto.toDomain dto
