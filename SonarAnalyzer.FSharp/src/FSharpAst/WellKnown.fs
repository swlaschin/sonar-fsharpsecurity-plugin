namespace FSharpAst

// some well known types, etc

[<RequireQualifiedAccess>]
module WellKnownType =

    // =======================================
    // F# specific types
    // =======================================

    // F# abbreviation for System.String
    let string : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "string"; CompiledName = "string"}

    // F# abbreviation for System.Char
    let char : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "char"; CompiledName = "char"}

    // F# abbreviation for System.Boolean
    let bool : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "bool"; CompiledName = "bool"}

    // F#-only type
    let unit : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "unit"; CompiledName = "unit"}

    // F# abbreviation for System.Object
    let obj : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "obj"; CompiledName = "obj"}

    // F# abbreviation for int32
    let int : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "int"; CompiledName = "int"}

    // F# abbreviation for System.Int32
    let int32 : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "int32"; CompiledName = "int32"}

    // F# abbreviation for Single
    let float32 : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "float32"; CompiledName = "float32"}

    // F# abbreviation for Single
    let single : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "single"; CompiledName = "single"}

    // F# abbreviation for Double
    let float : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "float"; CompiledName = "float"}

    // F# abbreviation for Double
    let double : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "double"; CompiledName = "double"}

    // F# abbreviation for Decimal
    let decimal : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "decimal"; CompiledName = "decimal"}

    // F# abbreviation for F# "List"
    let list : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; DisplayName = "list"; CompiledName = "list`1"}

    // F#-only type
    let fsharpList : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; DisplayName = "List"; CompiledName = "List`1"}

    // F# abbreviation for IEnumerable
    let seq : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Collections"; DisplayName = "seq"; CompiledName = "seq`1"}

    // F# abbreviation for F# "Option"
    let option : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "option"; CompiledName = "option`1"}

    // F#-only type
    let fsharpOption : Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "Option"; CompiledName = "Option`1"}

    // F#-only type
    let ExtraTopLevelOperators: Tast.NamedTypeDescriptor =
        {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "ExtraTopLevelOperators"; CompiledName = "ExtraTopLevelOperators"}

    // =======================================
    // CLR types
    // =======================================

    let SystemString : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "String"; CompiledName = "String"}

    let SystemChar : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Char"; CompiledName = "Char"}

    let SystemBoolean: Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Boolean"; CompiledName = "Boolean"}

    let SystemObject: Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Object"; CompiledName = "Object"}

    let SystemInt32 : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Int32"; CompiledName = "Int32"}

    let SystemSingle : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Single"; CompiledName = "Single"}

    let SystemDouble : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Double"; CompiledName = "Double"}

    let SystemDecimal : Tast.NamedTypeDescriptor =
        {AccessPath = "System"; DisplayName = "Decimal"; CompiledName = "Decimal"}

    let SystemList : Tast.NamedTypeDescriptor =
        {AccessPath = "System.Collections.Generic"; DisplayName = "List"; CompiledName = "List`1"}

    let IEnumerable : Tast.NamedTypeDescriptor =
        {AccessPath = "System.Collections.Generic"; DisplayName = "IEnumerable"; CompiledName = "IEnumerable`1"}


[<RequireQualifiedAccess>]
module WellKnownMember =

    let sprintf : Tast.MemberDescriptor =
        {DeclaringEntity = Some WellKnownType.ExtraTopLevelOperators; DisplayName = "sprintf"; CompiledName = "PrintFormatToString"}
