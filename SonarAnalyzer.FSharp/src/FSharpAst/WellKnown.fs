namespace FSharpAst

// some well known types, etc

[<RequireQualifiedAccess>]
module WellKnownType =

    // =======================================
    // F# specific types
    // =======================================

    // F# abbreviation for System.String
    let string : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "string"}

    // F# abbreviation for System.Char
    let char : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "char"}

    // F# abbreviation for System.Boolean
    let bool : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "bool"}

    // F#-only type
    let unit : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "unit"}

    // F# abbreviation for System.Object
    let obj : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "obj"}

    // F# abbreviation for int32
    let int : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "int"}

    // F# abbreviation for System.Int32
    let int32 : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "int32"}

    // F# abbreviation for Single
    let float32 : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "float32"}

    // F# abbreviation for Single
    let single : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "single"}

    // F# abbreviation for Double
    let float : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "float"}

    // F# abbreviation for Double
    let double : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "double"}

    // F# abbreviation for Decimal
    let decimal : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "decimal"}

    // F# abbreviation for F# "List"
    let list : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; CompiledName = "list`1"}

    // F#-only type
    let fsharpList : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; CompiledName = "List`1"}

    // F# abbreviation for IEnumerable
    let seq : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; CompiledName = "seq`1"}

    // F# abbreviation for F# "Option"
    let option : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "option`1"}

    // F#-only type
    let fsharpOption : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "Option`1"}

    // F#-only type
    let ExtraTopLevelOperators: Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; CompiledName = "ExtraTopLevelOperators"}

    // =======================================
    // CLR types
    // =======================================

    let SystemString : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "String"}

    let SystemChar : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Char"}

    let SystemBoolean: Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Boolean"}

    let SystemObject: Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Object"}

    let SystemInt32 : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Int32"}

    let SystemSingle : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Single"}

    let SystemDouble : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Double"}

    let SystemDecimal : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; CompiledName = "Decimal"}

    let SystemList : Tast.NamedTypeDescriptor =
        {AccessPath = "System.Collections.Generic"; CompiledName = "List`1"}

    let IEnumerable : Tast.NamedTypeDescriptor =
        {AccessPath = "System.Collections.Generic"; CompiledName = "IEnumerable`1"}


[<RequireQualifiedAccess>]
module WellKnownMember =

    let sprintf : Tast.MfvDescriptor =
        {DeclaringEntity = Some WellKnownType.ExtraTopLevelOperators; CompiledName = "PrintFormatToString"}
