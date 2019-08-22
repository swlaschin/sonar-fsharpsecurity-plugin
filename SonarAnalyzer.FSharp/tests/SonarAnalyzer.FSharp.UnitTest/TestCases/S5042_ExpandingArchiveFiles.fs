module SonarAnalyzer.FSharp.UnitTest.TestCases.S5042_ExpandingArchiveFiles

open System
open System.IO
open System.IO.Compression
open System.Linq

type Class1() =

    member this.ExtractArchive(archive:ZipArchive) =
        for entry in archive.Entries do
            entry.ExtractToFile("") // Noncompliant
//          ^^^^^^^^^^^^^^^^^^^^^^^

        for i = 0 to archive.Entries.Count - 1 do
            archive.Entries.[i].ExtractToFile("") // Noncompliant

        archive.Entries.ToList()
        |> Seq.iter (fun e -> e.ExtractToFile("")) // Noncompliant
//                            ^^^^^^^^^^^^^^^^^^^

    member this.ExtractEntry(entry:ZipArchiveEntry) =
        entry.ExtractToFile("") // Noncompliant
        entry.ExtractToFile("", true) // Noncompliant

        ZipFileExtensions.ExtractToFile(entry, "") // Noncompliant
        ZipFileExtensions.ExtractToFile(entry, "", true) // Noncompliant

        let stream = entry.Open() // Noncompliant

        entry.Delete() // Compliant, method is not tracked

        let fullName = entry.FullName // Compliant, properties are not tracked

        this.ExtractToFile(entry) // Compliant, method is not tracked

        //this.Invoke(ZipFileExtensions.ExtractToFile) // Compliant, not an invocation, but could be considered as FN

    member this.ExtractToFile(entry:ZipArchiveEntry) = ()

    member this.Invoke(action: Action<ZipArchiveEntry, string> ) = ()
