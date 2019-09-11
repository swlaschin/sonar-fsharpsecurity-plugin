namespace global

open System

type OptionBuilder() =
    member __.Return(x) = Some x
    member __.ReturnFrom(x) = x
    member __.Bind(x,f) = x |> Option.bind f
    member __.Zero() = Some ()
    member __.Combine(m, f) = Option.bind f m
    member __.Delay(f: unit -> _) = f
    member __.Run(f) = f()
    member this.TryWith(m, h) =
        try this.ReturnFrom(m)
        with e -> h e

    member this.TryFinally(m, compensation) =
        try this.ReturnFrom(m)
        finally compensation()

    member this.Using(res:#IDisposable, body) =
        this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member this.While(guard, f) =
        if not (guard()) then Some () else
        do f() |> ignore
        this.While(guard, f)

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
            fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

/// open this module to get access to the "option" computation expression
module OptionBuilder =
    let option = OptionBuilder()


