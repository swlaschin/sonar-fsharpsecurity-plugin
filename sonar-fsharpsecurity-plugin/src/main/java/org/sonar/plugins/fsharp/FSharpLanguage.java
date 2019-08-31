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

import org.apache.commons.lang.StringUtils;
import org.sonar.api.config.Configuration;
import org.sonar.api.resources.AbstractLanguage;
import java.util.Optional;

/*
This class defines properties of the FSharp Language, such as FILE_SUFFIXES
(which is actually a constant defined in the top-level plugin class FSharpPlugin)
*/

public class FSharpLanguage extends AbstractLanguage {

  private final Configuration configuration;

  public FSharpLanguage(Configuration configuration) {
    super(FSharpPlugin.LANGUAGE_KEY, FSharpPlugin.LANGUAGE_NAME);
    this.configuration = configuration;
  }

  @Override
  public boolean equals(Object obj) {
    if (!super.equals(obj)) {
      return false;
    }

    FSharpLanguage fobj = (FSharpLanguage) obj;
    // added field is tested
    return configuration.equals(fobj.configuration);
  }

  @Override
  public int hashCode() {
    int result = super.hashCode();
    result = 31 * result + configuration.hashCode();
    return result;
  }

  @Override
  public String[] getFileSuffixes() {
    String suffixesStr = configuration.get(FSharpPlugin.FILE_SUFFIXES_KEY).orElse(FSharpPlugin.FILE_SUFFIXES_DEFVALUE);
    String[] suffixes = StringUtils.split(suffixesStr, ",");
    return suffixes;
  }
}
