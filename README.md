# ReflectInsight-Extensions-SemanticLogging

[![Build status](https://ci.appveyor.com/api/projects/status/github/reflectsoftware/reflectinsight-extensions-semanticlogging?svg=true)](https://ci.appveyor.com/project/reflectsoftware/reflectinsight-extensions-semanticlogging)
[![Release](https://img.shields.io/github/release/reflectsoftware/reflectinsight-extensions-semanticlogging.svg)](https://github.com/reflectsoftware/reflectinsight-extensions-semanticlogging/releases/latest)
[![NuGet Version](http://img.shields.io/nuget/v/reflectsoftware.insight.extensions.semanticlogging.svg?style=flat)](http://www.nuget.org/packages/ReflectSoftware.Insight.Extensions.semanticlogging/)
[![NuGet](https://img.shields.io/nuget/dt/reflectsoftware.insight.extensions.semanticlogging.svg)](http://www.nuget.org/packages/ReflectSoftware.Insight.Extensions.SemanticLogging/)
[![Stars](https://img.shields.io/github/stars/reflectsoftware/reflectinsight-extensions-semanticlogging.svg)](https://github.com/reflectsoftware/reflectinsight-extensions-semanticlogging/stargazers)

## Overview ##

The ReflectInsight Semantic Logging Extension is a custom sink specifically developed for the Semantic Logging Framework. The Semantic Logging Framework was developed to tap into the ETW infrastructure and in doing so developers can redirect ETW messages to their preferred destinations (i.e. Viewer, etc.).

This document will show you how to setup and configure, with the ReflectInsight Semantic Logging Extension (Sink) for both “In-Process” and “Out-of-Process” implementations.

## Benefits of ReflectInsight Extensions ##

The benefits to using the Insight Extensions is that you can easily and quickly add them to your applicable with little effort and then use the ReflectInsight Viewer to view your logging in real-time, allowing you to filter, search, navigate and see the details of your logged messages.

## Getting Started

```powershell
Install-Package ReflectSoftware.Insight.Extensions.SemanticLogging
```

Then in your app.config or web.config file, add the following configuration sections:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="insightSettings" type="ReflectSoftware.Insight.ConfigurationHandler,ReflectSoftware.Insight" />
  </configSections>

  <insightSettings>
    <baseSettings>
      <configChange enabled="true" />
      <enable state="all" />
      <propagateException enabled="false" />
      <global category="ReflectInsight" />
      <exceptionEventTracker time="20" />
    </baseSettings>
    
    <listenerGroups active="Release">
      <group name="Release" enabled="true" maskIdentities="false">
        <destinations>
          <destination name="Viewer" enabled="true" filter="" details="Viewer" />
        </destinations>
      </group>
    </listenerGroups>
    
    <logManager default="semantic">
      <instance name="semantic" category="Semantic" />
    </logManager>
  </insightSettings>
  
  <startup> 
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
  </startup>
</configuration>
```

Additional configuration details for the ReflectSoftware.Insight.Extensions.SemanticLogging logging extension can be found [here](https://reflectsoftware.atlassian.net/wiki/pages/viewpage.action?pageId=5570590).

## Additional Resources

[Documentation](https://reflectsoftware.atlassian.net/wiki/display/RI5/ReflectInsight+5+documentation)

[Knowledge Base](http://reflectsoftware.uservoice.com/knowledgebase)

[Submit User Feedback](http://reflectsoftware.uservoice.com/forums/158277-reflectinsight-feedback)

[Contact Support](support@reflectsoftware.com)

[ReflectSoftware Website](http://reflectsoftware.com)
