namespace FSharpAst

open FSharp.Compiler.SourceCodeServices

// Context for logging errors
type Context = {
    Name : string
    Location : Tast.Location option
    }

type Stack<'a> = ResizeArray<'a>

module Stack =
    let tryTop (stack:Stack<_>) =
        if stack.Count = 0 then
            None
        else
            Some (stack.Item(0))

    let top (stack:Stack<_>) =
        if stack.Count = 0 then failwith "Stack underflow"
        stack.Item(0)

    let push (stack:Stack<_>) o =
        stack.Insert(0,o)

    let pop (stack:Stack<_>) =
        let top = top stack
        stack.RemoveAt(0)
        top


type FileTransformer(config:TransformerConfig) =
    // Note: use a class so that:
    // * globals such as state, logger, etc are available
    // * mutual recursion is easier with members rather than let-bound functions

    let logger = Serilog.Log.Logger
    let loggerPrefix = "FSharpAst.FileTransformer"

    // logging related
    let debug msg =
        logger.Debug(msg)
    let trace (loc:Tast.Location) msg =
        debug (sprintf "%O %s" loc msg)
    let warn msg =
        logger.Warning(msg)
    let error msg =
        logger.Error(msg)

    let declaringEntityStack = Stack<Tast.Entity>()

    //let adjustDeclaringEntity e =
    //    // if the entity is already the top one, then do nothing
    //    if e = Stack.top declaringEntityStack then
    //        ()
    //    // if the entity is on the stack already, then pop back to that one
    //    // else push it on the stack
    //    else
    //        Stack.push declaringEntityStack e


    //let currentDeclaringEntity() =
    //    Stack.tryTop declaringEntityStack

    let locationOf (range:FSharp.Compiler.Range.range) : Tast.Location =
        if not config.UseEmptyLocation then
            {
            FileName = range.FileName
            StartLine = range.StartLine
            StartColumn = range.StartColumn
            EndLine = range.EndLine
            EndColumn = range.EndColumn
            }
        else
            // use dummy values to make testing easier
            Tast.Location.NullLocation


    /// Convert the accessibility flags into a DU
    let accessibility (accessibility:FSharpAccessibility) (context:Context) =
        if accessibility.IsPublic then
            Tast.Accessibility.Public
        elif accessibility.IsPrivate then
            Tast.Accessibility.Private
        elif accessibility.IsInternal then
            Tast.Accessibility.Internal
        elif accessibility.IsProtected then
            Tast.Accessibility.Protected
        else
            let msg = sprintf "Unknown accessibility for Entity %s at %A" context.Name context.Location
            logger.Error("[{prefix}] {msg}", loggerPrefix, msg)
            Tast.Accessibility.Public // use default

    /// XmlDocs are stored as an IList<string>. This converts them into a string list
    let getXmlDoc xmlDoc =
        xmlDoc |> List.ofSeq

    let entityContext (entity:FSharpEntity) : Context =
        let location = locationOf entity.DeclarationLocation
        {Name=entity.CompiledName; Location=Some location}

    let tryMfvLocation (mfv:FSharpMemberOrFunctionOrValue) : Tast.Location option =
        if config.UseEmptyLocation then
            None
        else
            try
                mfv.ImplementationLocation |> Option.map locationOf
            with
            | _ ->
                try
                    mfv.DeclarationLocation |> locationOf |> Some
                with
                | _ ->
                    // compiler generated members such as .ctor will have no location
                    None

    let mfvContext (mfv:FSharpMemberOrFunctionOrValue) : Context =
        let locationOpt = tryMfvLocation mfv
        {Name=mfv.CompiledName; Location=locationOpt}

    // =======================================
    // Transform file and top level declarations
    // =======================================

    /// Transform the declarations and other contents of a file
    member this.TransformFile (implFile :FSharpImplementationFileContents) =
        //trace {Tast.Location.Default with FileName = implFile.FileName} "TransformFile"

        let decls = implFile.Declarations |> List.map this.TransformDecl
        let file : Tast.ImplementationFile = {
            Name = implFile.FileName
            Decls = decls
        }
        file

    /// Represents an individual declaration in an implementation file. Or subdeclarations in a container such as module/namespace/type
    /// Classified as:
    /// * "Entities" -> modules, type declarations, etc
    /// * "MemberOrFunctionOrValue" -> things with code expressions attached
    /// * "InitAction" -> initialization code not attached to anything
    member this.TransformDecl (fileDecl:FSharpImplementationFileDeclaration) : Tast.ImplementationFileDecl =
        match fileDecl with
        | FSharpImplementationFileDeclaration.Entity (entity, subDecls) ->
            this.TransformEntity(entity,subDecls)
            |> Tast.ImplementationFileDecl.Entity
        | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(mfv, vs, body) ->
            this.TransformMemberOrFunctionOrValue(mfv, vs, body)
            |> Tast.ImplementationFileDecl.MemberOrFunctionOrValue
        | FSharpImplementationFileDeclaration.InitAction(body) ->
            let initAction : Tast.InitAction = {
                Unhandled = {
                    Comment = "InitAction"
                    Location = Some (locationOf body.Range)
                    }
                }
            Tast.ImplementationFileDecl.InitAction initAction

    // =======================================
    // Transform entities
    // =======================================


    /// Represents an individual declaration in an implementation file. Or subdeclarations in a container such as module/namespace/type
    /// Classified as:
    /// * "Entities" -> modules, type declarations, etc
    /// * "MemberOrFunctionOrValue" -> things with code expressions attached
    /// * "InitAction" -> initialization code not attached to anything
    member this.TransformEntity(entity : FSharpEntity, subDecls:FSharpImplementationFileDeclaration list) : Tast.Entity =
        let location = locationOf entity.DeclarationLocation

        if entity.IsNamespace then
            let subDecls = subDecls |> List.map this.TransformDecl
            Tast.Namespace {Name=entity.CompiledName; Location=location; SubDecls=subDecls }
        elif entity.IsFSharpModule then
            let subDecls = subDecls |> List.map this.TransformDecl
            let context = entityContext entity
            let accessibility = accessibility entity.Accessibility context
            Tast.Module {Name=entity.CompiledName; Location=location; Accessibility=accessibility; SubDecls=subDecls}
        // handle type declarations in a different method
        elif entity.IsFSharpAbbreviation
            || entity.IsFSharpRecord
            || entity.IsFSharpUnion
            || entity.IsClass
            || entity.IsInterface
            || entity.IsDelegate
            || entity.IsEnum
            || entity.IsFSharpExceptionDeclaration
            || entity.IsMeasure
            || entity.IsProvided
            || entity.IsValueType then
                this.TransformTypeEntityDecl(entity,subDecls)
                |> Tast.TypeEntity
        else
            //TODO
            warn (sprintf "TransformEntity: Unhandled entity " + entity.CompiledName)
            let unhandled : Tast.Unhandled = {
                Comment = entity.CompiledName
                Location = Some (locationOf entity.DeclarationLocation)
            }
            Tast.UnhandledEntity unhandled

    /// Tranform a type entity into a NamedTypeDescriptor
    member this.NamedTypeDescriptor(entity:FSharpEntity) : Tast.NamedTypeDescriptor =
        {
            AccessPath = entity.AccessPath
            DisplayName = entity.DisplayName
            CompiledName = entity.CompiledName
        }

    /// Tranform a type into a TypeName
    member this.TypeName(ty:FSharpType) : Tast.TypeName =
        let context = FSharpDisplayContext.Empty
        ty.Format(context) |> Tast.TypeName


    /// If the entity declaration is known to be a type, transform to a TypeEntity
    member this.TransformTypeEntityDecl(entity:FSharpEntity, subDecls:FSharpImplementationFileDeclaration list) : Tast.TypeEntity =
        let location = locationOf entity.DeclarationLocation
        //trace location (sprintf "TypeEntity " + entity.CompiledName)

        let context = entityContext entity
        let name = entity.CompiledName
        let xmlDoc = getXmlDoc entity.XmlDoc
        let accessibility = accessibility entity.Accessibility context
        let genericParameters = this.TransformGenericParameters entity.GenericParameters
        let attributes =
            if entity.IsAttributeType || not entity.IsFSharp then
                // stop at first level otherwise we will drill down recursively and get a stack overflow
                // also, don't do this for system types like string
                []
            else
                this.TransformAttributes entity.Attributes
        let common : Tast.TypeEntityCommon = {
            Name = name
            Namespace = entity.Namespace
            Location = location
            XmlDoc = xmlDoc
            Accessibility = accessibility
            Attributes = attributes
            GenericParameters = genericParameters
            MemberOrFunctionOrValues = [] // to be added later
            }

        let typeEntity =
            if not entity.IsFSharp then
                Tast.NonFSharpType {
                    Name = name
                    Namespace = entity.Namespace
                    GenericParameters = genericParameters
                    }
            elif entity.IsFSharpAbbreviation then
                Tast.TypeAbbreviation {
                    Common = common
                    AbbreviatedType = this.TransformFSharpType entity.AbbreviatedType
                    }
            elif entity.IsFSharpRecord then
                Tast.Record {
                    Common = common
                    Fields = []
                    }
            elif entity.IsFSharpUnion then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsClass then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsInterface then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsDelegate then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsEnum then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsFSharpExceptionDeclaration then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsMeasure then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsProvided then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            elif entity.IsValueType then
                //TODO
                Tast.UnknownTypeEntity (entity.CompiledName,location)
            else
                warn (sprintf "TransformTypeEntityDecl: Unhandled entity " + entity.CompiledName)
                Tast.UnknownTypeEntity (entity.CompiledName,location)

        let currentTypeEntity = typeEntity
        typeEntity

    member this.TransformFSharpType (ty:FSharpType) : Tast.FSharpType =

        if ty.HasTypeDefinition then
            if ty.TypeDefinition.IsArrayType then
                let typeArg = ty.GenericArguments |> this.TransformGenericArguments |> List.head
                Tast.ArrayType {ArrayRank = ty.TypeDefinition.ArrayRank; TypeArg = typeArg}
            elif ty.TypeDefinition.IsByRef then
                let typeArg = ty.GenericArguments |> this.TransformGenericArguments |> List.head
                Tast.ByRefType {TypeArg = typeArg}
            else
                // otherwise it is a named type
                let descriptor = this.NamedTypeDescriptor(ty.TypeDefinition)
                let typeArgs = ty.GenericArguments |> this.TransformGenericArguments
                Tast.NamedType {Descriptor=descriptor; TypeArgs=typeArgs}
        elif ty.IsGenericParameter then
            let genParam = this.TransformGenericParameter ty.GenericParameter
            Tast.VariableType genParam
        elif ty.IsTupleType then
            let typeArgs = ty.GenericArguments |> this.TransformGenericArguments
            Tast.TupleType typeArgs
        elif ty.IsFunctionType then
            let typeArgs = ty.GenericArguments |> this.TransformGenericArguments
            match typeArgs with
            | [domain; range] -> Tast.FunctionType {Domain=domain; Range=range}
            | _ ->
                let msg = sprintf "TransformFSharpType.IsFunctionType: Expected 2 GenericArguments. Found %i" typeArgs.Length
                logger.Error("[{prefix}] {msg}", loggerPrefix, msg)
                failwith msg
        else
            warn (sprintf "TransformFSharpType: Unknown type classification for %A" ty)
            Tast.UnknownFSharpType (ty.ToString())

/// =============================================
/// Atributes
/// =============================================

    /// Transform a FSharpAttribute into a Tast.Attribute
    member this.TransformAttribute (attribute:FSharpAttribute) : Tast.Attribute =
        {
        AttributeType = this.NamedTypeDescriptor(attribute.AttributeType)
        ConstructorArguments =
            attribute.ConstructorArguments
            |> List.ofSeq
            |> List.map (fun (ty,ob) -> {
                ArgumentValue=ob
                //ArgumentType=this.TransformFSharpType ty
                } )
        NamedArguments =
            attribute.NamedArguments
            |> List.ofSeq
            |> List.map (fun (ty,name,ob,_bool) -> {
                ArgumentValue=ob
                ArgumentName=name
                //ArgumentType=this.TransformFSharpType ty
                } )
        }

    /// Attributes are stored as an IList<FSharpAttribute>. This converts them into a Tast.Attribute list
    member this.TransformAttributes (attributes:FSharpAttribute seq) =
        // Certain attributes are not relevant, such as
        // those in System.Runtime or System.Diagnostics
        let exclude (attr:FSharpAttribute) =
            let fullName = attr.AttributeType.FullName
            // List from "17.2 Custom Attributes Emitted by F#"
            [
                "System.Diagnostics"
                "System.Runtime"
                // "System.Reflection.DefaultMemberAttribute" // keep this one
                "FSharp.Core"
                "Microsoft.FSharp.Core.CompiledNameAttribute"
                "Microsoft.FSharp.Core.GeneralizableValueAttribute"
            ] |> List.exists (fun attrPrefix -> fullName.StartsWith(attrPrefix) )

        attributes
        |> Seq.filter (exclude >> not)
        |> List.ofSeq
        |> List.map this.TransformAttribute

/// =============================================
/// GenericParameterConstraints
/// =============================================

    /// Constraints are stored as an IList<FSharpGenericParameterConstraint>. This converts them into a Tast.GenericParameterConstraint list
    member this.TransformGenericParameterConstraint (gpConstraint:FSharpGenericParameterConstraint) : Tast.GenericParameterConstraint =
        if gpConstraint.IsCoercesToConstraint then
            let target = this.TransformFSharpType gpConstraint.CoercesToTarget
            Tast.SubtypeConstraint target
        elif gpConstraint.IsSupportsNullConstraint then
            Tast.SupportsNullConstraint
        elif gpConstraint.IsRequiresDefaultConstructorConstraint then
            Tast.RequiresDefaultConstructorConstraint
        elif gpConstraint.IsNonNullableValueTypeConstraint then
            Tast.NonNullableValueTypeConstraint
        elif gpConstraint.IsReferenceTypeConstraint then
            Tast.ReferenceTypeConstraint
        elif gpConstraint.IsEnumConstraint then
            let target = this.TransformFSharpType gpConstraint.EnumConstraintTarget
            Tast.EnumerationConstraint target
        elif gpConstraint.IsDelegateConstraint then
            let argTarget = this.TransformFSharpType gpConstraint.DelegateConstraintData.DelegateTupledArgumentType
            let returnTarget = this.TransformFSharpType gpConstraint.DelegateConstraintData.DelegateReturnType
            Tast.DelegateConstraint (argTarget,returnTarget)
        elif gpConstraint.IsUnmanagedConstraint then
            Tast.UnmanagedConstraint
        elif gpConstraint.IsEqualityConstraint then
            Tast.EqualityConstraint
        elif gpConstraint.IsComparisonConstraint then
            Tast.ComparisonConstraint
        elif gpConstraint.IsMemberConstraint then
            let info = gpConstraint.MemberConstraintData |> this.TransformMemberConstraintData
            Tast.MemberConstraint info
        elif gpConstraint.IsDefaultsToConstraint then
            //TODO fix stack overflow when TransformFSharpType is used. So use TypeName instead
            // Tast.DefaultsToConstraint (this.TransformFSharpType gpConstraint.DefaultsToConstraintData.DefaultsToTarget)
            Tast.DefaultsToConstraint (this.TypeName gpConstraint.DefaultsToConstraintData.DefaultsToTarget)
        else
            let msg = sprintf "Unknown FSharpGenericParameterConstraint %A" gpConstraint
            logger.Error("[{prefix}] {msg}", loggerPrefix, msg)
            failwith msg

    /// Constraints are stored as an IList<FSharpGenericParameterConstraint>. This converts them into a Tast.GenericParameterConstraint list
    member this.TransformGenericParameterConstraints (constraints:FSharpGenericParameterConstraint seq) =
        constraints
        |> List.ofSeq
        |> List.map this.TransformGenericParameterConstraint

    member this.TransformMemberConstraintData (memberConstraint:FSharpGenericParameterMemberConstraint) : Tast.GenericParameterMemberConstraint =
        {
            MemberName = memberConstraint.MemberName
            MemberIsStatic = memberConstraint.MemberIsStatic
            //TODO fix stack overflow when TransformFSharpType is used. So use TypeName instead
            MemberArgumentTypes = memberConstraint.MemberArgumentTypes |> List.ofSeq |> List.map this.TypeName
            MemberReturnType = memberConstraint.MemberReturnType |> this.TypeName
            MemberSources = memberConstraint.MemberSources |> List.ofSeq |> List.map this.TypeName
        }


    member this.TransformGenericParameter (gp:FSharpGenericParameter) : Tast.GenericParameter =
        let xmlDoc = getXmlDoc gp.XmlDoc
        {
        Name = gp.Name
        XmlDoc = xmlDoc
        Constraints = gp.Constraints |> this.TransformGenericParameterConstraints
        Attributes = gp.Attributes |> this.TransformAttributes
        Kind = if gp.IsMeasure then Tast.MeasureKind else Tast.TypeKind
        }

    member this.TransformGenericParameters (gps:FSharpGenericParameter seq) =
        gps
        |> List.ofSeq
        |> List.map this.TransformGenericParameter


    member this.TransformGenericArgument (ty:FSharpType) : Tast.FSharpType=
        this.TransformFSharpType ty

    member this.TransformGenericArguments (args:FSharpType seq) =
        args
        |> List.ofSeq
        |> List.map this.TransformGenericArgument


/// =============================================
/// Members, functions, or values
/// =============================================

    /// extract the member info from an MFV
    member this.MemberDescriptor(mfv:FSharpMemberOrFunctionOrValue) : Tast.MemberDescriptor =
        {
        DeclaringEntity = mfv.DeclaringEntity |> Option.map this.NamedTypeDescriptor
        CompiledName = mfv.CompiledName
        DisplayName = mfv.DisplayName
        }

    /// extract the common parts of an MFV
    member this.TransformMFVInfo(mfv:FSharpMemberOrFunctionOrValue) : Tast.MFVInfo =
        let context = mfvContext mfv
        let name = mfv.CompiledName
        let xmlDoc = getXmlDoc mfv.XmlDoc
        let accessibility = accessibility mfv.Accessibility context
        let attributes =
                this.TransformAttributes mfv.Attributes
        {
            Name = name
            Location = context.Location
            XmlDoc = xmlDoc
            Accessibility = accessibility
            Attributes = attributes
            Type = this.TransformFSharpType mfv.FullType
        }

    /// Transform a TransformMemberOrFunctionOrValue
    member this.TransformMemberOrFunctionOrValue (mfv:FSharpMemberOrFunctionOrValue,args: FSharpMemberOrFunctionOrValue list list, body: FSharpExpr) : Tast.MemberOrFunctionOrValue =
        if mfv.IsMember then
            this.TransformMember(mfv,args,body) |> Tast.Member
        elif mfv.IsTypeFunction then
            this.TransformFunction(mfv,args,body) |> Tast.Function
        elif mfv.IsValue then
            this.TransformValue(mfv,args,body) |> Tast.Value
        elif mfv.IsValCompiledAsMethod then
            // top level lambdas such as let x = fun y -> ...
            this.TransformValCompiledAsMethod(mfv,args,body) |> Tast.TopLevelLambdaValue
        elif mfv.IsModuleValueOrMember then
            let unhandled : Tast.Unhandled = {
                Comment = mfv.CompiledName
                Location = Some (locationOf mfv.DeclarationLocation)
            }
            Tast.UnhandledMemberOrFunctionOrValue unhandled
        else
            let unhandled : Tast.Unhandled = {
                Comment = mfv.CompiledName
                Location = Some (locationOf mfv.DeclarationLocation)
            }
            Tast.UnhandledMemberOrFunctionOrValue unhandled

    member this.TransformMember (mfv:FSharpMemberOrFunctionOrValue, argListList:FSharpMemberOrFunctionOrValue list list, body: FSharpExpr) : Tast.MemberDecl  =
        {
            Info = this.TransformMFVInfo mfv
            EnclosingEntity = mfv.DeclaringEntity |> Option.map this.NamedTypeDescriptor
            Parameters = [
                for argList in argListList do
                    if argList.Length = 0 then
                        yield Tast.NoParam
                    elif argList.Length = 1 then
                        yield Tast.Param (argList.Head |> this.TransformMFVInfo)
                    else
                        yield Tast.TupleParam [for arg in argList do yield this.TransformMFVInfo arg ]
                ]
            Body = this.TransformExpr body
        }

    member this.TransformFunction (mfv:FSharpMemberOrFunctionOrValue, argListList: FSharpMemberOrFunctionOrValue list list, body: FSharpExpr) : Tast.FunctionDecl  =
        {
            Info = this.TransformMFVInfo mfv
            EnclosingEntity = mfv.DeclaringEntity |> Option.map this.NamedTypeDescriptor
            Parameters = [
                for argList in argListList do
                    if argList.Length = 0 then
                        yield Tast.NoParam
                    elif argList.Length = 1 then
                        yield Tast.Param (argList.Head |> this.TransformMFVInfo)
                    else
                        yield Tast.TupleParam [for arg in argList do yield this.TransformMFVInfo arg ]
                ]
            Body = this.TransformExpr body
        }

    member this.TransformValue (mfv:FSharpMemberOrFunctionOrValue,args: FSharpMemberOrFunctionOrValue list list, body: FSharpExpr) : Tast.ValueDecl  =
        {
            Info = this.TransformMFVInfo mfv
            EnclosingEntity = mfv.DeclaringEntity |> Option.map this.NamedTypeDescriptor
            Body = this.TransformExpr(body)
        }

    member this.TransformValCompiledAsMethod (mfv:FSharpMemberOrFunctionOrValue, argListList:FSharpMemberOrFunctionOrValue list list, body: FSharpExpr) : Tast.TopLevelLambdaValue =
        {
            Info = this.TransformMFVInfo mfv
            EnclosingEntity = mfv.DeclaringEntity |> Option.map this.NamedTypeDescriptor
            Parameters = [
                for argList in argListList do
                    if argList.Length = 0 then
                        yield Tast.NoParam
                    elif argList.Length = 1 then
                        yield Tast.Param (argList.Head |> this.TransformMFVInfo)
                    else
                        yield Tast.TupleParam [for arg in argList do yield this.TransformMFVInfo arg ]
                ]
            Body = this.TransformExpr body
        }

    member this.TransformExpr (body: FSharpExpr) : Tast.Expression =
        let location = locationOf body.Range
        try
            //NOTE Use same order as defined in "Exprs.fsi" in FSC project
            match body with

            /// Matches expressions which are uses of values
            | BasicPatterns.Value (value:FSharpMemberOrFunctionOrValue)  ->
                Tast.ValueExpr {
                    // A value refers to another MemberOrFunctionOrValue
                    // we don't care about the implementation of the MFV, only the common stuff such as name and type
                    Value = this.TransformMFVInfo(value)
                    Location = location
                }

            /// Matches expressions which are the application of function values
            | BasicPatterns.Application (expr:FSharpExpr, types:FSharpType list, exprs:FSharpExpr list) ->
                let expr = this.TransformExpr expr
                let types = types |> List.map this.TransformFSharpType
                let exprs = exprs  |> List.map this.TransformExpr
                Tast.ApplicationExpr {
                    Function = expr
                    ArgTypes = types
                    Args  = exprs
                    Location = location
                }

            /// Matches expressions which are type abstractions
            | BasicPatterns.TypeLambda (typeParams:FSharpGenericParameter list,body:FSharpExpr) ->
                Tast.TypeLambdaExpr {
                    GenericParameters = typeParams |> List.map this.TransformGenericParameter
                    Body = this.TransformExpr body
                    Location = location
                }

            /// Matches expressions with a decision expression, each branch of which ends in DecisionTreeSuccess pasing control and values to one of the targets.
            | BasicPatterns.DecisionTree (expr:FSharpExpr, branches: (FSharpMemberOrFunctionOrValue list * FSharpExpr) list) ->
                Tast.DecisionTreeExpr {
                    Decision = this.TransformExpr expr
                    Branches =
                        branches
                        |> List.map (fun (mfvs,expr) ->
                            let mfvs = mfvs |> List.map this.TransformMFVInfo
                            let expr = this.TransformExpr expr
                            {Targets=mfvs; Expr=expr}
                            )
                    Location = location
                }

            /// Special expressions at the end of a conditional decision structure in the decision expression node of a DecisionTree .
            /// The given expressions are passed as values to the decision tree target.
            | BasicPatterns.DecisionTreeSuccess (i:int, exprs:FSharpExpr list) ->
                Tast.DecisionTreeSuccessExpr {
                    Index = i
                    Targets = exprs |> List.map this.TransformExpr
                    Location = location
                    }

            /// Matches expressions which are lambda abstractions
            | BasicPatterns.Lambda (value:FSharpMemberOrFunctionOrValue, body:FSharpExpr) ->
                Tast.LambdaExpr {
                    Function = this.TransformMFVInfo value
                    Body = this.TransformExpr body
                    Location = location
                    }

            /// Matches expressions which are conditionals
            | BasicPatterns.IfThenElse (condition:FSharpExpr, ifTrue:FSharpExpr, ifFalse:FSharpExpr) ->
                Tast.IfThenElseExpr {
                    Condition = this.TransformExpr condition
                    IfTrue = this.TransformExpr ifTrue
                    IfFalse = this.TransformExpr ifFalse
                    Location = location
                    }

            /// Matches expressions which are let definitions
            | BasicPatterns.Let (defn:(FSharpMemberOrFunctionOrValue * FSharpExpr), body:FSharpExpr) ->
                Tast.LetExpr {
                    Binding=
                        let mfv = this.TransformMFVInfo (fst defn)
                        let expr = this.TransformExpr (snd defn)
                        {Info = mfv; Expression = expr }
                    Body = this.TransformExpr body
                    Location = location
                    }

            /// Matches expressions which are calls to members or module-defined functions. When calling curried functions and members the
            /// arguments are collapsed to a single collection of arguments, as done in the compiled version of these.
            | BasicPatterns.Call (expr:FSharpExpr option, mfv:FSharpMemberOrFunctionOrValue, types1:FSharpType list, types2:FSharpType list, exprs:FSharpExpr list) ->
                let expr = expr |> Option.map this.TransformExpr
                let memb = mfv |> this.MemberDescriptor
                let types1 = types1 |> List.map this.TransformFSharpType
                let types2 = types2 |> List.map this.TransformFSharpType
                let args = exprs |> List.map this.TransformExpr
                Tast.CallExpr {
                    Expression = expr
                    Member = memb
                    ClassTypeArgs = types1
                    MethodTypeArgs = types2
                    Args = args
                    ArgTypes = exprs |> List.map (fun arg -> this.TransformFSharpType arg.Type)
                    Location = location
                }

            /// Matches expressions which are calls to object constructors
            | BasicPatterns.NewObject (target:FSharpMemberOrFunctionOrValue, typeArgs: FSharpType list, ctorArgs: FSharpExpr list) ->
                Tast.NewObjectExpr {
                    Ctor = this.MemberDescriptor target
                    TypeArgs = typeArgs |> List.map this.TransformFSharpType
                    Args = ctorArgs |> List.map this.TransformExpr
                    ArgTypes = ctorArgs |> List.map (fun arg -> this.TransformFSharpType arg.Type)
                    Location = location
                }

            /// Matches expressions which are uses of the 'this' value
            | BasicPatterns.ThisValue (ty:FSharpType) ->
                Tast.ThisValueExpr  {
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which are uses of the 'base' value
            | BasicPatterns.BaseValue (ty:FSharpType) ->
                Tast.BaseValueExpr  {
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which are quotation literals
            | BasicPatterns.Quote (expr:FSharpExpr) ->
                Tast.QuoteExpr  {
                    Expr = this.TransformExpr expr
                    Location = location
                }

            /// Matches expressions which are let-rec definitions
            | BasicPatterns.LetRec (defns:(FSharpMemberOrFunctionOrValue * FSharpExpr) list, body: FSharpExpr) ->
                Tast.LetRecExpr {
                    Definitions = defns |> List.map (fun (mfv,expr) ->
                        let mfv = this.TransformMFVInfo mfv
                        let expr = this.TransformExpr expr
                        {Info = mfv; Expression = expr }
                        )
                    Body = this.TransformExpr body
                    Location = location
                    }

            /// Matches record expressions
            | BasicPatterns.NewRecord (ty:FSharpType, args:FSharpExpr list) ->
                Tast.NewRecordExpr  {
                    Type = this.TransformFSharpType ty
                    Args = args |> List.map this.TransformExpr
                    ArgTypes = args |> List.map (fun arg -> this.TransformFSharpType arg.Type)
                    Location = location
                }

            /// Matches anonymous record expressions
            | BasicPatterns.NewAnonRecord (ty:FSharpType, args: FSharpExpr list) ->
                Tast.NewAnonRecordExpr  {
                    Type = this.TransformFSharpType ty
                    Args = args |> List.map this.TransformExpr
                    ArgTypes = args |> List.map (fun arg -> this.TransformFSharpType arg.Type)
                    Location = location
                }

            /// Matches expressions getting a field from an anonymous record. The integer represents the
            /// index into the sorted fields of the anonymous record.
            | BasicPatterns.AnonRecordGet (target:FSharpExpr, ty: FSharpType, index: int) ->
                Tast.AnonRecordGetExpr {
                    Type = this.TransformFSharpType ty
                    Target = this.TransformExpr target
                    Index = index
                    Location = location
                }

            /// Matches expressions which get a field from a record or class
            | BasicPatterns.FSharpFieldGet (target:FSharpExpr option,ty: FSharpType, field: FSharpField) ->
                Tast.FieldGetExpr {
                    Type = this.TransformFSharpType ty
                    Target = target |> Option.map this.TransformExpr
                    Field = this.TransformField field
                    Location = location
                }

            /// Matches expressions which set a field in a record or class
            | BasicPatterns.FSharpFieldSet (target:FSharpExpr option, ty:FSharpType, field: FSharpField, setExpr: FSharpExpr) ->
                Tast.FieldSetExpr {
                    Type = this.TransformFSharpType ty
                    Target = target |> Option.map this.TransformExpr
                    Field = this.TransformField field
                    SetExpr = this.TransformExpr setExpr
                    Location = location
                }

            /// Matches expressions which create an object corresponding to a union case
            | BasicPatterns.NewUnionCase (ty:FSharpType, case: FSharpUnionCase, exprs: FSharpExpr list) ->
                Tast.NewUnionCaseExpr {
                    Type = this.TransformFSharpType ty
                    Case = this.TransformUnionCase case
                    Exprs = exprs |> List.map this.TransformExpr
                    Location = location
                }

            /// Matches expressions which get a field from a union case
            | BasicPatterns.UnionCaseGet (target:FSharpExpr, ty:FSharpType, case:FSharpUnionCase, field:FSharpField) ->
                Tast.UnionCaseGetExpr {
                    Target = this.TransformExpr target
                    Type = this.TransformFSharpType ty
                    Case = this.TransformUnionCase case
                    Field = this.TransformField field
                    Location = location
                }

            /// Matches expressions which set a field from a union case (only used in FSharp.Core itself)
            | BasicPatterns.UnionCaseSet (target:FSharpExpr, ty:FSharpType, case:FSharpUnionCase, field:FSharpField, setExpr: FSharpExpr) ->
                Tast.UnionCaseSetExpr {
                    Target = this.TransformExpr target
                    Type = this.TransformFSharpType ty
                    Case = this.TransformUnionCase case
                    Field = this.TransformField field
                    SetExpr = this.TransformExpr setExpr
                    Location = location
                }

            /// Matches expressions which gets the tag for a union case
            | BasicPatterns.UnionCaseTag (target:FSharpExpr, ty:FSharpType) ->
                Tast.UnionCaseTagExpr {
                    Target = this.TransformExpr target
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which test if an expression corresponds to a particular union case
            | BasicPatterns.UnionCaseTest (target:FSharpExpr, ty:FSharpType, case:FSharpUnionCase) ->
                Tast.UnionCaseTestExpr {
                    Target = this.TransformExpr target
                    Type = this.TransformFSharpType ty
                    Case = this.TransformUnionCase case
                    Location = location
                }

            /// Matches tuple expressions
            | BasicPatterns.NewTuple (ty:FSharpType, expressions: FSharpExpr list) ->
                // A tuple has a overall type and a list of expressions, one for each part of the tuple
                Tast.NewTupleExpr {
                    TupleType = this.TransformFSharpType ty
                    Args = expressions |> List.map this.TransformExpr
                    Location = location
                }

            /// Matches expressions which get a value from a tuple
            | BasicPatterns.TupleGet (ty:FSharpType, index:int, expr: FSharpExpr) ->
                Tast.TupleGetExpr {
                    Type = this.TransformFSharpType ty
                    Index = index
                    Expression = this.TransformExpr expr
                    Location = location
                }

            /// Matches expressions which coerce the type of a value
            | BasicPatterns.Coerce (ty:FSharpType, target:FSharpExpr) ->
                Tast.CoerceExpr {
                    Target = this.TransformExpr target
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches array expressions
            | BasicPatterns.NewArray (ty:FSharpType, exprs: FSharpExpr list) ->
                Tast.NewArrayExpr {
                    Args = exprs |> List.map this.TransformExpr
                    TypeArg = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which test the runtime type of a value
            | BasicPatterns.TypeTest (ty:FSharpType, expr:FSharpExpr) ->
                Tast.TypeTestExpr {
                    Expr = this.TransformExpr expr
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which set the contents of an address
            | BasicPatterns.AddressSet (address:FSharpExpr, expr:FSharpExpr) ->
                Tast.AddressSetExpr {
                    Address = this.TransformExpr address
                    Expr = this.TransformExpr expr
                    Location = location
                }

            /// Matches expressions which set the contents of a mutable variable
            | BasicPatterns.ValueSet (variable:FSharpMemberOrFunctionOrValue, expr:FSharpExpr) ->
                Tast.ValueSetExpr {
                    Variable = this.TransformMFVInfo variable
                    Expr = this.TransformExpr expr
                    Location = location
                }

            /// Matches default-value expressions, including null expressions
            | BasicPatterns.DefaultValue (ty:FSharpType) ->
                Tast.DefaultValueExpr {
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches constant expressions, including signed and unsigned integers, strings, characters, booleans, arrays
            /// of bytes and arrays of unit16.
            | BasicPatterns.Const (value:obj, ty:FSharpType) ->
                // A constant has a type and a value of that type
                Tast.ConstantExpr {
                    Type = this.TransformFSharpType ty
                    Value = value
                    Location = location
                }

            /// Matches expressions which take the address of a location
            | BasicPatterns.AddressOf (expr:FSharpExpr) ->
                Tast.AddressOfExpr {
                    Expr = this.TransformExpr expr
                    Location = location
                }

            /// Matches sequential expressions
            | BasicPatterns.Sequential(firstExpr:FSharpExpr, secondExpr:FSharpExpr) ->
                Tast.SequentialExpr {
                    First = this.TransformExpr firstExpr
                    Second = this.TransformExpr secondExpr
                    Location = location
                }

            /// Matches fast-integer loops (up or down)
            | BasicPatterns.FastIntegerForLoop(start:FSharpExpr,finish:FSharpExpr, body: FSharpExpr, direction: bool) ->
                Tast.FastIntegerForLoopExpr {
                    Start = this.TransformExpr start
                    Finish = this.TransformExpr finish
                    Body = this.TransformExpr body
                    Direction = direction
                    Location = location
                }

            /// Matches while loops
            | BasicPatterns.WhileLoop(guard:FSharpExpr, body:FSharpExpr) ->
                Tast.WhileLoopExpr {
                    Guard = this.TransformExpr guard
                    Body = this.TransformExpr body
                    Location = location
                }

            /// Matches try/finally expressions
            | BasicPatterns.TryFinally(tryExpr:FSharpExpr, finallyExpr:FSharpExpr) ->
                Tast.TryFinallyExpr {
                    Try = this.TransformExpr tryExpr
                    Finally = this.TransformExpr finallyExpr
                    Location = location
                }

            /// Matches try/with expressions
            | BasicPatterns.TryWith(tryExpr:FSharpExpr, v1: FSharpMemberOrFunctionOrValue, v1Expr: FSharpExpr, v2:FSharpMemberOrFunctionOrValue, handler:FSharpExpr) ->
                Tast.TryWithExpr {
                    Try = this.TransformExpr tryExpr
                    V1 = this.TransformMFVInfo v1
                    V1Expr = this.TransformExpr v1Expr
                    V2 = this.TransformMFVInfo v2
                    Handler = this.TransformExpr handler
                    Location = location
                }

            /// Matches expressions which create an instance of a delegate type
            | BasicPatterns.NewDelegate(ty:FSharpType, expr: FSharpExpr) ->
                Tast.NewDelegateExpr {
                    Expr = this.TransformExpr expr
                    Type = this.TransformFSharpType ty
                    Location = location
                }

            /// Matches expressions which are IL assembly code
            | BasicPatterns.ILAsm(code:string, argTypes:FSharpType list, argExprs:FSharpExpr list) ->
                Tast.ILAsmExpr {
                    Code = code
                    ArgTypes = argTypes |> List.map this.TransformFSharpType
                    ArgExprs = argExprs |> List.map this.TransformExpr
                    Location = location
                }

            /// Matches expressions which fetch a field from a .NET type
            | BasicPatterns.ILFieldGet(objOpt:FSharpExpr option, ty:FSharpType, fieldName: string) ->
                Tast.ILFieldGetExpr {
                    Obj = objOpt |> Option.map this.TransformExpr
                    Type = this.TransformFSharpType ty
                    FieldName = fieldName
                    Location = location
                }

            /// Matches expressions which set a field in a .NET type
            | BasicPatterns.ILFieldSet(objOpt:FSharpExpr option, ty:FSharpType, fieldName:string, setExpr:FSharpExpr) ->
                Tast.ILFieldSetExpr {
                    Obj = objOpt |> Option.map this.TransformExpr
                    Type = this.TransformFSharpType ty
                    FieldName = fieldName
                    SetExpr = this.TransformExpr setExpr
                    Location = location
                }

            /// Matches object expressions, returning the base type, the base call, the overrides and the interface implementations
            | BasicPatterns.ObjectExpr(baseType:FSharpType, baseCall:FSharpExpr, overrides:FSharpObjectExprOverride list, interfaceImplementations: (FSharpType * FSharpObjectExprOverride list) list) ->
                Tast.ObjectExpr {
                    BaseType = this.TransformFSharpType baseType
                    BaseCall = this.TransformExpr baseCall
                    Overrides = overrides |> List.map this.TransformObjectExprOverride
                    InterfaceImplementations =
                        interfaceImplementations
                        |> List.map (fun (ty,overrides) ->
                            let ty = this.TransformFSharpType ty
                            let overrides = overrides |> List.map this.TransformObjectExprOverride
                            {Type=ty; Overrides=overrides}
                            )
                    Location = location
                }

            /// Matches expressions for an unresolved call to a trait
            | BasicPatterns.TraitCall(tys:FSharpType list, name:string, memberFlags:FSharp.Compiler.Ast.MemberFlags, tys2:FSharpType list, argTypes:FSharpType list, argExprs:FSharpExpr list) ->
                let msg = sprintf "BasicPatterns.TraitCall: not implemented at %O" location
                //warn msg
                Tast.UnknownExpression msg

            | pattern ->
                let msg = sprintf "TransformExpr: Expr Pattern not recognized '%A' at %O" pattern location
                warn msg
                Tast.UnknownExpression msg

        // if there is an exception, just swallow it
        with
        | ex ->
            Tast.UnknownExpression (sprintf "Exception: %s" ex.Message)

    member this.TransformField (field:FSharpField) : Tast.FieldDecl =
        {
            Name = field.Name
            Type = this.TransformFSharpType field.FieldType
        }

    member this.TransformUnionCase (case:FSharpUnionCase) : Tast.UnionCase =
        let context = {Name=case.Name; Location= Some (locationOf case.DeclarationLocation) }
        {
            Location = locationOf case.DeclarationLocation
            Name = case.Name
            XmlDoc = getXmlDoc case.XmlDoc
            Accessibility = accessibility case.Accessibility context
            Attributes = this.TransformAttributes case.Attributes
            ReturnType = this.TransformFSharpType case.ReturnType
            Fields = case.UnionCaseFields |> List.ofSeq |> List.map this.TransformField
        }

    member this.TransformObjectExprOverride(objExprOverride: FSharpObjectExprOverride) : Tast.ObjectExprOverride =
        {
            Signature = this.TransformAbstractSignature objExprOverride.Signature
            Body = this.TransformExpr objExprOverride.Body
            GenericParameters = objExprOverride.GenericParameters |> List.map this.TransformGenericParameter
            CurriedParameterGroups = [
                for argList in objExprOverride.CurriedParameterGroups do
                    if argList.Length = 0 then
                        yield Tast.NoParam
                    elif argList.Length = 1 then
                        yield Tast.Param (argList.Head |> this.TransformMFVInfo)
                    else
                        yield Tast.TupleParam [for arg in argList do yield this.TransformMFVInfo arg ]

                ]
        }

    member this.TransformAbstractSignature(signature: FSharpAbstractSignature) : Tast.AbstractSignature =
        {
            Name = signature.Name
            AbstractParameters = [
                for argList in signature.AbstractArguments do
                    if argList.Count = 0 then
                        yield Tast.NoParam
                    elif argList.Count = 1 then
                        yield Tast.Param (argList.[0] |> this.TransformAbstractParameter)
                    else
                        yield Tast.TupleParam [for arg in argList do yield this.TransformAbstractParameter arg ]
                ]
            AbstractReturnType = this.TransformFSharpType signature.AbstractReturnType
        }

    member this.TransformAbstractParameter(parameter: FSharpAbstractParameter) : Tast.AbstractParameter =
        {
            Name = parameter.Name
            Type = this.TransformFSharpType parameter.Type
            Attributes = this.TransformAttributes parameter.Attributes
            IsInArg = parameter.IsInArg
            IsOutArg = parameter.IsOutArg
            IsOptionalArg = parameter.IsOptionalArg
        }
