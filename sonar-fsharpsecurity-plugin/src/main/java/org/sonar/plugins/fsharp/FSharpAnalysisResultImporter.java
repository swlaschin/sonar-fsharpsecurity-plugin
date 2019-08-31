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

import com.google.common.base.Charsets;
import com.google.common.base.Throwables;
import com.google.common.io.Closeables;

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

import org.apache.commons.lang.StringEscapeUtils;
import org.apache.commons.lang.StringUtils;
import org.sonar.api.batch.DependedUpon;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.rule.ActiveRule;
import org.sonar.api.batch.sensor.Sensor;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonar.api.batch.sensor.cpd.NewCpdTokens;
import org.sonar.api.batch.sensor.highlighting.NewHighlighting;
import org.sonar.api.batch.sensor.highlighting.TypeOfText;
import org.sonar.api.batch.sensor.issue.NewIssue;
import org.sonar.api.batch.sensor.issue.NewIssueLocation;
import org.sonar.api.issue.NoSonarFilter;
import org.sonar.api.measures.CoreMetrics;
import org.sonar.api.measures.FileLinesContext;
import org.sonar.api.measures.FileLinesContextFactory;
import org.sonar.api.rule.RuleKey;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;


/*
The importer for analysis results. It parses an XML file and returns a list of FSharpIssues

IMPORTANT: the schema must match the one defined by the F# code in FsSonarRunner/DiagnosticsDto

*/


class FSharpAnalysisResultImporter {

    private static final Logger LOG = Loggers.get(FSharpAnalysisResultImporter.class);

    public FSharpAnalysisResultImporter() {

    }

    public List<FSharpIssue> parse(File file) {
        return new Parser().parse(file);
    }

    private static class Parser {
        private File file;
        private XMLStreamReader xmlReader;
        private ArrayList<FSharpIssue> issues = new ArrayList<FSharpIssue>();

        public List<FSharpIssue> parse(File file) {
            this.file = file;

            InputStreamReader reader = null;
            XMLInputFactory xmlFactory = XMLInputFactory.newInstance();

            try {
                logInfo("Reading analysis file: " + file.getAbsolutePath());
                reader = new InputStreamReader(new FileInputStream(file), Charsets.UTF_8);
                xmlReader = xmlFactory.createXMLStreamReader(reader);
                readRoot();
            } catch (IOException | XMLStreamException e) {
                throw Throwables.propagate(e);
            } finally {
                closeXmlStream();
                Closeables.closeQuietly(reader);
            }

            logInfo("Reading analysis file done. Issues size: " + Integer.toString(issues.size()));
            return issues;
        }

        private void closeXmlStream() {
            if (xmlReader != null) {
                try {
                    xmlReader.close();
                } catch (XMLStreamException e) {
                    throw new IllegalStateException(e);
                }
            }
        }


        // start reading from the root node
        private void readRoot() throws XMLStreamException {
            logDebug("parser: readRoot");
            while (xmlReader.hasNext()) {
                int eventType = xmlReader.next();
                switch (eventType) {
                    case XMLStreamReader.START_ELEMENT:
                        String elementName = xmlReader.getLocalName();
                        logDebug("<" + elementName + ">");
                        if (elementName.equals("AnalysisOutput")) {
                            readAnalysisOutput();
                            // return immediately when done
                            return;
                        } else {
                            throw parseError("Unexpected element in root: '" + elementName + "'");
                        }
                }
            }
        }

        // start reading from the AnalysisOutput node
        private void readAnalysisOutput() throws XMLStreamException {
            logDebug("parser: readAnalysisOutput");
            String elementName;
            while (xmlReader.hasNext()) {
                int eventType = xmlReader.next();
                switch (eventType) {
                    case XMLStreamReader.START_ELEMENT:
                        elementName = xmlReader.getLocalName();
                        logDebug("<" + elementName + ">");
                        if (elementName.equals("Issues"))
                            readIssues();
                        else
                            throw parseError("Unexpected element in 'AnalysisOutput' node: '" + elementName + "'");
                        break;

                    case XMLStreamReader.END_ELEMENT:
                        elementName = xmlReader.getLocalName();
                        logDebug("</" + elementName + ">");
                        if (elementName.equals("AnalysisOutput"))
                            // read successfully
                            return;
                        else
                            throw parseError("Expecting 'AnalysisOutput' end element. Got: '" + elementName + "'");
                }
            }

            // will only get here if there is no closing tag for the root
            throw parseError("Premature end of file or no closing tag for root found");
        }

        // start reading from an "Issues" node
        private void readIssues() throws XMLStreamException {
            logDebug("parser: readIssues");
            String elementName;
            while (xmlReader.hasNext()) {
                int eventType = xmlReader.next();
                switch (eventType) {
                    case XMLStreamReader.START_ELEMENT:
                        elementName = xmlReader.getLocalName();
                        logDebug("<" + elementName + ">");
                        if (elementName.equals("Issue"))
                            readIssue();
                        else
                            throw parseError("Unexpected element in 'Issues' node: '" + elementName + "'");
                        break;
                    case XMLStreamReader.END_ELEMENT:
                        elementName = xmlReader.getLocalName();
                        logDebug("</" + elementName + ">");
                        if (elementName.equals("Issue"))
                            // read one issue, loop again
                            break;
                        else if (elementName.equals("Issues"))
                            // read all issues successfully
                            return;
                        else
                            throw parseError("Expecting 'Issues' end element. Got: " + elementName);
                }
            }
            throw parseError("Premature end of file. No closing tag for 'Issues' found");
        }


        private void readIssue() throws XMLStreamException {
            logDebug("parser: readIssue");
            String ruleKey = readElement("RuleKey");
            String message = readElement("Message");
            String absoluteFilePath = readElement("AbsoluteFilePath");
            Integer startLine = readIntElement("StartLine");
            Integer startColumn = readIntElement("StartColumn");
            Integer endLine = readIntElement("EndLine");
            Integer endColumn = readIntElement("EndColumn");
            issues.add(new FSharpIssue(ruleKey, message, absoluteFilePath, startLine, startColumn, endLine, endColumn));
        }

        private Integer readIntElement(String expectedTagName) throws XMLStreamException {
            String value = readElement(expectedTagName);

            if (value == null) {
                return null;
            }

            try {
                return Integer.parseInt(value);
            } catch (NumberFormatException e) {
                throw parseError("Expected an integer instead of \"" + value + "\" for the element \"" + expectedTagName + "\"");
            }
        }

        private String readElement(String expectedTagName) throws XMLStreamException {
            logDebug("parser: readElement. Expected: + '" + expectedTagName + "'");
            String elementName = "";
            String result = "";
            while (xmlReader.hasNext()) {
                int eventType = xmlReader.next();
                switch (eventType) {
                    case XMLStreamReader.START_ELEMENT:
                        elementName = xmlReader.getLocalName();
                        logDebug("<" + elementName + ">");
                        if (elementName.equals(expectedTagName)) {
                            result = xmlReader.getElementText();
                            logDebug("Element text = '" + result + "'");
                            return result;
                        } else {
                            throw parseError("Expected element: '" + expectedTagName + "'. Found '" + elementName + "'");
                        }
                }
            }

            throw parseError("Premature end of file");
        }

        private ParseErrorException parseError(String message) {
            return new ParseErrorException(
                message + " in " + file.getAbsolutePath() + " at line " + xmlReader.getLocation().getLineNumber());
        }


        private void logDebug(String message){
            // in tests, use LogTester.setLevel(LogLevel.DEBUG) to see the output
            LOG.debug(message);
        }

        private void logInfo(String message){
            LOG.info(message);
        }

    }

    private static class ParseErrorException extends RuntimeException {

        private static final long serialVersionUID = 1L;

        public ParseErrorException(String message) {
            super(message);
        }

    }

}
