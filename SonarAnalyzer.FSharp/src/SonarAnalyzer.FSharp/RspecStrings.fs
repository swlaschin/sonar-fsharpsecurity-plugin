namespace SonarAnalyzer.FSharp

// The RspecStrings.resx is copied from the C# source

/// A strongly-typed resource class, for looking up localized strings, etc.
type RspecStrings() =

    static let mutable resourceMan : System.Resources.ResourceManager option = None

    /// Returns the cached ResourceManager instance used by this class.
    static member ResourceManager : System.Resources.ResourceManager =
        if resourceMan.IsNone then
            let temp = System.Resources.ResourceManager("SonarAnalyzer.FSharp.RspecStrings", typeof<RspecStrings>.Assembly)
            resourceMan <- Some temp
        resourceMan.Value

