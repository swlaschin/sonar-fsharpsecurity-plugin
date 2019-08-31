# Building, Testing and Debugging the F# Analyzer

The F# analyzer can be run as a standalone executable, outside of Sonar.

This can be useful for local testing before committing, or as a commit hook, etc.

## Getting the code

* Clone [this repository](https://github.com/swlaschin/sonar-fsharpsecurity-plugin.git)

## To build and test

The root of the F# code is `./SonarAnalyzer.FSharp`

So to build, change to that directory and build as normal.

```
cd .\SonarAnalyzer.FSharp
dotnet build SonarAnalyzer.FSharp.sln
```

To run the F# unit tests:

```
dotnet test SonarAnalyzer.FSharp.sln
```

## To run against F# code without using the Sonar server

The main executable is `FsSonarRunner`, so to run it on its own:

```
cd .\SonarAnalyzer.FSharp\src\FsSonarRunner
dotnet run 
```

This will show the available options.

As a demonstration, try running it on the test cases which are part of the test suite

```
cd .\SonarAnalyzer.FSharp\src\FsSonarRunner
dotnet run -- -d ..\..\tests\SonarAnalyzer.FSharp.UnitTest\TestCases
```

To convert the dotnet executable into a standalone:

```
cd .\SonarAnalyzer.FSharp\src\FsSonarRunner
dotnet publish --output publish/<platform> --runtime <platform>
```

where `<platform>` is `win-x64`, `linux-x64`, etc


## Contributing

Please see [Contributing Code](../CONTRIBUTING.md) for details on contributing changes back to the code.

