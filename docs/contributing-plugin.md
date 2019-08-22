# Building, Testing and Debugging the SonarQube plugin

## Working with the code

1. Clone [this repository](https://github.com/swlaschin/sonar-fsharpsecurity-plugin.git)
1. Download sub-modules `git submodule update --init --recursive`
1. Build the F# code (from the root)

```
dotnet build .\SonarAnalyzer.FSharp\SonarAnalyzer.FSharp.sln
```

1. Export the rules as xml files and copy them to the java project

```
dotnet run .\SonarAnalyzer.FSharp\src\FsSolarRunner -- -e 
xcopy .\SonarAnalyzer.FSharp\src\FsSolarRunner\fsharp 
```

1. Build the Java plugin .JAR file (from the root)

``
mvn clean install
```


## Developing with Eclipse or IntelliJ

When working with Eclipse or IntelliJ please follow the [sonar guidelines](https://github.com/SonarSource/sonar-developer-toolset)

## Running Tests

To run the F# unit tests:

```
dotnet test .\SonarAnalyzer.FSharp\SonarAnalyzer.FSharp.sln
```

To run the Java unit tests:

```
mvn clean test
```


## Contributing

Please see [Contributing Code](../CONTRIBUTING.md) for details on contributing changes back to the code.
