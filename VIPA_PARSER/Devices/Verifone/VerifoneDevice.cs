using System;
using System.Collections.Generic;
using System.Text;
using VIPA_PARSER.Devices.Common;
using VIPA_PARSER.Devices.Verifone.VIPA;
using VIPA_PARSER.Devices.Verifone.VIPA.Interfaces;

namespace VIPA_PARSER.Devices.Verifone
{
    public class VerifoneDevice
    {
        private DeviceLogHandler DeviceLogHandler { get; set; }
        private DeviceInformation DeviceInformation { get; set; }

        internal IVipa VipaConnection { get; private set; } = new VIPAImpl();

        public VerifoneDevice(DeviceLogHandler deviceLogHandler, DeviceInformation deviceInformation)
        {
            DeviceLogHandler = deviceLogHandler;
            DeviceInformation = deviceInformation;
        }

        public void ProcessCommand(string vipaCommand)
        {
            VipaConnection.Connect(DeviceLogHandler, DeviceInformation);
            VipaConnection.ProcessCommand(vipaCommand);
            VipaConnection.Disconnect();
        }
    }
}
