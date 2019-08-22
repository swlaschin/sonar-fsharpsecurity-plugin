module SonarAnalyzer.FSharp.UnitTest.TestCases.S4787_EncryptingData


open System
open System.Security.Cryptography

// RSPEC example: https://jira.sonarsource.com/browse/RSPEC-4938
type MyClass() =

    member this.Main() =
        let data :Byte[]  = [| 1uy; 1uy; 1uy |]

        let myRSA = RSA.Create()
        let padding = RSAEncryptionPadding.CreateOaep(HashAlgorithmName.SHA1)

        // Review all base RSA class' Encrypt/Decrypt calls
        myRSA.Encrypt(data, padding) |> ignore   // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^ {{Make sure that encrypting data is safe here.}}
        myRSA.EncryptValue(data)     |> ignore  // Noncompliant
        myRSA.Decrypt(data, padding) |> ignore // Noncompliant
        myRSA.DecryptValue(data)     |> ignore  // Noncompliant

        let myRSAC = new RSACryptoServiceProvider()
        // Review the use of any TryEncrypt/TryDecrypt and specific Encrypt/Decrypt of RSA subclasses.
        myRSAC.Encrypt(data, false) |> ignore    // Noncompliant
        myRSAC.Decrypt(data, false) |> ignore   // Noncompliant


        // Note: TryEncrypt/TryDecrypt are only in .NET Core 2.1+
        //            myRSAC.TryEncrypt(data, Span<byte>.Empty, padding, out written) // Non compliant
        //            myRSAC.TryDecrypt(data, Span<byte>.Empty, padding, out written) // Non compliant

        let rgbKey : byte[] = [| 1uy; 2uy; 3uy |]
        let rgbIV : byte[] = [| 4uy; 5uy; 6uy |]
        let rijn = SymmetricAlgorithm.Create()
        // Review the creation of Encryptors from any SymmetricAlgorithm instance.
        rijn.CreateEncryptor() |> ignore   // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^ {{Make sure that encrypting data is safe here.}}
        rijn.CreateEncryptor(rgbKey, rgbIV) |> ignore      // Noncompliant
        rijn.CreateDecryptor()              |> ignore      // Noncompliant
        rijn.CreateDecryptor(rgbKey, rgbIV) |> ignore      // Noncompliant


type MyAsymmetricCrypto() = // Noncompliant
    inherit System.Security.Cryptography.AsymmetricAlgorithm()


type MySymmetricCrypto() = // Noncompliant
    inherit System.Security.Cryptography.SymmetricAlgorithm()

    override this.CreateDecryptor(rgbKey, rgbIV) = null
    override this.CreateEncryptor(rgbKey, rgbIV) = null
    override this.GenerateIV() = ()
    override this.GenerateKey() = ()

type MyRSA() =
    inherit System.Security.Cryptography.RSA() // Noncompliant

    // Dummy methods with the same names as the additional methods added in Net Core 2.1.
    member this.TryEncrypt() = ()
    member this.TryEncrypt(dummyMethod) = ()
    member this.TryDecrypt() = ()
    member this.TryDecrypt(dummyMethod) = ()

    member this.OtherMethod() = ()

    // Abstract methods
    override this.ExportParameters(includePrivateParameters) = new RSAParameters()
    override this.ImportParameters(parameters) = ()


type Class2() =

    member this.AdditionalTests(data:Byte[], padding:RSAEncryptionPadding) =
        let customAsymProvider = new MyRSA()

        // Should raise on derived asymmetric classes
        customAsymProvider.Encrypt(data, padding)  |> ignore // Noncompliant
        customAsymProvider.EncryptValue(data)      |> ignore // Noncompliant
        customAsymProvider.Decrypt(data, padding)  |> ignore // Noncompliant
        customAsymProvider.DecryptValue(data)      |> ignore // Noncompliant

        // Should raise on the Try* methods added in NET Core 2.1
        // Note: this test is cheating - we can't currently referencing the
        // real 2.1 assemblies since the test project is targetting an older
        // NET Framework, so we're testing against a custom subclass
        // to which we've added the new method names.
        customAsymProvider.TryEncrypt()        // Noncompliant
        customAsymProvider.TryEncrypt(null)    // Noncompliant
        customAsymProvider.TryDecrypt()        // Noncompliant
        customAsymProvider.TryDecrypt(null)    // Noncompliant

        customAsymProvider.OtherMethod()

        // Should raise on derived symmetric classes
        let customSymProvider = new MySymmetricCrypto()
        customSymProvider.CreateEncryptor() |> ignore   // Noncompliant
        customSymProvider.CreateDecryptor() |> ignore   // Noncompliant

