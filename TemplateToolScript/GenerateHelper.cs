namespace TemplateToolScript
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualBasic;

    public static class GenerateHelper
    {
        public static Dictionary<string, bool> ValidPaths { get; } = new Dictionary<string, bool>();

        public static Dictionary<string, string> Models { get; } = new Dictionary<string, string>();

        public static Dictionary<string, string> Menus
            => new Dictionary<string, string>
            {
                { "valid", "Validate whether current folder can be used to generate code." },
                { "models", "Gives a list of all available models" },
                { "providers", "Generate providers for models" },
                { "services", "Generate services for models" },
                { "help", "Show an overview with available commands in module 'generate'" }
            };

        public static List<string> BlackList => new List<string>
        {
            "TrackableEntity",
            "EntityTranslation"
        };

        public static void Actions(List<string> actions)
        {
            if (actions.Count <= 1)
            {
                Help();
                return;
            }

            var action = actions[1];

            switch (action.ToLower())
            {
                case "help":
                    Help();
                    return;
                case "providers":
                    ProviderGenerator.GenerateProviders();
                    Program.EmptyLine();
                    return;
                case "services":
                    ServiceGenerator.GenerateServices();
                    Program.EmptyLine();
                    return;
                case "valid":
                    CheckIfDirectoryIsValid();
                    Program.EmptyLine();
                    return;
                case "models":
                    CheckIfDirectoryIsValid();

                    if (!ValidPaths.ContainsKey(Program.CurrentDirectory) ||
                        !ValidPaths.First(x => x.Key.Equals(Program.CurrentDirectory, StringComparison.InvariantCultureIgnoreCase)).Value)
                    {
                        Program.EmptyLine();
                        return;
                    }

                    SetModules();
                    PrintModules();
                    Program.EmptyLine();
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("> The module 'generate' does not recognize your specified command, use 'generate help' to find your command.");
                    Console.ResetColor();
                    Program.EmptyLine();
                    return;
            }
        }

        public static void Help()
        {
            Program.WriteTitle("Overview of all available commands in module: Generate:");

            var tab = Constants.vbTab;
            var longestLength = Menus
                .OrderByDescending(x => x.Key.Length)
                .First().Key.Length;

            foreach (var item in Menus)
            {
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.DarkMagenta;

                var label = item.Key;

                while (label.Length < longestLength)
                {
                    label += " ";
                }

                Console.Write($"generate {label}");
                Console.ResetColor();
                Console.WriteLine($" {tab} {item.Value}");
            }

            Console.WriteLine(string.Empty);
            Program.EmptyLine();
        }

        public static Tuple<string, string, bool> PathIsValidModel(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                var publicClass = lines.FirstOrDefault(line => line.Contains("public class "));

                if (string.IsNullOrWhiteSpace(publicClass))
                {
                    return new Tuple<string, string, bool>(path, string.Empty, false);
                }

                var lineParts = publicClass.Split(':');
                var className = lineParts.First().Replace("public class ", string.Empty);
                className = className.Trim();

                return new Tuple<string, string, bool>(path, className, true);
            }
            catch
            {
                return new Tuple<string, string, bool>(path, string.Empty, false);
            }
        }

        public static void CheckIfDirectoryIsValid()
        {
            if (ValidPaths.ContainsKey(Program.CurrentDirectory))
            {
                var valid = ValidPaths.First(m => m.Key.Equals(Program.CurrentDirectory)).Value;

                if (valid)
                {
                    ValidDirectory();
                    return;
                }

                NotValidDirectory();
                return;
            }

            var solutions = Directory.GetFiles(Program.CurrentDirectory, "*.sln");

            if (!solutions.Any())
            {
                NotValidDirectory();
                return;
            }

            var directories = Directory.GetDirectories(Program.CurrentDirectory);

            if (!directories.Any(directory => directory.Contains(".DAL", StringComparison.InvariantCultureIgnoreCase)) ||
                !directories.Any(directory => directory.Contains(".Providers", StringComparison.InvariantCultureIgnoreCase)) ||
                !directories.Any(directory => directory.Contains(".Services", StringComparison.InvariantCultureIgnoreCase)))
            {
                NotValidDirectory();
                return;
            }

            var directoryDAL = directories.First(directory => directory.Contains(".DAL", StringComparison.InvariantCultureIgnoreCase));
            var directoryProviders = directories.First(directory => directory.Contains(".Providers", StringComparison.InvariantCultureIgnoreCase));
            var directoryServices = directories.First(directory => directory.Contains(".Services", StringComparison.InvariantCultureIgnoreCase));

            ValidProjectDirectory(directoryDAL);
            ValidProjectDirectory(directoryProviders);
            ValidProjectDirectory(directoryServices);
            ValidDirectory();
        }

        public static string GetFileNamespace(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                var namespaceLine = lines.FirstOrDefault(line => line.Contains("namespace ", StringComparison.CurrentCultureIgnoreCase));

                if (string.IsNullOrWhiteSpace(namespaceLine))
                {
                    return string.Empty;
                }

                var lineParts = namespaceLine.Split('{');
                var finalNamespace = lineParts.First().Replace("namespace ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                finalNamespace = finalNamespace.Trim();

                return finalNamespace;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void NotValidDirectory()
        {
            Console.Write("> Your current directory is ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("not valid ");
            Console.ResetColor();
            Console.WriteLine("for generating code.");

            if (!ValidPaths.ContainsKey(Program.CurrentDirectory))
            {
                ValidPaths.Add(Program.CurrentDirectory, false);
            }

            Program.EmptyLine();
        }

        private static void ValidDirectory()
        {
            Console.Write("> Your current directory is ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("valid ");
            Console.ResetColor();
            Console.WriteLine("for generating code.");

            if (!ValidPaths.ContainsKey(Program.CurrentDirectory))
            {
                ValidPaths.Add(Program.CurrentDirectory, true);
            }
        }

        private static void ValidProjectDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.csproj");

                if (!files.Any())
                {
                    NotValidDirectory();
                }

                return;
            }

            NotValidDirectory();
        }

        private static void SetModules()
        {
            var directories = Directory.GetDirectories(Program.CurrentDirectory);
            var dd = directories.First(x => x.Contains(".DAL", StringComparison.InvariantCultureIgnoreCase));
            var modelPath = $"{(dd.EndsWith("\\") ? dd : $"{dd}\\")}Models";
            var modelFilePaths = Directory.GetFiles(modelPath, "*.cs", SearchOption.AllDirectories);

            foreach (var modelFilePath in modelFilePaths)
            {
                var (item1, item2, item3) = PathIsValidModel(modelFilePath);

                if (item3 && !BlackList.Contains(item2) && !Models.ContainsKey(item1))
                {
                    Models.Add(item1, item2);
                }
            }
        }

        private static void PrintModules()
        {
            var index = 1;

            Console.Write("Models: ");

            foreach (var (key, value) in Models)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(value);
                Console.ResetColor();

                Console.Write($"{(Models.Count == index ? string.Empty : ", ")}");

                index++;
            }

            Console.WriteLine();
        }
    }
}
