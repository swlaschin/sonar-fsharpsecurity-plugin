# sonar-fsharp-security

sonar-fsharp-security is a F# plugin for SonarQube. It contain rules for security/vuln scanning only.

## Rationale

Many enterprises use the SonarC# and SonarVB [static code analysers] for scanning C# and VB.NET code to check for security and vulnerability issues.

In some cases, this scanning is *required* before deployment. For another .NET language like F# to be accepted at these companies, an equivalent scanning tool is required.
The lack of such a tool is a hard blocker for F# acceptance.

SonarQube themselves have not built an F# plugin, hence this project.

The code is closely based on the C# code at https://github.com/SonarSource/sonar-dotnet. It uses exactly the same test suites (translated to F#) and the same rules (translated to F#).
This is to short circuit any complaints about the logic used. If it's good enough for C#, it's good enough for F#.

## Features

* 19 "Security Hotspot" rules have been ported from C# ([C# rules here](https://rules.sonarsource.com/csharp))
* 26 "Vulnerabilities" rules are coming soon

## How to test locally 

1. Install the demo version of SonarQube. [Instructions here](https://docs.sonarqube.org/latest/setup/get-started-2-minutes/)
1. Run the server with `StartSonar.bat` and make sure you can see the site at http://localhost:9000 
1. [Get a user token, aka login key](https://docs.sonarqube.org/latest/user-guide/user-token/)
1. Download [the plugin .jar file from Appveyor](https://ci.appveyor.com/project/swlaschin/sonar-fsharpsecurity-plugin-wxq94/build/artifacts)
1. Copy the plugin .jar file to the [SonarQube plugins directory](https://docs.sonarqube.org/latest/setup/install-plugin/) and restart SonarQube
1. Install [SonarScanner](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/)

Now you can try running the scanner!

1. Create a project in the SonaQube UI
1. Go to the directory containing the F# project
1. Run the following (assumes that `sonar-scanner.bat` in on your path and your login token is `01234567890`)

```
sonar-scanner.bat -D"sonar.projectKey=myProject" -D"sonar.sources=." -D"sonar.host.url=http://localhost:9000" -D"sonar.login=01234567890"
```

You can eliminate the need for the `host.url` and `login` parameters by editing `$install_directory/conf/sonar-scanner.properties`. 
([Instructions](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/))

## Have question or feedback?

To provide feedback (request a feature, report a bug etc.), simply
[create a GitHub Issue](https://github.com/swlaschin/sonar-fsharpsecurity-plugin/issues/new).

## Compiling locally

The plugin is a mix of Java (under `sonar-fsharpsecurity-plugin`) and F# (under `SonarAnalyzer.FSharp`).  

The plugin itself is written in Java and is loaded when `sonar-scanner` is used. The way is works is that sonar provides a number of abstract classes which
the plugin then implements. In this case the plugin executes the F# executable, which dumps out the results as files.
he plugin then reads these files in and stores them in the database associated with the server.


* [Building, testing and debugging the Java plugin](./docs/contributing-plugin.md)
* [Building, testing and debugging the F# analyzer](./docs/contributing-analyzer.md)
* [Using the rspec.ps1 script](./scripts/rspec/README.md)

## How to contribute

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## Acknowledgments

Massive thanks to [jmecosta](https://github.com/jmecosta) and [milbrandt](https://github.com/milbrandt) for creating 
the [fslint SonarQube F# plugin](https://github.com/jmecsoftware/sonar-fsharp-plugin). I copied all the Java and maven code from that project
and I would never have been able to implement this plugin without that as an example!


## License

Licensed under GPL. See LICENSE.txt.