namespace TemplateToolScript
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class ServiceGenerator
    {
        private static string ServiceNamespace { get; set; }

        public static void GenerateServices()
        {
            GenerateHelper.CheckIfDirectoryIsValid();

            var valid = GenerateHelper.ValidPaths.First(x => x.Key.Equals(Program.CurrentDirectory)).Value;

            if (!valid)
            {
                GenerateHelper.NotValidDirectory();
                return;
            }

            var tab = "    ";
            var createServices = new List<Tuple<string, string, bool>>();
            var directories = Directory.GetDirectories(Program.CurrentDirectory);
            var dd = directories.First(x => x.Contains(".DAL", StringComparison.InvariantCultureIgnoreCase));
            var sd = directories.First(x => x.Contains(".Services", StringComparison.InvariantCultureIgnoreCase));
            var modelPath = $"{(dd.EndsWith("\\") ? dd : $"{dd}\\")}Models";

            if (Directory.Exists(modelPath) && Directory.Exists(sd))
            {
                var models = new List<Tuple<string, string, bool>>();
                var modelFilePaths = Directory.GetFiles(modelPath, "*.cs", SearchOption.AllDirectories);
                var services = ModelServices(sd);

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
                    if (services.ContainsValue(models[i].Item2))
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

                        createServices.Add(models[i]);
                    }

                    Console.Write($"{(models.Count == i + 1 ? string.Empty : ", ")}");
                }

                Console.WriteLine();
            }

            var baseNamesapce = GetServiceNamespace(sd);

            foreach (var createService in createServices)
            {
                var dalNamespace = GenerateHelper.GetFileNamespace(createService.Item1);

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
                var fullPath = $"{(sd.EndsWith("\\") ? sd : $"{sd}\\")}";

                foreach (var pathPart in fullPathParts)
                {
                    fullPath += $"{pathPart}\\";
                    fullNewNamespace += $".{pathPart}";
                }

                var filePath = fullPath + $"{createService.Item2}Service.cs";

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
                        writter.WriteLine($"{tab}using {baseNamesapce.Replace(".Services", ".SharedKernel.Interfaces.PersistenceLayer")};");
                        writter.WriteLine(string.Empty);
                        writter.WriteLine($"{tab}public sealed class {createService.Item2}Service : BaseService<{createService.Item2}>");
                        writter.WriteLine(tab + "{");
                        writter.WriteLine($"{tab}{tab}public {createService.Item2}Service(IPersistenceLayer persistence)");
                        writter.WriteLine($"{tab}{tab}{tab}: base(persistence)");
                        writter.WriteLine(tab + tab + "{");
                        writter.WriteLine(tab + tab + "}");
                        writter.WriteLine(tab + "}");
                        writter.WriteLine("}");
                    }
                }
            }
        }

        private static Dictionary<string, string> ModelServices(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                var modelServices = new Dictionary<string, string>();

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    var serviceLine = lines.FirstOrDefault(line => line.Contains(" : BaseService", StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrWhiteSpace(serviceLine))
                    {
                        continue;
                    }

                    var parts = serviceLine.Split(':');
                    var servicePart = parts.FirstOrDefault(part => part.Contains("BaseService", StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrWhiteSpace(servicePart))
                    {
                        continue;
                    }

                    var providerModelParts = servicePart.Split(',');
                    var serviceModel = providerModelParts.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(serviceModel))
                    {
                        continue;
                    }

                    serviceModel = serviceModel.Replace("BaseService<", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    serviceModel = serviceModel.Replace(">", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    serviceModel = serviceModel.Trim();

                    modelServices.Add(file, serviceModel);
                }

                return modelServices;
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static string GetServiceNamespace(string path)
        {
            try
            {
                var cachedNamespace = ServiceNamespace;

                if (!string.IsNullOrWhiteSpace(cachedNamespace))
                {
                    return cachedNamespace;
                }

                var files = Directory.GetFiles(path, "*Service.cs", SearchOption.AllDirectories);

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

                lineParts = finalNamespace.Split(".Services");

                var fullNamespace = $"{lineParts[0].Trim()}.Services".Trim();

                ServiceNamespace = fullNamespace;

                return ServiceNamespace;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
