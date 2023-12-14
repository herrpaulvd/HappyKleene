using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HappyKleene
{
    class Program
    {
        // первая опция - /test | /compile
        static string GetOption(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("You can choose one of these options:");
                Console.WriteLine("/test: select project and file to parse and echo result parsing tree to <projectname>.log.txt");
                Console.WriteLine("/compile: select project and build a DLL with parsing code of this project");
                Console.Write("Your decision: ");
                return Console.ReadLine().Trim(); 
            }
            return args[0];
        }

        // чтение полного имени проекта
        static string GetFullProjectName(string[] args)
        {
            string n;
            if(args.Length < 2)
            {
                Console.WriteLine("Project is a group of two files at the same directory: <projectname>.lex and <projectname>.syn");
                Console.WriteLine("You should write your full project name: it is the path to <projectname>.lex without '.lex'");
                Console.Write("Full project name: ");
                n = Console.ReadLine().Trim();
            }
            else n = args[1];
            return new FileInfo(n).FullName;
        }

        // чтение имени файла для интепретации
        static string GetTextLocation(string[] args)
        {
            if(args.Length < 3)
            {
                Console.WriteLine("You should select a file to view his parsing tree");
                Console.Write("File path: ");
                return Console.ReadLine().Trim();
            }
            return args[2];
        }

        // вспомогательный метод, получающий из входных данных некоторые другие
        static bool CheckFullProjectName(string fullpname, out string directory, out string projectname, out string lexcode, out string syncode)
        {
            directory = null;
            projectname = null;
            lexcode = null;
            syncode = null;
            var lexfname = fullpname + ".lex";
            var lexInfo = new FileInfo(lexfname);
            if (!lexInfo.Exists) return false;
            var synfname = fullpname + ".syn";
            var synInfo = new FileInfo(synfname);
            if (!synInfo.Exists) return false;

            directory = lexInfo.DirectoryName + "\\";
            int start = lexInfo.DirectoryName.Length + 1;
            projectname = fullpname.Substring(start, fullpname.Length - start);
            lexcode = File.ReadAllText(lexfname);
            syncode = File.ReadAllText(synfname);
            return true;
        }

        // общий метод обработки интерпретации
        static void PrepareTest(string[] args)
        {
            string fullProjectName = GetFullProjectName(args);
            if (CheckFullProjectName(fullProjectName, out string directory, out string projectname, out string lexcode, out string syncode))
            {
                var testfname = GetTextLocation(args);
                var info = new FileInfo(testfname);
                if (info.Exists)
                {
                    var txtfile = File.ReadAllText(testfname);
                    var error = Interpreter.Run(lexcode, syncode, txtfile, projectname, directory);
                    if (error is object)
                        Console.WriteLine("There's an error: " + error);
                }
                else Console.WriteLine("Invalid test location");
            }
            else Console.WriteLine("Invalid projectname");
        }

        // обработка компиляции
        static void PrepareCompile(string[] args)
        {
            string fullProjectName = GetFullProjectName(args);
            if (CheckFullProjectName(fullProjectName, out string directory, out string projectname, out string lexcode, out string syncode))
            {
                var error = Compiler.Build(lexcode, syncode, projectname, directory);
                if (error is object)
                    Console.WriteLine("There's an error: " + error);
            }
            else Console.WriteLine("Invalid projectname");
        }

        static void Main(string[] args)
        {
            string option = GetOption(args);
            switch(option)
            {
                case "/test":
                    PrepareTest(args);
                    break;
                case "/compile":
                    PrepareCompile(args);
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }

            Console.WriteLine("Finished. Press any key to continue...");
            Console.ReadKey();
        }
    }
}
