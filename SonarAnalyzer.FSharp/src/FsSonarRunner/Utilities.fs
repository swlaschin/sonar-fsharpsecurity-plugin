module Utilities

(*
Helper functions used throughout
*)

open System.Xml
open System.IO
open System.Xml.Serialization
open System.Text

let logger = Serilog.Log.Logger

let serializeToXmlFile filePath (objectToSerialize:obj) =
    let settings = XmlWriterSettings(Indent = true,Encoding = new UTF8Encoding(false),IndentChars = "  ")
    
    let serializer = new XmlSerializer(objectToSerialize.GetType())
    let namespaces = XmlSerializerNamespaces [|XmlQualifiedName.Empty |]

    use stream = new MemoryStream()
    try
        use writer = XmlWriter.Create(stream, settings)
        serializer.Serialize(writer, objectToSerialize, namespaces)
        let ruleXml = Encoding.UTF8.GetString(stream.ToArray())
        System.IO.File.WriteAllText(filePath, ruleXml)
    with
    | ex ->
        logger.Error("Failed to write file at {filePath}. Exception: {exception}",filePath, ex.Message)
        reraise()

let deserializeFromXmlFile (filePath:string) : 'T =
    let settings = XmlReaderSettings()
    let serializer = new XmlSerializer( typeof<'T> )

    try
        use textReader = new StreamReader(filePath)
        use reader = XmlReader.Create(textReader, settings)
        let obj = serializer.Deserialize(reader)
        obj :?> 'T
    with
    | ex ->
        logger.Error("Failed to read file at {filePath}. Missing or wrong format? Exception: {exception}",filePath, ex.Message)
        reraise()

