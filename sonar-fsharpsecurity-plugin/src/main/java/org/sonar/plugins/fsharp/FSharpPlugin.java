/*
 * Sonar FSharpSecurity Plugin, open source software quality management tool.
 *
 * Sonar FSharpSecurity Plugin is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * Sonar FSharpSecurity Plugin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 */
package org.sonar.plugins.fsharp;

import org.sonar.api.Properties;
import org.sonar.api.Property;

import org.sonar.api.Plugin;

/*
This class is the top-level plugin class, called by SonarScanner.
It installs some extensions, listed below.

All code is in this project is adaptedwith gratitude from
https://github.com/jmecsoftware/sonar-fsharp-plugin
https://github.com/SonarSource/sonar-csharp
*/


@Properties({
  @Property(
    key = FSharpPlugin.FILE_SUFFIXES_KEY,
    defaultValue = FSharpPlugin.FILE_SUFFIXES_DEFVALUE,
    name = "File suffixes",
    description = "Comma-separated list of suffixes of files to analyze.",
    project = true, global = true
  )
})
public class FSharpPlugin implements Plugin {

  public static final String LANGUAGE_KEY = "fs";
  public static final String LANGUAGE_NAME = "F#";

  public static final String FILE_SUFFIXES_KEY = "sonar.fs.file.suffixes";
  public static final String FILE_SUFFIXES_DEFVALUE = ".fs,.fsx,.fsi";

  public static final String FSHARP_WAY_PROFILE = "Sonar way";

  public static final String REPOSITORY_KEY = "fsharpsecurity";
  public static final String REPOSITORY_NAME = "SonarQube";

  @Override
  public void define(Context context) {
    context.addExtension(FSharpLanguage.class);          // the F# language properties
    context.addExtension(FSharpSonarRulesDefinition.class);  // the list of rules available
    context.addExtension(FSharpSonarWayProfile.class);   // the quality profile
    context.addExtension(FsSonarRunnerExtractor.class);  // a utility to unzip the F# executable
    context.addExtension(FSharpSensor.class);            // the main analyzer
  }
}
