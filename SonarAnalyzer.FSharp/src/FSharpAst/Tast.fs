[<RequireQualifiedAccess>]
module rec FSharpAst.Tast

(*
A Typed AST based on the FSharp.Compiler.Service Typed AST, but clearer and more tractable.

References to "Spec" mean the F# Compiler spec.
Currently at https://fsharp.org/specs/language-spec/4.1/FSharpSpec-4.1-latest.pdf

"Spec#5" means Spec, chapter 5.
"Spec#5.2.1" means Spec, section 5.2.1

*)

open System
open System.Collections.Generic
open System.IO


// ================================================
// Common/Shared
// ================================================

/// The location of a node in the Tast
type Location = {
    FileInfo : FileInfo
    StartLine : int
    StartColumn : int
    EndLine : int
    EndColumn : int
    }
    with
    override this.ToString() =
        sprintf "%s:%i:%i" this.FileInfo.Name this.StartLine this.StartColumn
    // a null location for when we don't have one
    static member NullLocation =
        {FileInfo=FileInfo __SOURCE_FILE__; StartLine=0; StartColumn=0; EndLine=0; EndColumn=0 }

/// An unhandled element in the source file
type Unhandled = {
    Comment : string
    Value : obj
    Location : Location
    }

/// The list of XML doc strings (such as this one!) associated with a declaration
type XmlDoc = string list

type QualifiedName = QualifiedName of string

type Accessibility = Public | Private | Protected | Internal

/// Parameters can be passed in singly or as tuples.
/// Eg in let f x (y,z) = ... there are two parameter groups.
/// The first param is a single and the second param is a tuple.
type ParameterGroup<'t> =
    | NoParam
    | Param of 't
    | TupleParam of 't list

/// A formatted string for a type
type TypeName = TypeName of string

// ================================================
// Keys to access types, members etc
// ================================================

//type ParamOption =
//    | IsParams
//    | IsOptional
//    | HasDefault of obj


//type RefKind =
//    | In
//    | Out
//    | Ref
//    | RefReadOnly

//type ParamSig = {
//    Name : string
//    Type : TypeSig
//    Options : ParamOption list
//    RefKind : RefKind option
//    }

//type MemberSig = {
//    Name : string
//    TypeArgs : string
//    ParamSigs : ParamSig list
//}

// ================================================
// Keys
// ================================================

/// A unique identifier for a named type.
/// To get the actual definition of the type, use the Descriptor to look it up in the NamedType dictionary.
type NamedTypeDescriptor = {
    /// The namespaces and modules this type is defined in.
    /// Eg. "MyNamespace.MyModule.MySubmodule"
    /// If any of these contain non-standard characters, they are wrapped in double backticks in the usual way.
    /// Eg. "MyNamespace.``My Module``.MySubmodule"
    AccessPath: string
    /// The CompiledName is used rather that the diplay name because it is unique
    /// and might be different from the DisplayName.
    /// 1) modules or other entities that have a CompiledNameAttribute
    /// 2) generic params cause the name to have a back tick followed by the number of generic params.
    ///    E.g. "MyType<'a>" has CompiledName="MyType`1"
    CompiledName : string
    }

/// A unique identifier for a member, function or value.
/// To get the actual definition, you can look it up in the Mfv Dictionary
type MfvDescriptor = {
    DeclaringEntity : NamedTypeDescriptor option
    CompiledName : string
    }

// ================================================
// Project and files
// ================================================

/// The root node of the typed abstract syntax tree
type Project = {
    /// The project file that was analyzed. None if not applicable.
    ProjectFile : FileInfo option
    /// The Tast for each implementation file in the project
    Files: ImplementationFile list
    /// A quick way to find information about named types that were encountered during analysis
    NamedTypeDict : Dictionary<NamedTypeDescriptor,NamedType>
    /// A quick way to find information about MFVs that were encountered during analysis
    MfvDict : Dictionary<MfvDescriptor,MfvDecl>
    }

/// An implementation file (as opposed to a signature file, etc) that has been analyzed.
/// It contains a list of top-level declarations.
type ImplementationFile = {
    FileInfo : FileInfo
    Decls: ImplementationFileDecl list
    }

/// A top-level declaration in an implementation file
type ImplementationFileDecl =
    | Entity of Entity
    | MfvDecl of MfvDecl
    | InitAction of InitAction

// ================================================
// Entities
// ================================================

// An entity is a container for other objects, such as a module, type declarations, etc.
type Entity =
    | Namespace of Namespace
    | Module of Module
    | TypeEntity of TypeEntity // the type entities are grouped together
    | UnhandledEntity of Unhandled

type Namespace = {
    Name : string
    Location : Location
    SubDecls : ImplementationFileDecl list
    }

type Module = {
    Name : string
    Location : Location
    Accessibility : Accessibility
    SubDecls : ImplementationFileDecl list
    }

/// A entity representing a type declaration.
/// See Spec#5.4.1
type TypeEntity =
    | UnhandledTypeEntity of Unhandled
    /// Types defined outside of this project
    | NonFSharpType of NonFSharpType
    | TypeAbbreviation of TypeAbbreviationDecl
    | ClassDecl of Unhandled
    | InterfaceDecl of Unhandled
    | DelegateDecl of Unhandled
    | StructDecl of Unhandled
    | RecordDecl of RecordDecl
    | UnionDecl of Unhandled
    | EnumDecl of Unhandled
    | ExceptionDecl of Unhandled
    | MeasureDecl of Unhandled
    | AbstractDecl of Unhandled


/// Fields all type declarations have in common
type TypeEntityCommon = {
    Name : string
    EnclosingEntity : NamedTypeDescriptor option
    XmlDoc : XmlDoc
    Accessibility : Accessibility
    Attributes : Attribute list
    GenericParameters : GenericParameter list
    MemberOrFunctionOrValues : MfvDecl list
    Location : Location
    }

/// Declaration of a Type Abbreviation
type TypeAbbreviationDecl = {
    Common: TypeEntityCommon
    // custom to this type
    AbbreviatedType : FSharpType
    }

/// Declaration of a Record
type RecordDecl = {
    Common: TypeEntityCommon
    // custom to this type
    Fields : FieldDecl list
    }

type FieldDecl = {
    Name : string
    Type : FSharpType
    }

type UnionCase = {
    Location : Location
    Name : string
    XmlDoc : XmlDoc
    Accessibility : Accessibility
    Attributes : Attribute list
    ReturnType : FSharpType
    Fields : FieldDecl list
    }

type ClassDecl= {
    Name : string
    }


// ============================================================
// Attributes associated with a declaration
// ============================================================

/// Information about how an Attribute is constructed. E.g. [<MyAttribute(param1)>]
[<CustomEquality; CustomComparison>]
type AttributeConstructorArgument = {
    ArgumentValue : obj
    //ArgumentType : FSharpType // ignore for now
    }
    // need to add CustomEquality and CustomComparison because of the use of "obj"
    with
    override this.Equals(other) =
        match other with
        | :? AttributeConstructorArgument as other ->
            this.ArgumentValue = other.ArgumentValue
        | _ ->
            false
    override this.GetHashCode() = hash this.ArgumentValue
    interface System.IComparable with
        member this.CompareTo (other) = if Object.ReferenceEquals(this,other) then 0 else 1

/// Information about how an Attribute is constructed. E.g. [<MyAttribute(size=param1)>]
[<CustomEquality; CustomComparison>]
type AttributeNamedArgument =
    {
    ArgumentName : string
    ArgumentValue : obj
    //ArgumentType : FSharpType // ignore for now
    }
    // need to add CustomEquality and CustomComparison because of the use of "obj"
    with
    override this.Equals(other) =
        match other with
        | :? AttributeNamedArgument as other ->
            this.ArgumentName = other.ArgumentName
            && this.ArgumentValue = other.ArgumentValue
        | _ ->
            false
    override this.GetHashCode() = hash this.ArgumentName
    interface System.IComparable with
        member this.CompareTo (other) = if Object.ReferenceEquals(this,other) then 0 else 1

/// Represents an Attribute attached to a type or parameter or module E.g. [<MyAttribute(size=param1)>]
type Attribute =
    {
    AttributeType : NamedTypeDescriptor
    ConstructorArguments : AttributeConstructorArgument list
    NamedArguments : AttributeNamedArgument list
    }

// ============================================================
// Parameters for a type constructor (aka Generic Parameter)
// ============================================================


/// See Spec#5.2 "Type Constraints"
type GenericParameterConstraint =
    | SubtypeConstraint of FSharpType
    | SupportsNullConstraint
    | RequiresDefaultConstructorConstraint
    | NonNullableValueTypeConstraint
    | ReferenceTypeConstraint
    | EnumerationConstraint of FSharpType
    | DelegateConstraint of argType:FSharpType * returnType:FSharpType
    | UnmanagedConstraint
    | EqualityConstraint
    | ComparisonConstraint
    | MemberConstraint of GenericParameterMemberConstraint
    | DefaultsToConstraint of TypeName

type GenericParameterMemberConstraint = {
    MemberName : string
    MemberIsStatic : bool
    MemberArgumentTypes : TypeName  list
    MemberReturnType : TypeName
    MemberSources : TypeName list
    }

/// Represents a type parameter. E.g. the 'T in type A<'T>
/// It can have constraints: type A<'T when 'T:equality>
/// It can have attributes: type A<[<MyAttribute>]'T >
/// Note: "Parameters" are what are used at the declaration site. "Arguments" are what are passed in when the type is constructed.
type GenericParameter = {
    Name : string
    XmlDoc : XmlDoc
    Constraints : GenericParameterConstraint list
    Attributes : Attribute list
    Kind : GenericParameterKind
    }

/// A GenericParameter cen be a normal type, or a measure
type GenericParameterKind = TypeKind | MeasureKind

// ================================================
// Type expressions
// ================================================

/// A type expression. Can be a named type, a tuple, a function type, etc.
/// For example, in "let x: int * int", "int * int" is a type expression.
/// A specific type expression can have arguments. For example, in "let x: List<int>",
/// the type is "List<'T>" with a type argument of "int".
/// See Spec#5.1 "Checking Syntactic Types"
type FSharpType =
    /// A type expression we don't know how to handle
    | UnknownFSharpType of string

    /// The type expression is a named type and a number of type arguments.
    /// let x : Option<int>
    | NamedType of NamedType

    /// A variable type references a type or measure variable in scope
    /// let x : 'a
    /// let y : int<'a>
    | VariableType of GenericParameter

    /// The type is a tuple of other types.
    /// let x : int * string
    | TupleType of FSharpType list

    /// A function type is curried, so has one input type and one return type
    /// let x : int -> (string -> bool)
    | FunctionType of FunctionType

    /// The type is an array
    /// let x : int[]
    | ArrayType of ArrayType

    /// The type is a ByRef<_>
    /// let x : byref<int>
    | ByRefType of ByRefType

    //| AnonType of FSharpType list

    /// Types we don't care about
    | OtherType


type NamedType = {
    Descriptor : NamedTypeDescriptor
    TypeArgs : FSharpType list
    }

type FunctionType = {
    Domain : FSharpType
    Range : FSharpType
    }

type ArrayType = {
    ArrayRank : int
    TypeArg : FSharpType
    }

type ByRefType = {
    TypeArg : FSharpType
    }

type NonFSharpType = {
    Name : string
    Namespace : string option
    // Note: Parameters are what is declared. Args are what are passed in.
    GenericParameters : GenericParameter list
    }

// ================================================
// Members, functions and values
// ================================================

/// A top level MemberOrFunctionOrValue declaration contains code (in the form of expressions).
/// Info for a MFV declaration is Name, type, attributes, etc.
type MfvDecl =
    | MemberDecl of MemberDecl
    | FunctionDecl of FunctionDecl
    | ValueDecl of ValueDecl
    | UnhandledMemberOrFunctionOrValue of Unhandled
    | TopLevelLambdaValueDecl of TopLevelLambdaValueDecl

type MemberDecl = {
    Info : MfvInfo
    Parameters : ParameterGroup<MfvInfo> list
    Body : Expression
    Location : Location
    }

type FunctionDecl = {
    Info : MfvInfo
    Parameters : ParameterGroup<MfvInfo> list
    Body : Expression
    Location : Location
    }

type TopLevelLambdaValueDecl = {
    Info : MfvInfo
    Parameters : ParameterGroup<MfvInfo> list
    Body : Expression
    Location : Location
    }

type ValueDecl = {
    Info : MfvInfo
    Body : Expression
    Location : Location
    }

type MfvInfo = {
    Name : string
    Type : FSharpType
    XmlDoc : XmlDoc
    Accessibility : Accessibility
    Attributes : Attribute list
    EnclosingEntity : NamedTypeDescriptor option
    }


// ================================================
// Expressions
// ================================================

type Expression =
    /// For expressions that we don't know how to transform
    | UnknownExpression of string

    // NOTE Use same order as defined in "Exprs.fsi" in FSC project
    // For documentation on each case, see comments for associated type

    | ValueExpr of ValueExpr
    | ApplicationExpr of ApplicationExpr
    | TypeLambdaExpr of TypeLambdaExpr
    | DecisionTreeExpr  of DecisionTreeExpr
    | DecisionTreeSuccessExpr of DecisionTreeSuccessExpr
    | LambdaExpr of LambdaExpr
    | IfThenElseExpr of IfThenElseExpr
    | LetExpr of LetExpr
    | CallExpr of CallExpr
    | NewObjectExpr of NewObjectExpr
    | ThisValueExpr of ThisOrBaseValueExpr
    | BaseValueExpr of ThisOrBaseValueExpr
    | QuoteExpr of QuoteExpr
    | LetRecExpr of LetRecExpr
    | NewRecordExpr of NewRecordExpr
    | NewAnonRecordExpr of NewRecordExpr
    | AnonRecordGetExpr of AnonRecordGetExpr
    | FieldGetExpr of FieldGetExpr
    | FieldSetExpr of FieldSetExpr
    | NewUnionCaseExpr of NewUnionCaseExpr
    | UnionCaseGetExpr of UnionCaseGetExpr
    | UnionCaseSetExpr of UnionCaseSetExpr
    | UnionCaseTagExpr of UnionCaseTagExpr
    | UnionCaseTestExpr of UnionCaseTestExpr
    | NewTupleExpr of NewTupleExpr
    | TupleGetExpr of TupleGetExpr
    | CoerceExpr of CoerceExpr
    | NewArrayExpr of NewArrayExpr
    | TypeTestExpr of TypeTestExpr
    | AddressSetExpr of AddressSetExpr
    | ValueSetExpr of ValueSetExpr
    | DefaultValueExpr of DefaultValueExpr
    | ConstantExpr of ConstantExpr
    | AddressOfExpr of AddressOfExpr
    | SequentialExpr of SequentialExpr
    | FastIntegerForLoopExpr of FastIntegerForLoopExpr
    | WhileLoopExpr of WhileLoopExpr
    | TryFinallyExpr of TryFinallyExpr
    | TryWithExpr of TryWithExpr
    | NewDelegateExpr of NewDelegateExpr
    | ILAsmExpr of ILAsmExpr
    | ILFieldGetExpr of ILFieldGetExpr
    | ILFieldSetExpr of ILFieldSetExpr
    | ObjectExpr of ObjectExpr
    //| TraitCall of TraitCall


/// A value expression references a Mfv
/// E.g ... = y where y is a Mfv declared elsewhere
type ValueExpr = {
    // we don't care about the implementation of the MFV, only the common stuff such as name and type
    Value : MfvDescriptor
    Location : Location
    }

/// An "application" expression represents the application of function values
/// E.g ... = f(1,"A") where f is the function, the arguments are [1; "A"], the arg types are [int; string]
type ApplicationExpr = {
    Function : Expression
    Args : Expression list
    ArgTypes : FSharpType list
    Location : Location
    }

/// A "type lambda" matches type abstractions
/// TODO
type TypeLambdaExpr = {
    GenericParameters : GenericParameter list
    Body : Expression
    Location : Location
    }

/// Matches expressions with a decision expression,
/// each branch of which ends in DecisionTreeSuccess passing control and values to one of the targets.
/// TODO
type DecisionTreeExpr = {
    Decision : Expression
    Branches : DecisionTreeBranch list
    Location : Location
    }

/// TODO
type DecisionTreeBranch = {
    Expr : Expression
    Targets : MfvInfo list
    }

/// Special expressions at the end of a conditional decision structure in the decision expression node of a DecisionTree .
/// The given expressions are passed as values to the decision tree target.
/// TODO
type DecisionTreeSuccessExpr = {
    Index : int
    Targets : Expression list
    Location : Location
    }


/// Matches expressions which are lambda abstractions
/// TODO
type LambdaExpr = {
    Function : MfvInfo
    Body : Expression
    Location : Location
    }

/// Matches expressions which are conditionals
/// TODO
type IfThenElseExpr = {
    Condition : Expression
    IfTrue : Expression
    IfFalse : Expression
    Location : Location
    }

(*
"Let" is short for "let [defn] in [body]" so code like
    let a = 1
    let b = 2
    a + b

means
    let a = 1 in (
      let b = 2 in (
        a + b
      )
    )

So the transform output is a series of nested LetExpr. In this case, something like
    LetExpr {
      Binding = MFV="a", Value = 1
      Body = LetExpr {
         Binding = MFV="b", Value = 2
         Body = ..addition expression...
      }

*)

/// Matches expressions which are let definitions
/// E.g. in
/// let x = 1 in (x + 2) the binding is (let x = 1) and the body is (x + 2)
type LetExpr = {
    Binding : Binding
    Body : Expression
    Location : Location
    }

/// The left side of a let definition
/// E.g. in
/// let x = 1 the info is {name="x"; type=int} and the expression is "const 1"
type Binding = {
    // info about the bound variable
    Info : MfvInfo
    // what it is bound to
    Expression : Expression
    }

/// A call to another member, function, or value
/// E.g
/// let x = y where y is a value in scope.
/// let x = y(1) where y is a function in scope.
/// let x = y.m where m is a property on y.
/// let x = y.m() where m is a method on y.
type CallExpr = {
    // for static members and functions, this is None.
    // for instance members and functions, this is the object to call against.
    Instance : Expression option
    Member : MfvDescriptor
    ClassTypeArgs : FSharpType list
    MethodTypeArgs : FSharpType list
    Args : Expression list
    ArgTypes : FSharpType list
    Location : Location
    }

/// Matches expressions which are calls to object constructors
/// TODO
type NewObjectExpr = {
    Ctor : MfvDescriptor
    // type args for generic classes. In "new X<int>()" int is a type arg
    TypeArgs : FSharpType list
    Args : Expression list
    ArgTypes : FSharpType list
    Location : Location
    }

/// Matches expressions which are uses of the 'this' or 'base' value
/// TODO
type ThisOrBaseValueExpr = {
    Type : FSharpType
    Location : Location
    }

/// Matches expressions which are quotation literals
/// TODO
type QuoteExpr = {
    Expr : Expression
    Location : Location
    }

/// Matches expressions which are let-rec definitions
/// TODO
type LetRecExpr = {
    Definitions : Binding list
    Body : Expression
    Location : Location
    }

/// Matches record expressions
/// TODO
type NewRecordExpr = {
    Type : FSharpType
    Args : Expression list
    ArgTypes : FSharpType list
    Location : Location
    }

/// Matches expressions getting a field from an anonymous record. The integer represents the
/// index into the sorted fields of the anonymous record.
type AnonRecordGetExpr = {
    Type : FSharpType
    Target : Expression
    Index : int
    Location : Location
    }

type FieldGetExpr = {
    Type : FSharpType
    Target : Expression option
    Field : FieldDecl
    Location : Location
    }

type FieldSetExpr = {
    Type : FSharpType
    Target : Expression option
    Field : FieldDecl
    SetExpr : Expression
    Location : Location
    }

type NewUnionCaseExpr = {
    Type : FSharpType
    Case : UnionCase
    Exprs : Expression list
    Location : Location
    }

type UnionCaseGetExpr = {
    Type : FSharpType
    Target : Expression
    Case : UnionCase
    Field : FieldDecl
    Location : Location
    }

type UnionCaseSetExpr = {
    Type : FSharpType
    Target : Expression
    Case : UnionCase
    Field : FieldDecl
    SetExpr : Expression
    Location : Location
    }

type UnionCaseTagExpr = {
    Type : FSharpType
    Target : Expression
    Location : Location
    }

type UnionCaseTestExpr = {
    Type : FSharpType
    Target : Expression
    Case : UnionCase
    Location : Location
    }

/// Expression to construct a new tuple.
/// In "let x = 1,"A" " : TupleType=int*string and Expressions=[1,"A"]
type NewTupleExpr = {
    /// The type of the tuple as a whole. E.g. Ast.TupleType [fstInt; fstString]
    TupleType : FSharpType
    /// The list of expressions to build the tuple.
    Args : Expression list
    Location : Location
}

/// Getting a value from tuple has a overall type, an index, and expression to hold the tuple
/// E.g let (_,x) = (1,"A").Item1 has type=int*string, index=1, and expression = NewTupleExpr as above
type TupleGetExpr = {
    Type : FSharpType
    Index : int
    Expression : Expression
    Location : Location
}

type CoerceExpr = {
    Type : FSharpType
    Target : Expression
    Location : Location
    }

/// Expression to construct a new array.
/// In "let x = [|1;2|]" : TypeArg=int and ValueArgs=[1;2]
type NewArrayExpr = {
    /// The type argument for Array<_>
    TypeArg : FSharpType
    /// The list of values to initialize the array with
    Args : Expression list
    Location : Location
    }

type TypeTestExpr = {
    Type : FSharpType
    Expr : Expression
    Location : Location
    }

type AddressSetExpr = {
    Address : Expression
    Expr : Expression
    Location : Location
    }

type ValueSetExpr = {
    Variable : MfvDescriptor
    Expr : Expression
    Location : Location
    }

type DefaultValueExpr = {
    Type : FSharpType
    Location : Location
    }

/// Expression to construct a const.
/// In "let x = 1" : Type=int and Value=1
type ConstantExpr = {
    /// The type of the constant
    Type : FSharpType
    /// The value of the constant
    Value : obj
    Location : Location
    }

type AddressOfExpr = {
    Expr : Expression
    Location : Location
    }

type SequentialExpr = {
    First : Expression
    Second : Expression
    Location : Location
    }

type FastIntegerForLoopExpr = {
    Start : Expression
    Finish : Expression
    Body : Expression
    Direction : bool
    Location : Location
}

type WhileLoopExpr = {
    Guard : Expression
    Body : Expression
    Location : Location
    }

type TryFinallyExpr = {
    Try : Expression
    Finally : Expression
    Location : Location
    }

type TryWithExpr = {
    Try : Expression
    V1 : MfvInfo
    V1Expr : Expression
    V2 : MfvInfo
    Handler : Expression
    Location : Location
    }

type NewDelegateExpr = {
    Type : FSharpType
    Expr : Expression
    Location : Location
    }

type ILAsmExpr = {
    Code : string
    ArgTypes : FSharpType list
    ArgExprs : Expression list
    Location : Location
    }

type ILFieldGetExpr = {
    Obj : Expression option
    Type : FSharpType
    FieldName : string
    Location : Location
    }

type ILFieldSetExpr = {
    Obj : Expression option
    Type : FSharpType
    FieldName : string
    SetExpr : Expression
    Location : Location
    }

type ObjectExpr = {
    BaseType : FSharpType
    BaseCall : Expression
    Overrides : ObjectExprOverride list
    InterfaceImplementations : ObjectExprInterfaceImplementation list
    Location : Location
}

type ObjectExprOverride = {
    Signature : AbstractSignature
    Body : Expression
    GenericParameters : GenericParameter list
    CurriedParameterGroups : ParameterGroup<MfvInfo> list
}

type ObjectExprInterfaceImplementation  = {
    Type : FSharpType
    Overrides : ObjectExprOverride list
}


type AbstractParameter = {
    Name : string option
    Type : FSharpType
    Attributes : Attribute list
    IsInArg : bool
    IsOutArg : bool
    IsOptionalArg : bool
    }


type AbstractSignature = {
    Name : string
    AbstractParameters : ParameterGroup<AbstractParameter> list
    AbstractReturnType : FSharpType
    }

// ================================================
// Other code objects
// ================================================

type InitAction = {
    Unhandled : Unhandled
    }


