using System.Threading.Tasks;
using static VIPA_PARSER.Devices.Common.Types;

namespace VIPA_PARSER.Devices.Common
{
    public delegate void DeviceLogHandler(LogLevel logLevel, string message);
    //public delegate void DeviceEventHandler(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
    //public delegate Task ComPortEventHandler(PortEventType comPortEvent, string portNumber);
    public delegate void QueueEventOccured();
}
