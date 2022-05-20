using DTE_ATTACHER.DTE;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DTE_ATTACHER
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            List<string> processesList = ConfigurationLoad(0);

            ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();
            do
            { 
                if (LoadDebuggerAutomation(processesList))
                { 
                    Console.WriteLine("\r\nPRESS <ENTER> to RERUN\r\nPRESS <ESC> to QUIT\r\n");
                    keyPressed = Console.ReadKey(true);
                }
                else
                {
                    Console.WriteLine("\r\n\r\nUSER ABORTED PROCESS!");
                    break;
                }

            } while (keyPressed.Key != ConsoleKey.Escape);
        }

        static bool LoadDebuggerAutomation(List<string> processesList)
        {
            ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();

            foreach (string targetProcess in processesList)
            {
                Console.Write($"Waiting for process {targetProcess} ...");

                while (!DTEAttacher.Attach(targetProcess))
                {
                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        keyPressed = Console.ReadKey(true);
                        if (keyPressed.Key == ConsoleKey.Escape)
                        {
                            return false;
                        }
                    }
                }

                if (keyPressed.Key != ConsoleKey.Escape)
                {
                    Console.WriteLine(" attached!");
                }
            }

            return keyPressed.Key != ConsoleKey.Escape;
        }

        static List<string> ConfigurationLoad(int index)
        {
            // Get appsettings.json config.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Processes
            return ProcessesGroup(configuration, index);
        }

        static List<string> ProcessesGroup(IConfiguration configuration, int index)
        {
            List<string> targetProceses = new List<string>();

            var processesPayload = configuration.GetSection("Processes")
                    .GetChildren()
                    .ToList()
                    .Select(x => new
                    {
                        ProcessName = x.GetValue<string>("Name"),
                    });

            // Is there a matching item?
            if (processesPayload.Count() > index)
            {
                targetProceses.AddRange(from value in processesPayload
                                        select processesPayload.ElementAt(index++).ProcessName);
            }

            Console.WriteLine("LIST OF PROCESSES TO ATTACH TO VS DEBUGGER\r\n");
            foreach (string process in targetProceses)
            {
                Console.WriteLine($"{process}");
            }
            Console.WriteLine("");

            return targetProceses;
        }
    }
}
