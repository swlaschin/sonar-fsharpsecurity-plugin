module rec SonarAnalyzer.FSharp.UnitTest.TestCases.S4790_CreatingHashAlgorithms

open System.Security.Cryptography

type TestClass() =

    // RSPEC 4790: https://jira.sonarsource.com/browse/RSPEC-4790
    member this.ComputeHash() =
        // Review all instantiations of classes that inherit from HashAlgorithm, for example:
        let hashAlgo = HashAlgorithm.Create()  // Noncompliant
//                     ^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that hashing data is safe here.}}
        let hashAlgo2 = HashAlgorithm.Create("SHA1") // Noncompliant
//                      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that hashing data is safe here.}}

        let sha = new SHA1CryptoServiceProvider() // Noncompliant
//                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that hashing data is safe here.}}

        let md5 = new MD5CryptoServiceProvider() // Noncompliant
//                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that hashing data is safe here.}}
        ()

    member this.AdditionalTests(sha1:SHA1CryptoServiceProvider ) =
        use myHash = new MyHashAlgorithm()     // Noncompliant
        use myHash = new MyHashAlgorithm(123)      // Noncompliant

        let myHash = MyHashAlgorithm.Create()      // Noncompliant
        let myHash = MyHashAlgorithm.Create(42)    // Noncompliant

        let myHash = MyHashAlgorithm.CreateHash()  // compliant - method name is not Create
        let myHash = MyHashAlgorithm.DoCreate()    // compliant - method name is not Create

        // Other methods are not checked
        let hash = sha1.ComputeHash(null:byte[])
        let hash = sha1.Hash
        let canReuse = sha1.CanReuseTransform
        sha1.Clear()

type MyHashAlgorithm(data:int) =
    inherit HashAlgorithm()  // Noncompliant
//          ^^^^^^^^^^^^^
    new () = new MyHashAlgorithm(1)
    static member Create() : MyHashAlgorithm = failwith "not implemented"
    static member Create(data) : MyHashAlgorithm = failwith "not implemented"

    static member CreateHash() : MyHashAlgorithm = failwith "not implemented"
    static member DoCreate() :MyHashAlgorithm = failwith "not implemented"

    override this.Initialize() = ()
    override this.HashCore(array, ibStart, cbSize) = ()
    override this.HashFinal() = failwith "not implemented"


