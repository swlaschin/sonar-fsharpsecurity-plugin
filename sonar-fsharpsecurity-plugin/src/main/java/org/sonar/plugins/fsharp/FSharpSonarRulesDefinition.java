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


 // based on plugins from from https://github.com/SonarSource
package org.sonar.plugins.fsharp;

import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import org.sonar.api.rule.Severity;
import org.sonar.api.server.rule.RuleParamType;
import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.api.server.rule.RulesDefinitionXmlLoader;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;

/*
This class loads the available rules into the repository.
The rules come from a XML resource file.


IMPORTANT: the schema must match the one defined by the F# code in FsSonarRunner/RuleDefinitionDto
and also as defined by Sonar at http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/server/rule/RulesDefinitionXmlLoader.html

*/

public class FSharpSonarRulesDefinition implements RulesDefinition {
  private static final String PATH_TO_RULES_XML = "/rules.xml";
  private static final Logger LOG = Loggers.get(FSharpSonarRulesDefinition.class);

  private void defineRulesForLanguage(Context context, String repositoryKey, String repositoryName, String languageKey, String filename) {
    NewRepository repository = context.createRepository(repositoryKey, languageKey).setName(repositoryName);
    LOG.info("Reading rules definition. File: '" + filename + "' repositoryKey:" +  repositoryKey + " repositoryName:" +  repositoryName + " languageKey:" +  languageKey);

    InputStream rulesXml = this.getClass().getResourceAsStream(filename);
    if (rulesXml != null) {
      RulesDefinitionXmlLoader rulesLoader = new RulesDefinitionXmlLoader();
      rulesLoader.load(repository, rulesXml, StandardCharsets.UTF_8.name());
    }
    else {
      LOG.error("No resource found for rules definition");
    }

    repository.done();
  }

  @Override
  public void define(Context context) {
    defineFromFile(context,PATH_TO_RULES_XML);
  }

  public void defineFromFile(Context context, String filename) {
    defineRulesForLanguage(context, FSharpPlugin.REPOSITORY_KEY, FSharpPlugin.REPOSITORY_NAME, FSharpPlugin.LANGUAGE_KEY,filename);
  }

}
