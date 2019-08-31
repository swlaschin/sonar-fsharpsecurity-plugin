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

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;
import org.sonar.api.server.rule.RulesDefinition.Context;

import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.LogTester;
import org.sonar.api.utils.log.LoggerLevel;

public class FSharpSonarRulesDefinitionTest {
  private static final String rulesFile = "/rulesExample.xml";
  private static final LogTester logTester = new LogTester();

  @Test
  public void repositories_exactlyOne() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);
    Context context = new Context();
    assertEquals(0, context.repositories().size());

    // Act
    new FSharpSonarRulesDefinition().defineFromFile(context,rulesFile);

    // Assert
    assertEquals(1, context.repositories().size());
  }

  @Test
  public void repository_expectedNameAndKey() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);
    Context context = new Context();

    // Act
    new FSharpSonarRulesDefinition().defineFromFile(context,rulesFile);

    // Assert
    assertEquals(FSharpPlugin.REPOSITORY_NAME, context.repositories().get(0).name());
    assertNotNull(context.repository(FSharpPlugin.REPOSITORY_KEY));
  }

  @Test
  public void moreThanOneRuleLoaded() {
    // Arrange
    logTester.setLevel(LoggerLevel.DEBUG);
    Context context = new Context();
    assertEquals(0, context.repositories().size());

    // Act
    new FSharpSonarRulesDefinition().defineFromFile(context,rulesFile);

    // Assert
    boolean moreThanOneRule = context.repository(FSharpPlugin.REPOSITORY_KEY).rules().size() > 0;
    assertTrue(moreThanOneRule, "Expecting more than one rule");
  }
}
