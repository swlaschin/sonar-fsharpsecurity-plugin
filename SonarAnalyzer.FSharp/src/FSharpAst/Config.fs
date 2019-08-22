namespace FSharpAst

// ===========================
// Configuration
// ===========================

/// Configuration for the Transformer
type TransformerConfig = {

    /// The AST has location information for each object. This is useful for debugging and helful error messages.
    /// But it can interfere with testing (because of structural equality).
    /// To make testing easier, this flag allows an known empty location to be used instead.
    UseEmptyLocation : bool
    }
    with
    static member Default = {
        UseEmptyLocation = false
        }

