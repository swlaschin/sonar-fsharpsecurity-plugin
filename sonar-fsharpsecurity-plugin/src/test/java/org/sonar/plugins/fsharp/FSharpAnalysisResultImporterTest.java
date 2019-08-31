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

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Paths;
import java.util.Base64;
import java.util.Collection;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Map.Entry;
import java.util.List;
import java.util.ArrayList;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;

import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.LogTester;
import org.sonar.api.utils.log.LoggerLevel;

public class FSharpAnalysisResultImporterTest {

  private static final LogTester logTester = new LogTester();
  private static final String analysisOutput = "/sonarDiagnosticsExample.xml";

  @Test
  public void moreThanOneIssueLoaded() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);
    File file = new File(getClass().getResource(analysisOutput).getFile());

    // Act
    List<FSharpIssue> issues = new FSharpAnalysisResultImporter().parse(file);

    // Assert
    boolean moreThanOneRule = issues.size() > 0;
    assertTrue(moreThanOneRule, "Expecting more than one issue from the example file");
  }
}
