﻿using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using VIPA_PARSER.Devices.Common;
using VIPA_PARSER.Devices.Common.Helpers;
using VIPA_PARSER.Devices.Verifone.Connection;
using VIPA_PARSER.Devices.Verifone.VIPA;
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

            ConfigurationLoad(0);
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
                string vipaCommand = vipaPayload.ElementAt(index).VIPACommand;
                string vipaResponse = vipaPayload.ElementAt(index).VIPAResponse.Replace("-", string.Empty);

                try
                {
                    //1234567890|1234567890|12345
                    Console.WriteLine($"==== [ VIPA RESPONSE PARSER ] ====");
                    DeviceInformation deviceInformation = new DeviceInformation();
                    VIPAImpl vipa = new VIPAImpl(deviceLogHandler, deviceInformation);
                    vipa.ProcessCommand(vipaResponse);
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