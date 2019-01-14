namespace TemplateToolScript
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.VisualBasic;

    public static class DirectoryHelper
    {
        public static void UpdateDirectory(List<string> actions)
        {
            if (actions.Count <= 1)
            {
                Program.EmptyLine();
                return;
            }

            var path = actions[1];

            if (path.Trim().Equals(".."))
            {
                if (ParentDirectoryExists(Program.CurrentDirectory))
                {
                    Program.CurrentDirectory = Directory.GetParent(Program.CurrentDirectory).FullName;
                    Program.EmptyLine();
                    return;
                }
            }

            if (Directory.Exists(path))
            {
                Program.CurrentDirectory = path;
                Program.EmptyLine();
                return;
            }

            var cd = Program.CurrentDirectory;
            var newPath = $"{(cd.EndsWith("\\") ? cd : $"{cd}\\")}{path}";

            if (Directory.Exists(newPath))
            {
                Program.CurrentDirectory = newPath;
                Program.EmptyLine();
                return;
            }

            if (string.Equals(path, "--default", StringComparison.InvariantCultureIgnoreCase))
            {
                Program.CurrentDirectory = Environment.CurrentDirectory;
                Program.EmptyLine();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("> The system can not find the specified path.");
            Console.ResetColor();
            Program.EmptyLine();
        }

        public static void ShowFilesInDirectory()
        {
            var tab = Constants.vbTab;
            var files = Directory.GetFiles(Program.CurrentDirectory);
            var directories = Directory.GetDirectories(Program.CurrentDirectory);

            foreach (var directory in directories)
            {
                var finalDirectory = directory.Replace(Program.CurrentDirectory, string.Empty);

                while (finalDirectory.StartsWith("\\"))
                {
                    finalDirectory = finalDirectory.Substring(1);
                }

                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("DIR ");
                Console.ResetColor();
                Console.WriteLine($" {tab} {finalDirectory}");
            }

            foreach (var file in files)
            {
                var finalFile = file.Replace(Program.CurrentDirectory, string.Empty);

                while (finalFile.StartsWith("\\"))
                {
                    finalFile = finalFile.Substring(1);
                }

                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("FILE");
                Console.ResetColor();
                Console.WriteLine($" {tab} {finalFile}");
            }

            Program.EmptyLine();
        }

        public static bool ParentDirectoryExists(string dir)
        {
            var dirInfo = Directory.GetParent(dir);

            return dirInfo != null && dirInfo.Exists;
        }
    }
}
