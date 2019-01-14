namespace TemplateToolScript
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class ProviderGenerator
    {
        private static string ProviderNamespace { get; set; }

        public static void GenerateProviders()
        {
            GenerateHelper.CheckIfDirectoryIsValid();

            var valid = GenerateHelper.ValidPaths.First(x => x.Key.Equals(Program.CurrentDirectory)).Value;

            if (!valid)
            {
                GenerateHelper.NotValidDirectory();
                return;
            }

            var tab = "    ";
            var createProviders = new List<Tuple<string, string, bool>>();
            var directories = Directory.GetDirectories(Program.CurrentDirectory);
            var dd = directories.First(x => x.Contains(".DAL", StringComparison.InvariantCultureIgnoreCase));
            var pd = directories.First(x => x.Contains(".Providers", StringComparison.InvariantCultureIgnoreCase));
            var modelPath = $"{(dd.EndsWith("\\") ? dd : $"{dd}\\")}Models";

            if (Directory.Exists(modelPath) && Directory.Exists(pd))
            {
                var models = new List<Tuple<string, string, bool>>();
                var modelFilePaths = Directory.GetFiles(modelPath, "*.cs", SearchOption.AllDirectories);
                var providers = ModelProviders(pd);

                foreach (var modelFilePath in modelFilePaths)
                {
                    var result = GenerateHelper.PathIsValidModel(modelFilePath);

                    if (result.Item3 && !GenerateHelper.BlackList.Contains(result.Item2))
                    {
                        models.Add(result);
                    }
                }

                for (var i = 0; i < models.Count; i++)
                {
                    if (providers.ContainsValue(models[i].Item2))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(models[i].Item2);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(models[i].Item2);
                        Console.ResetColor();

                        createProviders.Add(models[i]);
                    }

                    Console.Write($"{(models.Count == i + 1 ? string.Empty : ", ")}");
                }

                Console.WriteLine();
            }

            var baseNamesapce = GetProviderNamespace(pd);

            foreach (var createProvider in createProviders)
            {
                var dalNamespace = GenerateHelper.GetFileNamespace(createProvider.Item1);

                if (string.IsNullOrWhiteSpace(dalNamespace))
                {
                    continue;
                }

                var path = dalNamespace.Split(".Models");

                if (path.Length <= 1)
                {
                    continue;
                }

                var secondPart = path[1];
                var fullPathParts = secondPart.Split('.')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
                var fullNewNamespace = baseNamesapce;
                var fullPath = $"{(pd.EndsWith("\\") ? pd : $"{pd}\\")}";

                foreach (var pathPart in fullPathParts)
                {
                    fullPath += $"{pathPart}\\";
                    fullNewNamespace += $".{pathPart}";
                }

                var filePath = fullPath + $"{createProvider.Item2}Provider.cs";

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                if (!File.Exists(filePath))
                {
                    using (var writter = new StreamWriter(filePath, false))
                    {
                        writter.WriteLine($"namespace {fullNewNamespace}");
                        writter.WriteLine("{");
                        writter.WriteLine($"{tab}using {dalNamespace};");
                        writter.WriteLine($"{tab}using {baseNamesapce}.Base;");
                        writter.WriteLine($"{tab}using {baseNamesapce.Replace(".Providers", ".SharedKernel.Interfaces.PersistenceLayer")};");
                        writter.WriteLine(string.Empty);
                        writter.WriteLine($"{tab}public sealed class {createProvider.Item2}Provider : BaseProvider<{createProvider.Item2}>");
                        writter.WriteLine(tab + "{");
                        writter.WriteLine($"{tab}{tab}public {createProvider.Item2}Provider(IPersistenceLayer persistence)");
                        writter.WriteLine($"{tab}{tab}{tab}: base(persistence)");
                        writter.WriteLine(tab + tab + "{");
                        writter.WriteLine(tab + tab + "}");
                        writter.WriteLine(tab + "}");
                        writter.WriteLine("}");
                    }
                }
            }
        }

        private static Dictionary<string, string> ModelProviders(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                var modelProviders = new Dictionary<string, string>();

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    var providerLine = lines.FirstOrDefault(line => line.Contains(" : BaseProvider", StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrWhiteSpace(providerLine))
                    {
                        continue;
                    }

                    var parts = providerLine.Split(':');
                    var providerPart = parts.FirstOrDefault(part => part.Contains("BaseProvider", StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrWhiteSpace(providerPart))
                    {
                        continue;
                    }

                    var providerModelParts = providerPart.Split(',');
                    var providerModel = providerModelParts.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(providerModel))
                    {
                        continue;
                    }

                    providerModel = providerModel.Replace("BaseProvider<", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    providerModel = providerModel.Replace(">", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    providerModel = providerModel.Trim();

                    modelProviders.Add(file, providerModel);
                }

                return modelProviders;
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static string GetProviderNamespace(string path)
        {
            try
            {
                var cachedNamespace = ProviderNamespace;

                if (!string.IsNullOrWhiteSpace(cachedNamespace))
                {
                    return cachedNamespace;
                }

                var files = Directory.GetFiles(path, "*Provider.cs", SearchOption.AllDirectories);

                if (!files.Any())
                {
                    return string.Empty;
                }

                var firstFile = files.First();
                var lines = File.ReadAllLines(firstFile);
                var namespaceLine = lines.FirstOrDefault(line => line.Contains("namespace ", StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrWhiteSpace(namespaceLine))
                {
                    return string.Empty;
                }

                var lineParts = namespaceLine.Split('{');
                var finalNamespace = lineParts.First().Replace("namespace ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                finalNamespace = finalNamespace.Trim();

                lineParts = finalNamespace.Split(".Providers");

                var fullNamespace = $"{lineParts[0].Trim()}.Providers".Trim();

                ProviderNamespace = fullNamespace;

                return ProviderNamespace;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
