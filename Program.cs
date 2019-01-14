namespace TemplateToolScript
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualBasic;

    public class Program
    {
        public static List<Tuple<string, string, bool>> Actions
            => new List<Tuple<string, string, bool>>
            {
                new Tuple<string, string, bool>("cd", "Changes your current directory", false),
                new Tuple<string, string, bool>("ls", "Provides an overview of all directories and files in the current directory", false),
                new Tuple<string, string, bool>("help", "Show an overview with available commands", false),
                new Tuple<string, string, bool>("generate", "Module 'generate' is for generating code", true),
                new Tuple<string, string, bool>("clear", "Clears the console", false),
                new Tuple<string, string, bool>("quit", "Close the console", false),
            };

        public static string CurrentDirectory { get; set; }

        public static void Main(string[] args)
        {
            CurrentDirectory = Environment.CurrentDirectory;

            WelcomeMessage();
        }

        public static void Action(string command)
        {
            command = command.Trim();

            if (string.IsNullOrWhiteSpace(command))
            {
                EmptyLine();
            }

            var tab = Constants.vbTab;
            var actions = command.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            var actionsByQuotes = command.Split('"')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!actions.Any())
            {
                EmptyLine();
                return;
            }

            if (command.Contains('"'))
            {
                actions = new List<string> { actions.First() };
                var newParameters = actionsByQuotes.Where(para =>
                    !para.Trim().Equals(actions.First()));
                actions.AddRange(newParameters);
            }

            switch (actions.First().ToLower())
            {
                case "cd":
                    DirectoryHelper.UpdateDirectory(actions);
                    return;
                case "ls":
                    DirectoryHelper.ShowFilesInDirectory();
                    return;
                case "generate":
                    GenerateHelper.Actions(actions);
                    return;
                case "clear":
                    Console.Clear();
                    WelcomeMessage();
                    return;
                case "quit":
                case "exit":
                    Environment.Exit(1);
                    return;
                case "help":
                    WriteTitle("Overview of all available commands:");

                    var longestLength = Actions
                        .OrderByDescending(x => x.Item1.Length)
                        .First().Item1.Length;

                    foreach (var action in Actions.Where(m => !m.Item3))
                    {
                        Console.Write(" - ");
                        Console.ForegroundColor = ConsoleColor.Magenta;

                        var label = action.Item1;

                        while (label.Length < longestLength)
                        {
                            label += " ";
                        }

                        Console.Write(label);
                        Console.ResetColor();
                        Console.WriteLine($" {tab} {action.Item2}");
                    }

                    WriteTitle("Overview of all available modules:");

                    foreach (var action in Actions.Where(m => m.Item3))
                    {
                        Console.Write(" - ");
                        Console.ForegroundColor = ConsoleColor.Magenta;

                        var label = action.Item1;

                        while (label.Length < longestLength)
                        {
                            label += " ";
                        }

                        Console.Write(label);
                        Console.ResetColor();
                        Console.WriteLine($" {tab} {action.Item2}");
                    }

                    Console.WriteLine(string.Empty);
                    EmptyLine();
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("> The system does not recognize your specified command");
                    Console.ResetColor();
                    EmptyLine();
                    return;
            }
        }

        public static void EmptyLine()
        {
            Console.Write($"{CurrentDirectory}> ");
            var command = Console.ReadLine();
            Action(command);
        }

        public static void WelcomeMessage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("NetCoreTemplateTool [Version 0.0.1]");
            Console.WriteLine("(c) 2018 ThymonA. All rights reserved.");
            Console.WriteLine(string.Empty);
            Console.Write($"{CurrentDirectory}> ");
            var command = Console.ReadLine();
            Action(command);
        }

        public static void WriteTitle(
            string title,
            bool emptyTop = true,
            bool emptyBottom = true)
        {
            if (emptyTop)
            {
                Console.WriteLine(string.Empty);
            }

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(" ");
            Console.ResetColor();
            Console.WriteLine($" {title}");

            if (emptyBottom)
            {
                Console.WriteLine(string.Empty);
            }
        }
    }
}
