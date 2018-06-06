using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlainTextPasswords
{
    class Program
    {
        /// <summary>
        /// Regex to recognize hardcoded passwords
        /// </summary>
        static readonly Regex regex = new Regex(@"^(.*)(accountkey\w*=|password\w*=|authentication\w*=|authenticationkey\w*=)(.*)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// List of all the extension that the tool will scan
        /// </summary>
        static List<string> extensions = new List<string>();

        /// <summary>
        /// Scans all the files in the given path and recursively continues to sub directories.
        /// </summary>
        /// <param name="path"></param>
        static void ScanFiles(string path)
        {
            var filesInThisFolder = Enumerable.Empty<string>();
            foreach(var extension in extensions)
            {
                filesInThisFolder = filesInThisFolder.Concat(Directory.EnumerateFiles(path, extension, SearchOption.TopDirectoryOnly));
            }

            foreach(var filePath in filesInThisFolder)
            {
                using (var reader = File.OpenText(filePath))
                {
                    while(!reader.EndOfStream)
                    {
                        var line = reader.ReadLine().Trim();
                        if (regex.IsMatch(line))
                        {
                            var matchingLine = regex.Replace(line, "$1*$2*$3");
                            Console.WriteLine($"{filePath},{matchingLine}");
                        }
                    }
                }
            }

            // We'll ignore ./bin or ./obj folders if they are under a folder that has a *.*proj file
            // Otherwise we get duplicates from build outputs
            var projectFolder = Directory.EnumerateFiles(path, "*.*proj").Any();
            var dirs = Directory.EnumerateDirectories(path);
            foreach(var dir in dirs)
            {
                if (projectFolder)
                {
                    var directory = new DirectoryInfo(dir);
                    if (string.Equals(directory.Name, "obj", StringComparison.InvariantCultureIgnoreCase) || string.Equals(directory.Name, "bin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                ScanFiles(dir);
            }
        }

        static void Main(string[] args)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var configuration = builder.Build();
            foreach(var kvp in configuration.GetSection("extensions").GetChildren())
            {
                extensions.Add(kvp.Value);
            }

            ScanFiles(args[0]);
        }
    }
}
