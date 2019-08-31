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

/*
Represents an issue detected by the scanner.

Designed to be easily compatible with http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/batch/sensor/issue/NewIssue.html
*/
public class FSharpIssue {

    private final String ruleKey;
    private final String message;
    private final String absoluteFilePath;
    private final int startLine;
    private final int startLineOffset;
    private final int endLine;
    private final int endLineOffset;

  public FSharpIssue(String ruleKey,  String message, String absoluteFilePath, int startLine, int startLineOffset, int endLine, int endLineOffset) {
    this.ruleKey = ruleKey;
    this.message = message;
    this.absoluteFilePath = absoluteFilePath;
    this.startLine = startLine;
    this.startLineOffset = startLineOffset;
    this.endLine = endLine;
    this.endLineOffset = endLineOffset;
  }

  public String ruleKey() {
    return ruleKey;
  }

  public String message() {
    return message;
  }

  public String absoluteFilePath() {
    return absoluteFilePath;
  }

  public int startLine() {
    return startLine;
  }

  public int startLineOffset() {
    return startLineOffset;
  }

  public int endLine() {
    return endLine;
  }

  public int endLineOffset() {
    return endLineOffset;
  }

}
