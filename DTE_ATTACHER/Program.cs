using DTE_ATTACHER.DTE;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
                Task<bool> taskResult = LoadDebuggerAutomation(processesList);

                if (taskResult.Result)
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

        static async Task<bool> AttacherTask(string targetProcess)
        {
            bool escapeKeyPressed = false;

            await Task.Run(() =>
            {
                Console.Write($"Waiting for process {targetProcess} ...");

                while (!DTEAttacher.Attach(targetProcess))
                {
                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                        if (keyPressed.Key == ConsoleKey.Escape)
                        {
                            Console.WriteLine("");
                            escapeKeyPressed = true;
                            break;
                        }
                    }
                }

                if (!escapeKeyPressed)
                {
                    Console.WriteLine(" attached!");
                }

            });

            return escapeKeyPressed;
        }

        static async Task<bool> LoadDebuggerAutomation(List<string> processesList)
        {
            bool escapeKeyPressed = false;

            foreach (string targetProcess in processesList)
            {
                escapeKeyPressed = await AttacherTask(targetProcess);
                if (escapeKeyPressed)
                {
                    break;
                }
            }

            return escapeKeyPressed;
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
