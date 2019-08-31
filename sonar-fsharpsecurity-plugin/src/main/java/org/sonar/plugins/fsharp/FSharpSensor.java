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

 // adapted from https://github.com/SonarSource/sonar-csharp
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

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

import org.apache.commons.lang.StringEscapeUtils;
import org.apache.commons.lang.StringUtils;
import org.sonar.api.batch.DependedUpon;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile;
import org.sonar.api.batch.fs.TextRange;
import org.sonar.api.batch.fs.TextPointer;
import org.sonar.api.batch.rule.ActiveRule;
import org.sonar.api.batch.rule.Checks;
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
import org.sonar.api.measures.Metric;
import org.sonar.api.rule.RuleKey;
import org.sonar.api.utils.command.Command;
import org.sonar.api.utils.command.CommandExecutor;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;


/*
The main analyzer.

When analyzer is called:
* SonarScanner passes the rules and the files into the plugin ("execute")
* The plugin creates a configuration ("createConfiguration") containing those rules and files and writes it to an XML file
* The plugin then executes the F# executable (`FsSolarRunner.exe`) passing that input file as parameter
* The F# exe dumps out the results as files.
* The plugin then reads these files in ("importResults") and updates the passed-in context.

*/

@DependedUpon("NSonarQubeAnalysis")
public class FSharpSensor implements Sensor {

  private static final Logger LOG = Loggers.get(FSharpSensor.class);

  private static final String RULE_KEY = "RuleKey";

  private static final String DIAGNOSTICS_FILE = "sonarDiagnostics.xml";
  private static final String ANALYSIS_CONFIG_FILE = "sonarAnalysisConfig.xml";

  private final FsSonarRunnerExtractor extractor;
  private final FileSystem filesystem;
  private final FileLinesContextFactory fileLinesContextFactory;
  private final NoSonarFilter noSonarFilter;

  public FSharpSensor(FsSonarRunnerExtractor extractor, FileSystem filesystem, FileLinesContextFactory fileLinesContextFactory,
      NoSonarFilter noSonarFilter) {
    this.extractor = extractor;
    this.filesystem = filesystem;
    this.fileLinesContextFactory = fileLinesContextFactory;
    this.noSonarFilter = noSonarFilter;
  }

  @Override
  public void describe(SensorDescriptor descriptor) {
    descriptor.name(FSharpPlugin.LANGUAGE_NAME).onlyOnLanguage(FSharpPlugin.LANGUAGE_KEY);
  }

  @Override
  public void execute(SensorContext context) {
    try {
      analyze(context);
      List<FSharpIssue> issues = importResults(context);
      saveIssues(context, issues);
    } catch (Exception ex) {
      LOG.error("SonarQube Community F# plugin analyzis failed", ex);
    }
  }

  private void analyze(SensorContext context) {
    File analysisInput = toolInput();
    File analysisOutputDir = toolOutputDir();


    try {
      LOG.info("FSharpSensor: writing config to:" + analysisInput.getAbsolutePath());
      StringBuilder sb = createConfiguration(context);
      String workdirRoot = context.fileSystem().workDir().getCanonicalPath();
      FSharpSensor.writeStringToFile(analysisInput.getAbsolutePath(), sb.toString());

      LOG.info("FSharpSensor: extracting ZIP file to: " + workdirRoot);
      File executableFile = extractor.executableFile(workdirRoot);
      LOG.info("FSharpSensor: executableFile file lives at: " + executableFile.getAbsolutePath());

      Command command = Command.create(executableFile.getAbsolutePath());
      command
          .addArgument("-c")
          .addArgument("-ci")
          .addArgument(analysisInput.getAbsolutePath())
          .addArgument("-od")
          .addArgument(analysisOutputDir.getAbsolutePath());

      LOG.info("FSharpSensor: executing command line: " + command.toCommandLine());
      CommandExecutor.create().execute(command, LOG::info, LOG::error, Integer.MAX_VALUE);

    } catch (IOException e) {
      LOG.error("Could not write settings to file '{0}'", e.getMessage());
    }
  }

  private List<FSharpIssue> importResults(SensorContext context) {
    File analysisOutput = toolOutputFile();
    LOG.info("FSharpSensor: analysis output expected at: " + analysisOutput.getAbsolutePath());

    List<FSharpIssue> issues = new FSharpAnalysisResultImporter().parse(analysisOutput);
    LOG.info("FSharpSensor: issues count is: " +  Integer.toString(issues.size()) );
    return issues;
  }

  private void saveIssues(SensorContext context, List<FSharpIssue> issues) {
    for (FSharpIssue issue : issues) {
      String absoluteFilePath = issue.absoluteFilePath();
      LOG.debug(String.format("Creating issue to save. RuleKey:'%s' File:%s",issue.ruleKey(),absoluteFilePath));

      // setup a new issue
      RuleKey ruleKey = RuleKey.of(FSharpPlugin.REPOSITORY_KEY, issue.ruleKey());
      NewIssue newIssue = context
        .newIssue()
        .forRule(ruleKey);

      // try to find the file in the files that Sonar told us to process
      InputFile inputFile;
      try {
          inputFile = filesystem.inputFile(filesystem.predicates().hasAbsolutePath(absoluteFilePath));
      } catch (Exception ex){
          logSkippedIssue(issue, "Failed find input file: exception: \"" + ex.getMessage() +"\"");
          continue;
      }

      // if found, set the location and save it
      if (inputFile != null) {
        TextRange textRange = inputFile.newRange(issue.startLine(), issue.startLineOffset(), issue.endLine(), issue.endLineOffset() );
        NewIssueLocation primaryLocation = newIssue.newLocation()
          .on(inputFile)
          .at(textRange)
          .message(issue.message());

        newIssue.at(primaryLocation);
        newIssue.save();
      }
    }
  }

  private static void logSkippedIssue(FSharpIssue issue, String reason) {
    LOG.info(String.format("Skipping an issue: reason:'%s'. file:'%s' line:%d",reason,issue.absoluteFilePath(),issue.startLine()));
  }

  private File toolInput() {
      return new File(filesystem.workDir(), ANALYSIS_CONFIG_FILE);
  }

  public File toolOutputFile() {
      return new File(filesystem.workDir(), DIAGNOSTICS_FILE);
  }

  public File toolOutputDir() {
    return filesystem.workDir();
}

  /* =====================================
  Analysis Configuration builder from here down
  ===================================== */

  private StringBuilder createConfiguration(SensorContext context) {
    StringBuilder sb = new StringBuilder();
    appendLine(sb, 0, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    appendLine(sb, 0, "<AnalysisInput>");
    appendSettings(sb);
    appendRules(sb, context.activeRules().findByRepository(FSharpPlugin.REPOSITORY_KEY));
    appendFiles(sb);
    appendLine(sb, 0, "</AnalysisInput>");
    return sb;
  }

  private void appendSettings(StringBuilder sb) {
    appendLine(sb, 1, "<Settings>");
    appendLine(sb, 1, "</Settings>");
  }

  private void appendRules(StringBuilder sb, Collection<ActiveRule> rules) {
    appendLine(sb, 1, "<Rules>");
    for (ActiveRule activeRule : rules) {
      appendLine(sb, 2, "<Rule>");
      Map<String, String> parameters = effectiveParameters(activeRule);
      appendLine(sb, 3, "<Key>" + parameters.get(RULE_KEY) + "</Key>");
      if (!parameters.isEmpty()) {
        appendLine(sb, 3, "<Parameters>");
        for (Entry<String, String> parameter : parameters.entrySet()) {
          if (RULE_KEY.equals(parameter.getKey())) {
            continue;
          }

          appendLine(sb, 4, "<Parameter>");
          appendLine(sb, 5, "<Key>" + parameter.getKey() + "</Key>");
          appendLine(sb, 5, "<Value>" + StringEscapeUtils.escapeXml(parameter.getValue()) + "</Value>");
          appendLine(sb, 4, "</Parameter>");
        }
        appendLine(sb, 3, "</Parameters>");
      }
      appendLine(sb, 2, "</Rule>");
    }
    appendLine(sb, 1, "</Rules>");
  }

  private void appendFiles(StringBuilder sb) {
    appendLine(sb, 1, "<Files>");
    filesToAnalyze().forEach(f -> appendFile(sb, f));
    appendLine(sb, 1, "</Files>");
  }

  private Iterable<InputFile> filesToAnalyze() {
    return filesystem.inputFiles(filesystem.predicates().hasLanguage(FSharpPlugin.LANGUAGE_KEY));
  }

  private void appendFile(StringBuilder sb, InputFile file) {
    appendLine(sb, 2, "<File>" + Paths.get(file.uri()).toAbsolutePath() + "</File>");
  }

  private static Map<String, String> effectiveParameters(ActiveRule activeRule) {
    Map<String, String> builder = new HashMap<>();

    if (!"".equals(activeRule.templateRuleKey())) {
      builder.put(RULE_KEY, activeRule.ruleKey().rule());
    }

    for (Map.Entry<String, String> param : activeRule.params().entrySet()) {
      builder.put(param.getKey(), param.getValue());
    }

    return builder;
  }

  private void appendLine(StringBuilder sb, int indent, String str) {
    sb.append(StringUtils.repeat("  ", indent)).append(str).append(System.lineSeparator());
  }

  public static void writeStringToFile(String path, String content) throws IOException {
    File file = new File(path);
    try (BufferedWriter writer = new BufferedWriter(new FileWriter(file))) {
        writer.write(content);
    }
  }

}
