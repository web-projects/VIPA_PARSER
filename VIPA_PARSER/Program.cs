using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VIPA_PARSER.Devices.Common;
using VIPA_PARSER.Devices.Verifone;
using VIPA_PARSER.Devices.Verifone.VIPA;
using VIPA_PARSER.DTE;
using static VIPA_PARSER.Devices.Common.Types;

namespace VIPA_PARSER
{
    class Program
    {
        static DeviceLogHandler deviceLogHandler = DeviceLogger;

        static void Main(string[] args)
        {
            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            // TEST ONLY
            //LoadDebuggerAutomation();

            //int value = 0x01;
            //bool ischained = (value & 0x01) == 0x01;
            //value = 0x40;
            //bool isNotchained = (value & 0x01) == 0x00;
            //value = 0x41;
            //bool isNotKnown = (value & 0x00) == 0x00;

            ConfigurationLoad(0);
        }

        static void LoadDebuggerAutomation()
        {
            string programName = Assembly.GetEntryAssembly().GetName().Name;
            string exeLocation = Assembly.GetExecutingAssembly().GetName().CodeBase.Replace(".dll", ".exe");

            List<System.Diagnostics.Process> processes = new List<System.Diagnostics.Process>();

            System.Diagnostics.Process myProgram = LoadDTE.LoadIfNotRunning(programName, exe: exeLocation);
            processes.Add(myProgram);

            LoadDTE.Attach(myProgram);

            Console.WriteLine("Press any key to shut down...");
            Console.ReadKey();
            foreach (var p in processes)
            {
                try
                {
                    Console.WriteLine("Killing " + p.ProcessName);
                    p.Kill();
                }
                catch (Exception ex)
                {
                    try
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Closing " + p.ProcessName);
                        p.Close();
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.Message);
                    }
                }
            }
        }

        static void ConfigurationLoad(int index)
        {
            // Get appsettings.json config.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // VIPA DATA GROUP
            VIPADataGroup(configuration, index);
        }

        static void VIPADataGroup(IConfiguration configuration, int index)
        {
            var vipaPayload = configuration.GetSection("VIPADataGroup")
                    .GetChildren()
                    .ToList()
                    .Select(x => new
                    {
                        VIPACommand = x.GetValue<string>("command"),
                        VIPAResponse = x.GetValue<string>("response"),
                    });

            // Is there a matching item?
            if (vipaPayload.Count() > index)
            {
                string vipaCommand = vipaPayload.ElementAt(index).VIPACommand.Replace("-", string.Empty);
                string vipaResponse = vipaPayload.ElementAt(index).VIPAResponse.Replace("-", string.Empty);

                try
                {
                    Console.WriteLine($"===== [ VIPA RESPONSE PARSER ] =====");
                    DeviceInformation deviceInformation = new DeviceInformation();
                    VerifoneDevice device = new VerifoneDevice(deviceLogHandler, deviceInformation);
                    device.ProcessCommand(vipaResponse);
                    if (!string.IsNullOrEmpty(deviceInformation.SerialNumber))
                    {
                        DeviceLogger(LogLevel.Info, $"====== [DEVICE INFORMATION] ======");
                        DeviceLogger(LogLevel.Info, $"device serial number : {deviceInformation.SerialNumber}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"EXCEPTION: {e.Message}");
                }
            }
        }

        public static void DeviceLogger(LogLevel logLevel, string message)
        {
            Console.WriteLine($"[{logLevel}]: {message}");
        }
    }
}
