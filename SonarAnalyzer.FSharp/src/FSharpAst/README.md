# FSharpAst

**WORK IN PROGRESS**

**This project is still being worked on. It will be part of `SonarAnalyzer.FSharp` until it becomes stable!**

The project aims to make the Fsharp Compiler Services easier to understand and use by:

* Creating a set of AST types that well documented and are easier to understand 
* A process to convert the FSC types into this AST
* Tools to work with the new AST

## Why do this?

The FSC classes are designed for efficiency but are quite hard to understand because their design
is mixed up with the implementation of the actual compiler.
There are some helpers in the form of Active Patterns but these are not well documented.

Some benefits of having a "domain-only" AST:

* The entire AST is in one file, making it easier to understand and document.
* Because this AST uses DUs rather than active patterns, we get the benefits of exhaustive pattern matching.
* Finally, the names of the fields in the AST have been tweaked to make them more consistent, again to aid understanding.

## What are the downsides?

This AST is better for batch-style processing. It is not meant to be used when high performance is needed.
So for interactive tooling (such as Ionide) the original FSC is better.

However, it follows the FSC model reasonably closely, so if you understand this AST, it is a good
launching point to understand the real one.

## Potential uses

* To implement batch-oriented source analyzers. 
  In fact, the motivation for this project came from implementing a F# SonarQube scanner.
* To implement a transpiler. E.g from F# to C#. 
  The Fable implementation uses a similar approach of creating an intermediate AST.

## Notes on the design and implementation

### Concepts

The overall hierarchy of the AST is:

* A File contains "Declarations"
* A Declaration is either an "Entity" (Namespace, Module, or Type) or a "Member, Function, or Value"
* A Namespace contains a list of sub Declarations 
* A Module contains a list of sub Declarations 
* A "Member, Function, or Value" (or MFV for short) is the name given to any declaration that contains code.
  (as opposed to data declarations such as types, or containers such as modules).
  An MFV has a name, attributes, etc, and most importantly, a "Body" which is an "Expression".
  * Property members are compiled into two separate methods: `get_MyProp` and `set_MyProp`.
  * Top level values in a module are similar to read-only properties on a static class, 
    but are compiled without the "get_" prefix. 
    So just `myValue` rather than `get_myValue`. 

* An "Expression" represents executable code. There are lots of different kinds of expressions, such as constant expressions,
  calls, new object creation, etc. See the `Expression` DU for the complete list.
* Expressions generally contain sub-expressions. For example, in a function call, the arguments are themselves expressions,
  and so on.

### Abbreviations

Common abbreviations are:

* `expr` for Expression
* `ty` or `t` for Type
* `arg` or `a` for Argument
* `param` or `p` for Parameter
* `prop` for Property
* `mfv` for "Member, Function, or Value"
* `m` for Member


### Naming conventions

To assist in understandability, some common conventions are used:

* "Parameters" vs "arguments". In this design a "parameter" is what is used at the declaration site. 
  An argument is what is used at the call site. In some cases, the number of parameters is not the same 
  as the number of arguments.
* "typeArg" vs "argType". 
   A `typeArg` is an argument applied to a generic type. E.g in `List<int>`, `int` is the type arg.
   An `argType` is the type of an argument. E.g in `x(1)`, `1` is the first arg, and `int` is the corresponding argType.
