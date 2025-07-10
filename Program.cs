using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crossbill.Common.Log;
using Crossbill.Common.Utils;
using Crossbill.Common.Plugins;
using Crossbill.Install.Lib.Helpers;
using System.Reflection;
using System.Xml;
using Microsoft.Web.XmlTransform;
using Crossbill.Common;
using Microsoft.Extensions.Configuration;
using Crossbill.Common.Resources;
using ICSharpCode.SharpZipLib.Zip;
using static ICSharpCode.SharpZipLib.Zip.Compression.Deflater;

namespace Crossbill.Packager
{
    class Program
    {
        protected static Dictionary<string, SearchOption> _fileToClearDebug = new Dictionary<string, SearchOption>() {
            { "*.runtimeconfig.dev.json", SearchOption.TopDirectoryOnly },
            { "appsettings.Development.json", SearchOption.TopDirectoryOnly },
            { "web.Release.config", SearchOption.TopDirectoryOnly },
            { "Properties/launchSettings.json", SearchOption.TopDirectoryOnly }
        };

        protected static Dictionary<string,SearchOption> _fileToClearRelease = new Dictionary<string, SearchOption>() {
            { "*.pdb", SearchOption.TopDirectoryOnly },
            { "*.hash", SearchOption.TopDirectoryOnly },
            { "*.nrmap", SearchOption.TopDirectoryOnly },
            { "plugins/*.pdb", SearchOption.AllDirectories },
            { "plugins/*.hash", SearchOption.AllDirectories },
            { "plugins/*.nrmap", SearchOption.AllDirectories },
            { "*.staticwebassets.runtime.json", SearchOption.TopDirectoryOnly }
		};

        static void Main(string[] args)
        {
			string mode = "pack";
            string basePath = Environment.CurrentDirectory;
            string destination = "";
            string env = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null && args[i] == "/path")
                {
                    basePath = args[i + 1];
                    while (basePath.StartsWith("'") && !basePath.EndsWith("'"))
                    {
                        i++;
                        basePath += " " + args[i + 1];
                    }
                    if (basePath.StartsWith("'"))
                    {
                        basePath = basePath.Substring(1, basePath.Length - 2);
                    }
                }
                if (args[i] != null && args[i] == "/dest")
                {
                    destination = args[i + 1];
                    while (destination.StartsWith("'") && !destination.EndsWith("'"))
                    {
                        i++;
                        destination += " " + args[i + 1];
                    }
                    if (destination.StartsWith("'"))
                    {
                        destination = destination.Substring(1, destination.Length - 2);
                    }
                }
                if (args[i] != null && args[i] == "/mode")
                {
                    mode = args[i + 1];
                }
                if (args[i] != null && args[i] == "/env")
                {
                    env = args[i + 1];
                }
            }

            if (env == null)
            {
                env = mode == "copy" ? "Debug\\net8.0" : "Release\\net8.0";
            }

            if (!Directory.Exists(basePath))
            {
                basePath = Path.Combine(Environment.CurrentDirectory, basePath);
                if (!String.IsNullOrEmpty(destination))
                {
                    destination = Path.Combine(Environment.CurrentDirectory, destination);
                }
            }

            Console.WriteLine("Crossbill.Packager mode [{0}] started in [{1}]", mode, basePath);

            if (!Directory.Exists(basePath))
            {
                Console.WriteLine("Directory not found");
                return;
            }

            string fileList = Path.Combine(basePath, String.Format("{0}.txt", mode));
            if (!File.Exists(fileList))
            {
                fileList = Path.Combine(basePath, "files.txt");
                if (!File.Exists(fileList))
                {
                    Console.WriteLine("files.txt not found. No files to package");
                    return;
                }
            }

            string tempFolder = Path.Combine(basePath, ".packager");
            if (Directory.Exists(tempFolder))
            {
                Console.WriteLine("Removing existing temp folder {0}", tempFolder);
                Delete(tempFolder);
            }

            try
            {
                Directory.CreateDirectory(tempFolder);

                string[] files = File.ReadAllLines(fileList);
                files.AsParallel().ForAll(file =>
                {
                    if (String.IsNullOrEmpty(file) || String.IsNullOrEmpty(file.Trim()))
                    {
                        return;
                    }

                    file = file.Replace("@{env}", env);

                    string[] paths = file
                                        .Split(new string[] { "=>" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim())
                                        .ToArray();
                    string source = Path.Combine(basePath, paths[0]);
                    string tempFile = Path.Combine(tempFolder, paths.Length > 1 ? paths[1] : file);
                    string dirName = Path.GetDirectoryName(tempFile);
                    if (!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }

                    if (File.Exists(source))
                    {
                        try
                        {
                            Console.WriteLine("Copying file {0} to {1}", source, tempFile);
                            File.Copy(source, tempFile);
                        }
                        catch (IOException)
                        {
                        }
                    } else if (Directory.Exists(source))
                    {
                        Console.WriteLine("Copying directory {0} to {1}", source, tempFile);
                        DirectoryCopy(source, tempFile);
                    }
                });

                var filesToClear = _fileToClearDebug;
                if (env == null || !env.Contains("Debug"))
                {
                    filesToClear = filesToClear
                                    .Concat(_fileToClearRelease)
                                    .ToDictionary(p => p.Key, p => p.Value);
                }

                filesToClear.AsParallel().ForAll(f =>
                {
                    string expression = f.Key;
                    string path = tempFolder;
                    if (expression.Contains('/'))
                    {
                        path = Path.Combine(path, expression.Substring(0, expression.LastIndexOf('/')));
                        expression = expression.Substring(expression.LastIndexOf('/') + 1);
                    }

                    if (Directory.Exists(path))
                    {
                        string[] found = Directory.GetFiles(path, expression, f.Value);
                        foreach (string foundFile in found)
                        {
                            File.Delete(foundFile);
                        }
                        if (path != tempFolder && !Directory.EnumerateFileSystemEntries(path).Any())
                        {
                            Delete(path);
                        }
                    }
                });

                string metaFile = Path.Combine(basePath, "Meta.jsdata");
                if (File.Exists(metaFile))
                {
                    var meta = Serializer.GetFromJson<Metadata>(File.ReadAllText(metaFile));
                    if (meta != null && meta.App != null && meta.App.ParamMeta != null && meta.App.ParamMeta.Count > 0)
                    {
                        ConsoleLogger logger = new ConsoleLogger();
                        ExternalConfigHelper.SetConfigValues(logger, meta.App, tempFolder);
                    }
                }

                if (mode != null)
                {
                    if (mode.ToLower() == "pack" || mode.ToLower() == "zip")
                    {
                        string webConfig = Path.Combine(tempFolder, "web.config");
                        string transform = Directory.GetFiles(basePath, "web.Release.config", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (File.Exists(webConfig) && transform != null)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(webConfig);
                            using (XmlTransformation trns = new XmlTransformation(transform))
                            {
                                trns.Apply(doc);
                            }
                            doc.Save(webConfig);
                        }
                    }

                    switch (mode.ToLower())
                    {
                        case "pack":
                            string packTarget = Zip(basePath, destination, tempFolder);

                            packTarget = Path.GetDirectoryName(packTarget);

                            string csproj = Directory.GetFiles(basePath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            string nuspec = Directory.GetFiles(basePath, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();

                            if (nuspec != null && csproj != null)
                            {
                                PROCESS.RunProcess((s, i) => { Console.WriteLine(s); }, "dotnet", String.Format("pack {0} --no-build --no-dependencies --no-restore --nologo --verbosity minimal -p:NuspecFile={1} -p:NuspecBasePath={2} --output {3}", csproj, nuspec, basePath, packTarget), basePath, true, false);
                            }

                            string nupkg = Directory.GetFiles(packTarget, "*.nupkg", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            if (nupkg != null)
                            {
                                string packsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Packs");
                                if (!Directory.Exists(packsDir))
                                {
                                    Directory.CreateDirectory(packsDir);
                                }
                                File.Copy(nupkg, Path.Combine(packsDir, Path.GetFileName(nupkg)), true);
                            }

                            break;
                        case "zip":
                            Zip(basePath, destination, tempFolder);
                            break;
                        case "copy":
                            string target = destination;
                            Console.WriteLine("Copy {0} to {1}", tempFolder, target);
                            DirectoryCopy(tempFolder, target);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                string message = ErrorMessageHelper.GetErrorMessage(ex, "Unexpected error.").ToString();
                Console.WriteLine(message);
                throw ex;
            }
            finally
            {
                Console.WriteLine("Removing temp folder {0}", tempFolder);
                Delete(tempFolder);
            }
        }

        private static string Zip(string basePath, string destination, string tempFolder)
        {
            string target = String.IsNullOrEmpty(destination) ? Path.Combine(basePath, "Plugin.zip") : (destination.EndsWith(".zip") ? destination : Path.Combine(destination, "Plugin.zip"));
            if (File.Exists(target))
            {
                File.Delete(target);
            }
            
            Console.WriteLine("Zip {0} to {1}", tempFolder, target);
            FastZip zip = new FastZip();
            zip.CreateZip(target, tempFolder, true, null);
            
            return target;
        }

        public static void Delete(string dir)
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                try
                {
                    Thread.Sleep(2000);
                    Directory.Delete(dir, true);
                }
                catch
                {
                }
            }
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }else if(dir.Attributes.HasFlag(FileAttributes.Hidden)){
                return;
            }

            DirectoryInfo[] dirs = dir.GetDirectories("*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
            files.AsParallel().ForAll(file =>
            {
                string temppath = Path.Combine(destDirName, file.Name);
                try
                {
                    file.CopyTo(temppath, true);
                }
                catch(IOException)
                {
                }                
            });

            dirs.AsParallel().ForAll(subdir =>
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            });
        }

    }
}
