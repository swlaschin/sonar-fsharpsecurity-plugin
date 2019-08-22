module rec SonarAnalyzer.FSharp.UnitTest.TestCases.S4818_SocketsCreation

open System.Net.Sockets

type TestSocket() =

    // RSpec example: https://jira.sonarsource.com/browse/RSPEC-4944
    static member Run() =
        let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) // Noncompliant
//                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ {{Make sure that sockets are used safely here.}}

        // TcpClient and UdpClient simply abstract the details of creating a Socket
        let client = new TcpClient("example.com", 80)  // Noncompliant
//                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^  {{Make sure that sockets are used safely here.}}

        let listener = new UdpClient(80)  // Noncompliant
//                     ^^^^^^^^^^^^^^^^^  {{Make sure that sockets are used safely here.}}
        ()

    member this.Tests(socket:Socket, tcp:TcpClient, udp:UdpClient) =
        // Ok to call other methods and properties
        socket.Accept() |> ignore
        let isAvailable = tcp.Available
        udp.DontFragment <- true

        // Creating of subclasses is not checked
        let s = new MySocket()
        let s = new MyTcpClient()
        let s = new MyUdpClient()
        ()

type MySocket() =
    inherit Socket(new SocketInformation())

type MyTcpClient() =
    inherit TcpClient()

type MyUdpClient() =
    inherit UdpClient()

