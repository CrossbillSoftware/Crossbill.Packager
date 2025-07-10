# Crossbill.Packager utility for packaging files, applications, and plugins

Crossbill.Packager is the utility application for files, applications, and plugins packaging in Crossbill Central format. The utility allows to package almost any application to be deployable or automatically provisioned to a server environment using Crossbill Central or to multiple cloud environments using Crossbill Seeder.

## Release version

The compiled version of the utility can be obtained from the release section of this repository.

## Package format note

The package format itself is NuGet file format (.nupkg). So, it can be produced not only by Crossbill Packager, but also by other tools like MSBuild or Nuget.exe. The main difference is the agreements related to metadata files included in the package. It is Meta.jsdata to control deployment time parameters and behaviour. So, it is more common for deployment packages in Crossbill Central or Crossbill Seeder format to be packed by a specialised Crossbill Packager utility.

## Usage

### Call from CLI

The utility can be run from the command line interface as follows:
```
Crossbill.Packager.exe /mode pack /path "d:/Projects/Crossbill.Plugins/Crossbill.Central.Agent.Plugins.Cloudflare" /dest "d:/Projects/Crossbill.Plugins/Crossbill.Central.Agent.Plugins.Cloudflare/bin/Plugin.zip"
```

### Call from a Visual Studio project file
The utility can be run during the project compilation using the following configuration:
```
<PropertyGroup>
		<IsPackable>true</IsPackable>
		<NuspecFile>Crossbill.Central.Agent.Plugins.Cloudflare.nuspec</NuspecFile>
		<NuspecProperties>version=$(PackageVersion)</NuspecProperties>
		<GeneratePackageOnBuild Condition="'$(Configuration)' == 'Release'">True</GeneratePackageOnBuild>
</PropertyGroup>
	
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Condition="'$(Configuration)' == 'Release'" Command="c:\Crossbill.Packager.exe /mode zip /path &quot;$(ProjectDir).&quot; /dest &quot;$(ProjectDir)bin/Plugin.zip&quot;" />
</Target>
```

> [!NOTE]
> The build process can be configured according to your needs. In the above sample the Crossbill Packager prepares the files as a Plugin.zip file and MSBuild does the rest producing a package in .nupkg format.
> In the configuration like the following, everything is packed by Crossbill Packager only:
> ```
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Condition="'$(Configuration)' == 'Release'" Command="c:\Crossbill.Packager.exe /mode pack /path &quot;$(ProjectDir).&quot; /dest &quot;$(ProjectDir)bin/Plugin.zip&quot;" />
</Target>
```

## Package any app for automated deployment

You can prepare a package for almost any set of the files. Basically you have to:
1. Put files.txt with the relevant configuration to the source directory to instruct Crossbill Packager which files to package. For example:
```
bin\@{env}\Crossbill.Central.Agent.Plugins.Cloudflare.dll => plugins\Crossbill.Central.Agent.Plugins.Cloudflare\Crossbill.Central.Agent.Plugins.Cloudflare.dll
bin\@{env}\Crossbill.Central.Agent.Plugins.Cloudflare.deps.json => plugins\Crossbill.Central.Agent.Plugins.Cloudflare\Crossbill.Central.Agent.Plugins.Cloudflare.deps.json
plugins => plugins
```
2. Optionally put the metadata configuration file Meta.jsdata to the source directory to define the deployment time parameters. Sample metadata file:
```
{
  "App": {
    "AppType": "Plugin",
    "FileName": "Plugin.zip",
    "ConfigFile": "appsettings.json",
    "DeploymentPath": ".",
	"ProtectedDirectories": [
        "plugins/Crossbill.Bone.Plugins.Generated"
    ],
    "ParamMeta": [
      {
        "Name": "Cone API URL",
        "Description": "Specify the Url of Cone API application",
        "DefaultValue": "http://127.0.0.1/ConeAPI/",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/ConnectionStrings/Cone/ConnectionString"
          }
        ]
      }
    ]
  }
}
```
3. Put .nuspec file (for example Crossbill.Central.Agent.Plugins.Cloudflare.nuspec) with the relevant configuration to the source directory to instruct Crossbill Packager which metadata parameters to use. Commonly you can use the following .nuspec file contents replacing the relevant names:
```
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
  <metadata>
    <version>1.00</version>
    <authors>Pavel Korsukov</authors>
    <owners>Crossbill</owners>
    <id>Crossbill.Central.Agent.Plugins.Cloudflare</id>
    <title>Cloudflare plugin for Central Agent</title>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>The Cloudflare plugin for Crossbill Central Agent provides integration for Cloudflare DNS API.</description>
    <copyright>Crossbill</copyright>
    <tags>Plugin Crossbill.Central.Agent</tags>
    <license type="file">LICENSE.txt</license>
	<projectUrl>https://crossbillsoftware.com/en/Crossbill-Central-Agent/</projectUrl>
  </metadata>
  <files>
    <file src="bin\Plugin.zip" target="Content\Crossbill.Central.Agent.Plugins.Cloudflare" />
    <file src="Meta.jsdata" target="Content\Crossbill.Central.Agent.Plugins.Cloudflare" />
    <file src="plugins\Crossbill.Central.Agent.Plugins.Cloudflare\*.txt" target="" />
  </files>
</package>
```
4. Package the files from the command line interface:
```
Crossbill.Packager.exe /mode pack /path "d:/Projects/Crossbill.Plugins/Crossbill.Central.Agent.Plugins.Cloudflare"
```
5. The packed file now should be located under the Packs folder in the Crossbill Packager directory.
6. Now you can upload the produced package file to Crossbill Nest and use it in the automated scenarios or put it in a server's local directory to deploy it using Crossbill Central.

## Supported parameters
* /path - the directory containing the files to package;
* /dest - the target destination file;
* /mode - the packaging mode. The following modes are supported:
** zip - packs files listed in files.txt as a .zip archive;
** pack - packs files listed in files.txt as a .zip archive and then packs the result as a .nupkg file ready for Crossbill Central, Crossbill Seeder and Crossbill Nest;
** copy - copy files listed in files.txt from path to dest directory.

## Sample files.txt configuration

The files.txt configures which files to include in the package. The syntax supports directory copy, change of the target name, and substitution of the `@{env}` environment variable.
```
bin\@{env}\Crossbill.Central.Agent.Plugins.Cloudflare.dll => plugins\Crossbill.Central.Agent.Plugins.Cloudflare\Crossbill.Central.Agent.Plugins.Cloudflare.dll
bin\@{env}\Crossbill.Central.Agent.Plugins.Cloudflare.deps.json => plugins\Crossbill.Central.Agent.Plugins.Cloudflare\Crossbill.Central.Agent.Plugins.Cloudflare.deps.json
plugins => plugins
```

## Sample metadata file

The produced packages can contain the parameters used during the deployment. The parameters allow changing any file in the target directory and also define how to deploy your files. For example, to create a web site, a service, a daemon, or just unpack everything to the destination directory.

The file supports the syntax used by configuration files of Crossbill Install, of Crossbill Central Agent and project files of Crossbill Seeder. So, refer to Crossbill Seeder docs for detailed syntax information or either contact Crossbill support.

### Sample 1
```
{
  "App": {
    "AppType": "Plugin",
    "FileName": "Plugin.zip",
    "ConfigFile": "appsettings.json",
    "DeploymentPath": "."
  }
}
```

### Sample 2
```
{
  "App": {
    "AppType": "WebSite",
    "ApplicationName": "Cone",
	"WebSiteLocalPort": "5020",
	"IsTopLevelSite": "true",
    "ConfigFile": "appsettings.json",
    "ProtectedDirectories": [
        "Data",
        "plugins",
        "wwwroot/robots.txt"
    ],
    "WriteAccessDirectories": [
        "Data\\Repo",
        "Data\\Cache",
        "Data\\Files",
        "appsettings.json"
    ],
    "WebSiteAuth": "anonymous,forms",
	"FilesMaxSize": "104857600",
    "ParamMeta": [
      {
        "Name": "Database Connection",
        "Description": "Specify the connection string for the application database",
        "DefaultValue": "server=127.0.0.1;user id=crossbill;password=;Port=5432;database=crossbill",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/ConnectionStrings/CrossbillDatabase/ConnectionString"
          }
        ]
      },
      {
        "Name": "Database Schema",
        "Description": "Specify the schema for the application database",
        "DefaultValue": "cb",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/AppSettings/DatabaseSchema"
          }
        ]
      },
      {
        "Name": "Twofish Secret",
        "Description": "Twofish Cryptor Secret",
        "DefaultValue": "",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/AppSettings/TwofishSecret"
          }
        ]
      },
      {
        "Name": "Twofish IV",
        "Description": "Twofish Initial Vector",
        "DefaultValue": "",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/AppSettings/TwofishIV"
          }
        ]
      }
    ]

  }
}
```

### Sample 3
```
{
  "App": {
    "AppType": "Service",
    "FileName": "Crossbill.Signalman.zip",
    "ConfigFile": "appsettings.json",
	"Identity":  "LocalSystem",
	"ProtectedDirectories": [
        "Data",
        "plugins"
    ],
    "WriteAccessDirectories": [
        "Data\\Repo",
        "Data\\Cache",
        "Data\\Files",
        "appsettings.json"
    ],
    "ParamMeta": [
      {
        "Name": "Database Connection",
        "Description": "Specify the connection string for the application database",
        "DefaultValue": "server=127.0.0.1;user id=crossbill;password=;Port=5432;database=crossbill",
        "Targets": [
          {
            "Kind": "JsonFile",
            "Scope": "appsettings.json",
            "Match": "/ConnectionStrings/CrossbillDatabase/ConnectionString"
          }
        ]
      }
    ]
  }
}
```

## License

The Crossbill Software License Agreement is located in [plugins/Crossbill.Plugins.DataVersioning.Git/LICENSE.txt](plugins/Crossbill.Plugins.DataVersioning.Git/LICENSE.txt) file.

The Third Party Code in Crossbill Products notice is located in [plugins/Crossbill.Plugins.DataVersioning.Git/third-party-code.txt](plugins/Crossbill.Plugins.DataVersioning.Git/third-party-code.txt) file.

The copyright and license texts for the third party code can be found in [plugins/Crossbill.Plugins.DataVersioning.Git/third-party-notices.txt](plugins/Crossbill.Plugins.DataVersioning.Git/third-party-notices.txt) file.

