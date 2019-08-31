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

import org.sonar.api.batch.InstantiationStrategy;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import org.sonar.api.batch.ScannerSide;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;
import org.sonar.plugins.fsharp.utils.OSInfo;
import org.sonar.plugins.fsharp.utils.UnZip;

/*
Extract the FsSolarRunner from the zip file
*/

// adapted from https://github.com/SonarSource/sonar-csharp
@InstantiationStrategy(InstantiationStrategy.PER_BATCH)
@ScannerSide()
public class FsSonarRunnerExtractor {
  public static final Logger LOG = Loggers.get(FsSonarRunnerExtractor.class);
  private static final String SONARQUBE_ANALYZER_EXE = "FsSonarRunner";
  private static final String SONARQUBE_ANALYZER_ZIP = "SonarAnalyzer.FSharp.zip";

  private File file = null;

  public File executableFile(String workDir) throws IOException {
    // once loaded, file is cached between calls
    if (file == null) {
      String filePath;
      switch (OSInfo.getOs()) {
      case WINDOWS:
        filePath = "win-x86" + File.separator + SONARQUBE_ANALYZER_EXE + ".exe";
        break;
      case LINUX:
        filePath = "linux-x86" + File.separator + SONARQUBE_ANALYZER_EXE;
        break;
      default:
        String msg = "Operation system `" + OSInfo.getOs().toString() + "`not supported";
        LOG.error(msg);
        throw new UnsupportedOperationException(msg);
      }

      file = unzipAnalyzerFile(filePath, workDir);
      if (!file.canExecute() && !file.setExecutable(true)) {
        LOG.error("Could not set executable permission");
      }
    }

    return file;
  }

  private File unzipAnalyzerFile(String fileName, String workDir) throws IOException {
    File toolWorkingDir = new File(workDir, "ProjectTools");
    File zipFile = new File(workDir, SONARQUBE_ANALYZER_ZIP);

    if (zipFile.exists()) {
      return new File(toolWorkingDir, fileName);
    }

    try {
      try (InputStream is = getClass().getResourceAsStream("/" + SONARQUBE_ANALYZER_ZIP)) {
        Files.copy(is, zipFile.toPath());
      }

      UnZip unZip = new UnZip();
      unZip.unZipIt(zipFile.getAbsolutePath(), toolWorkingDir.getAbsolutePath());
      return new File(toolWorkingDir, fileName);
    } catch (IOException e) {
      LOG.error("Unable to unzip File: {} => {}", fileName, e.getMessage());
      throw e;
    }
  }
}
