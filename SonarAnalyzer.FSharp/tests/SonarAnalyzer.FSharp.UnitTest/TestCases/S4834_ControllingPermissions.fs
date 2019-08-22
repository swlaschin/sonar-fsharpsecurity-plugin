module rec SonarAnalyzer.FSharp.UnitTest.TestCases.S4834_ControllingPermissions

open System
open Microsoft.IdentityModel.Tokens
open System.Security.Permissions
open System.Security.Principal
open System.Threading
open System.Web

module Program =
    open Microsoft.AspNetCore.Http

    type MyIdentity() =

        interface IIdentity with // Noncompliant {{Make sure that permissions are controlled safely here.}}
//                ^^^^^^^^^
            member this.Name = raise (NotImplementedException())
            member this.AuthenticationType = raise (NotImplementedException())
            member this.IsAuthenticated = raise (NotImplementedException())


    type MyPrincipal() =
        interface IPrincipal with // Noncompliant
            member this.Identity = raise (NotImplementedException())
            member this.IsInRole(role) = raise (NotImplementedException())

    // Indirectly implementing IIdentity
    type MyWindowsIdentity() =
        inherit WindowsIdentity("") // Noncompliant

    [<PrincipalPermission(SecurityAction.Demand, Role = "Administrators")>]
    let SecuredMethod() = () // Noncompliant, decorated with PrincipalPermission
//      ^^^^^^^^^^^^^

    let ValidateSecurityToken(handler:SecurityTokenHandler, securityToken:SecurityToken) =
        //handler.ValidateToken(securityToken) // Noncompliant
        ()

    let CreatingPermissions() =
        WindowsIdentity.GetCurrent() |> ignore // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // All instantiations of PrincipalPermission
        let principalPermission = new PrincipalPermission(PermissionState.None) // Noncompliant
        let principalPermission = new PrincipalPermission("", "") // Noncompliant
        let principalPermission = new PrincipalPermission("", "", true) // Noncompliant
        ()

    let HttpContextUser(httpContext:HttpContext) =
        let user = httpContext.User // Noncompliant
//                 ^^^^^^^^^^^^^^^^
        httpContext.User <- user // Noncompliant
//      ^^^^^^^^^^^^^^^^

    let AppDomainSecurity(appDomain:AppDomain, principal:IPrincipal) = // Noncompliant, IPrincipal parameter, see another section with tests
        appDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal) // Noncompliant
//          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        appDomain.SetThreadPrincipal(principal) // Noncompliant
        appDomain.ExecuteAssembly("") // Compliant, not one of the tracked methods

    let ThreadSecurity(principal:IPrincipal) = // Noncompliant, IPrincipal parameter, see another section with tests
        Thread.CurrentPrincipal <- principal // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^
        let principal = Thread.CurrentPrincipal // Noncompliant
        ()


    let CreatingPrincipalAndIdentity(windowsIdentity:WindowsIdentity) =  // Noncompliant, IIdentity parameter, see another section with tests
        let identity = new MyIdentity() // Noncompliant, creation of type that implements IIdentity
//                     ^^^^^^^^^^^^^^^^
        let identity = new WindowsIdentity("") // Noncompliant
        let principal = new MyPrincipal() // Noncompliant, creation of type that implements IPrincipal
        let principal = new WindowsPrincipal(windowsIdentity) // Noncompliant
        ()

    // Method declarations that accept IIdentity or IPrincipal
    let AcceptIdentity(identity:MyIdentity ) = () // Noncompliant
//      ^^^^^^^^^^^^^^
    let AcceptIdentity2(identity:IIdentity) = () // Noncompliant
    let AcceptPrincipal(principal:MyPrincipal) = () // Noncompliant
    let AcceptPrincipal2(principal:IPrincipal) = () // Noncompliant


type Properties() =

    let mutable identity: IIdentity = null

    member this.Identity
        with get() = identity
        and set value = // Compliant, we do not raise for property accessors
            identity <- value
