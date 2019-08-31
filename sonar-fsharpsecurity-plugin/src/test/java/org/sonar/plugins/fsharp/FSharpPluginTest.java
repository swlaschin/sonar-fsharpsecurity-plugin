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
import static org.mockito.Mockito.mock;

import org.junit.jupiter.api.Test;
import org.sonar.api.Plugin;
import org.sonar.api.SonarRuntime;


/*
Check that 5 extensions are loaded in FSharpPlugin
*/

public class FSharpPluginTest {
  @Test
  public void addExtensions_expectedNumber() {
    // Arrange
    Plugin.Context context = new Plugin.Context(mock(SonarRuntime.class));
    FSharpPlugin plugin = new FSharpPlugin();

    // Act
    plugin.define(context);

    // Assert
    assertEquals(5, context.getExtensions().size());
  }
}
