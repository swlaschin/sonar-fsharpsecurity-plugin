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
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.mock;

import org.junit.jupiter.api.Test;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.sensor.Sensor;
import org.sonar.api.batch.sensor.internal.DefaultSensorDescriptor;
import org.sonar.api.issue.NoSonarFilter;
import org.sonar.api.measures.FileLinesContextFactory;

import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.LogTester;
import org.sonar.api.utils.log.LoggerLevel;

/*
Check that the analyzer works.

The real unit tests are in the F# code, so we just need to check that it can be called without crashing.
*/

public class FSharpSensorTest {

    private static final LogTester logTester = new LogTester();


    @Test
    public void describe_languageAndKey_asExpected() {
        // Arrange
        logTester.setLevel(LoggerLevel.DEBUG);
        FsSonarRunnerExtractor extractor = new FsSonarRunnerExtractor();
        FileSystem fs = mock(FileSystem.class);
        FileLinesContextFactory fileLinesContextFactory = mock(FileLinesContextFactory.class);
        NoSonarFilter noSonarFilter = new NoSonarFilter();
        Sensor sensor = new FSharpSensor(extractor, fs, fileLinesContextFactory, noSonarFilter);

        DefaultSensorDescriptor descriptor = new DefaultSensorDescriptor();

        // Act
        sensor.describe(descriptor);

        // Assert
        assertEquals(FSharpPlugin.LANGUAGE_NAME, descriptor.name());
        assertEquals(1, descriptor.languages().size());
        assertTrue(descriptor.languages().contains(FSharpPlugin.LANGUAGE_KEY), "LANGUAGE_KEY not found");
    }
}
