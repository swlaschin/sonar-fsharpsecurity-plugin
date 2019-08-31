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

import java.util.List;
import java.util.ArrayList;
import java.io.File;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.charset.StandardCharsets;
import java.net.URL;
import java.io.IOException;
import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.util.stream.Stream;
import java.util.stream.Collectors;
import org.sonar.api.server.profile.BuiltInQualityProfilesDefinition;
import org.sonar.api.utils.log.Logger;
import org.sonar.api.utils.log.Loggers;


/*
This class defines a Quality Profile, which captures which rules are available and what the severities are.
See https://docs.sonarqube.org/latest/instance-administration/quality-profiles/

The default (non-overridable) profile is called "Sonar Way" and is provided by the plugin.
*/


public class FSharpSonarWayProfile implements BuiltInQualityProfilesDefinition {
  private static final String PATH_TO_PROFILE_TXT = "/profile.txt";
  public static final Logger LOG = Loggers.get(FSharpSonarWayProfile.class);

  @Override
  public void define(Context context) {
      URL profileUrl = getClass().getResource(PATH_TO_PROFILE_TXT);
      defineFromUrl(context,profileUrl);
  }

  public void defineFromUrl(Context context, URL profileUrl) {
    LOG.info("FSharpSonarWayProfile: reading profile " + profileUrl.toString());
    NewBuiltInQualityProfile profile = context.createBuiltInQualityProfile(FSharpPlugin.FSHARP_WAY_PROFILE, FSharpPlugin.LANGUAGE_KEY);
    profile.setDefault(true);

    List<String> ruleKeys = importRuleKeysFromUrl(profileUrl);
    ruleKeys.forEach( (ruleKey) -> profile.activateRule(FSharpPlugin.REPOSITORY_KEY, ruleKey) );

    profile.done();
  }

  public List<String> importRuleKeysFromUrl(URL profileUrl) {
    List<String> ruleKeys = new ArrayList<String>();

    try (InputStream stream = profileUrl.openStream()) {
      ruleKeys = new BufferedReader(new InputStreamReader(stream, StandardCharsets.UTF_8)).lines().collect(Collectors.toList());      
      LOG.info("FSharpSonarWayProfile: #rules found: " + Integer.toString(ruleKeys.size()) );
    } catch (IOException e) {
      LOG.error("Unable to read profile stream: {} => {}", profileUrl.toString(), e.getMessage());
    }

    return ruleKeys;
  }

}
