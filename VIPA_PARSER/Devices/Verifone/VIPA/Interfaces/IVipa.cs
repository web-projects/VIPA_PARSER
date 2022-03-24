using System;
using VIPA_PARSER.Devices.Common;

namespace VIPA_PARSER.Devices.Verifone.VIPA.Interfaces
{
    public interface IVipa : IDisposable
    {
        public void Connect(DeviceLogHandler deviceLogHandler, DeviceInformation deviceInformation);
        public void Disconnect();
        public void ProcessCommand(string vipaResponse);
    }
}
