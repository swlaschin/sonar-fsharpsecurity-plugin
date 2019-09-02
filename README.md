# sonar-fsharpsecurity-plugin

sonar-fsharpsecurity-plugin is a F# plugin for SonarQube focused on security/vuln scanning only.

## Rationale

Many enterprises use the SonarC# and SonarVB static code analysers for scanning C# and VB.NET code to check for security and vulnerability issues.

In some cases, this scanning is *required* before deployment. For another .NET language such as F# to be accepted at these companies, an equivalent scanning tool is required.
The lack of such a tool is a hard blocker for F# acceptance.

SonarQube themselves have not built an F# plugin, hence this project.

The code is closely based on the C# code at https://github.com/SonarSource/sonar-dotnet.
It uses exactly the same test suites (translated to F#) and the same rules (translated to F#).
This is to short circuit any complaints about the logic used. If it's good enough for C#, it's good enough for F#!

## Features

* 19 "Security Hotspot" rules have been ported from C# ([C# rules here](https://rules.sonarsource.com/csharp)).
* 26 "Vulnerabilities" rules are coming soon.


## How to run SonarQube locally 

NOTE: In order to run SonarQube, you will need a recent version of the JDK (v11 or newer). If you don't have it, follow instructions in [building, testing and debugging the Java plugin](./docs/contributing-plugin.md).

Install SonarQube:

1. Install the Community Edition version of SonarQube. [Instructions here](https://docs.sonarqube.org/latest/setup/get-started-2-minutes/).
1. Run the server with `StartSonar.bat` and make sure you can see the site at http://localhost:9000. Make sure `JAVA_HOME` or equivalent is set.

Install the plugin:

1. Download [the plugin `sonar-fsharpsecurity-plugin.jar` file from Appveyor](https://ci.appveyor.com/project/swlaschin/sonar-fsharpsecurity-plugin/build/artifacts).
1. Shut down SonarQube, then copy the plugin `sonar-fsharpsecurity-plugin.jar` file to the [SonarQube plugins directory](https://docs.sonarqube.org/latest/setup/install-plugin/) and restart SonarQube.

Prepare for using SonarScanner:

1. [Get a user token, aka login key](https://docs.sonarqube.org/latest/user-guide/user-token/).
1. Install [SonarScanner](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/).

Now you can try running the scanner!

1. In the SonarQube UI, create a project such as `myProject`.
1. Go to the directory containing the F# project
1. Run the following (assumes that `sonar-scanner.bat` is not already on your path and your login token is `01234567890`)

```
set JAVA_HOME=path\to\jdk // optional
set SONARSCANNER=path\to\sonar-scanner\bin
%SONARSCANNER%\sonar-scanner.bat -D"sonar.projectKey=myProject" -D"sonar.sources=." -D"sonar.host.url=http://localhost:9000" -D"sonar.login=01234567890"
```

You can eliminate the need for the `host.url` and `login` parameters by editing `$install_directory/conf/sonar-scanner.properties`. 
([Instructions](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/)).

## To run directly without using the SonarQube server

The plugin contains an executable (`FsSonarRunner`) that can be run on its own. To use this:

1. Download the plugin as described above
1. Unzip the .JAR file to reveal the `SonarAnalyzer.FSharp.zip` 
1. Unzip `SonarAnalyzer.FSharp.zip` to reveal a `win-x86` directory. 
1. Copy this directory to your favorite location.

To run the scanner, just do:

```
FsSonarRunner
```

This will show the available command line options.

As a demonstration, try running it on the test cases which are part of the test suite in [this repository](https://github.com/swlaschin/sonar-fsharpsecurity-plugin.git).

```
FsSonarRunner -d .\SonarAnalyzer.FSharp\tests\SonarAnalyzer.FSharp.UnitTest\TestCases
```

The output file (`sonarDiagnostics.xml`) will be written to that directory.

## Have question or feedback?

To provide feedback (request a feature, report a bug etc.), simply
[create a GitHub Issue](https://github.com/swlaschin/sonar-fsharpsecurity-plugin/issues/new).

## Building, testing and debugging locally

If you would like to build or modify the code, see the instructions at:

* [Building, testing and debugging the Java plugin](./docs/contributing-plugin.md)
* [Building, testing and debugging the F# analyzer](./docs/contributing-analyzer.md)

## How to contribute

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## Acknowledgments

Massive thanks to [jmecosta](https://github.com/jmecosta) and [milbrandt](https://github.com/milbrandt) for creating 
the [fslint SonarQube F# plugin](https://github.com/jmecsoftware/sonar-fsharp-plugin). I copied all the Java and maven code from that project
and I would never have been able to implement this plugin without that as an example!

## License

Licensed under the GPL. See LICENSE.txt.