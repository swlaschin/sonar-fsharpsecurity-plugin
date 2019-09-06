module Attempt

(*
Logic for Attempt and Attempt computation expression.

An Attempt is a thunk that returns a Result.
That is, it has the shape:  unit -> Result<_,_>

*)

open System

type Attempt<'S,'F> = Attempt of (unit -> Result<'S,'F>)

/// Unwrap an attempt and run the inner function to get a Result
let runAttempt (Attempt a) =
    a()

/// Wrap a function f in the Attempt type
let wrap f =
    Attempt f

/// Lift a value into an Attempt with Result.Ok = x
let succeed x =
    (fun () -> Ok x) |> wrap

/// Lift a value into an Attempt with Result.Error = x
let failed err =
    (fun () -> Error err) |> wrap

/// The utility functions for Attempt
module private Util =

    /// Delay an attempt by wrapping it a thunk
    let delay f : Attempt<_,_> =
        Attempt (fun () -> f() |> runAttempt)

    /// Chain an attempt into a attempt-returning function,
    /// but only if the attempt is successful.
    let bind onSuccess input : Attempt<_,_> =
        match runAttempt input with
        | Ok s -> onSuccess s
        | Error f -> failed f

open Util

type AttemptBuilder() =

    member this.Bind(m : Attempt<_, _>, success) =
        bind success m

    member this.Bind(m : Result<_, _>, success) =
        bind success (Attempt (fun () -> m))

    member this.Bind(m : Result<_, _> option, success) =
        match m with
        | None -> this.Combine(this.Zero(), success)
        | Some x -> this.Bind(x, success)

    member this.Return(x) : Attempt<_, _> =
        succeed x

    member this.ReturnFrom(x : Attempt<_, _>) =
        x

    member this.Combine(v, f) : Attempt<_, _> =
        bind f v

    member this.Yield(x) =
        Ok x

    member this.YieldFrom(x) =
        x

    member this.Delay(f) : Attempt<_, _> =
        delay f

    member this.Zero() : Attempt<_, _> =
        succeed ()

    member this.While(guard, body: Attempt<_, _>) =
        if not (guard())
        then this.Zero()
        else this.Bind(body, fun () ->
            this.While(guard, body))

    member this.TryWith(body, handler) =
        try this.ReturnFrom(body())
        with e -> handler e

    member this.TryFinally(body, compensation) =
        try this.ReturnFrom(body())
        finally compensation()

    member this.Using(disposable:#System.IDisposable, body) =
        let body' = fun () -> body disposable
        this.TryFinally(body', fun () ->
            match disposable with
                | null -> ()
                | disp -> disp.Dispose())

    member this.For(sequence:seq<'a>, body: 'a -> Attempt<_,_>) =
        this.Using(sequence.GetEnumerator(),fun enum ->
            this.While(enum.MoveNext,
                this.Delay(fun () -> body enum.Current)))

/// The "attempt" computation expression
let attempt = new AttemptBuilder()
