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
import java.net.URL;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;

import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.LogTester;
import org.sonar.api.utils.log.LoggerLevel;

import org.sonar.api.server.profile.BuiltInQualityProfilesDefinition;
import org.sonar.api.server.profile.BuiltInQualityProfilesDefinition.NewBuiltInQualityProfileImpl;

public class FSharpSonarWayProfileTest {

  private static final LogTester logTester = new LogTester();
  private static final String PROFILE_EXAMPLE_PATH = "/profileExample.txt";

  @Test
  public void moreThanOneRuleLoaded() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);
    URL profileUrl = getClass().getResource(PROFILE_EXAMPLE_PATH);

    // Act
    List<String> ruleKeys = new FSharpSonarWayProfile().importRuleKeysFromUrl(profileUrl);

    // Assert
    boolean moreThanOneRule =  ruleKeys.size() > 0;
    assertTrue(moreThanOneRule, "Expecting more than one issue from the example file");
  }

  @Test
  public void checkProfileWorks() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);

    URL profileUrl = getClass().getResource(PROFILE_EXAMPLE_PATH);

    BuiltInQualityProfilesDefinition.Context context = new BuiltInQualityProfilesDefinition.Context();

    // Act
    new FSharpSonarWayProfile().defineFromUrl(context, profileUrl);

    // Assert
    // just make sure it doesn't crash
  }

}
