# Building, Testing and Debugging the SonarQube plugin



## Getting the code

* Clone [this repository](https://github.com/swlaschin/sonar-fsharpsecurity-plugin.git)
* Download sub-modules `git submodule update --init --recursive`

## To build and test

* build the Java plugin .JAR file (from the root)

```
mvn clean install
```

To run the Java unit tests:

```
mvn clean test
```


## Developing with VS Code

Install:

* Language Support for Java by Red Hat -- [redhat.java](https://marketplace.visualstudio.com/items?itemName=redhat.java)
* Microsoft Debugger for Java -- [vscjava.vscode-java-debug](https://marketplace.visualstudio.com/items?itemName=vscjava.vscode-java-debug)

To debug a plugin, see [the instructions on the SonarQube site](https://docs.sonarqube.org/latest/extend/developing-plugin/)

## Developing with Eclipse or IntelliJ

When working with Eclipse or IntelliJ please follow the [sonar guidelines](https://github.com/SonarSource/sonar-developer-toolset)

## Understanding the Sonar Plugin API

See http://javadocs.sonarsource.org/7.9.1/apidocs/


## Contributing

Please see [Contributing Code](../CONTRIBUTING.md) for details on contributing changes back to the code.
