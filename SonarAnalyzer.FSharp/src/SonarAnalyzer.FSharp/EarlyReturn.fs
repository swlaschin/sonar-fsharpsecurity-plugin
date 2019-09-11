module EarlyReturn

// helpers for doing early returns

/// Raise this exeception to return early
exception EarlyReturn

/// Call a option-generating function and if it raises EarlyReturn, return None
let checkWithEarlyReturn f x =
    try
        f x
    with
    | :? EarlyReturn ->
        None

/// Call the first function and if that fails or raises EarlyReturn, call the second function
let orElse f g x =
    match (checkWithEarlyReturn f x) with
    | Some r -> Some r
    | None -> checkWithEarlyReturn g x


