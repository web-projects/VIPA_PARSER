using System;
using System.Text;
using VIPA_PARSER.Devices.Common;
using VIPA_PARSER.Devices.Verifone.VIPA;
using static VIPA_PARSER.Devices.Common.Types;

namespace VIPA_PARSER.Devices.Verifone.Connection
{
    public class SerialConnection
    {
#if DEBUG
        internal const bool LogSerialBytes = true;
#else
        internal const bool LogSerialBytes = false;
#endif

        private readonly IVIPASerialParser serialParser;

        internal VIPAImpl.ResponseTagsHandlerDelegate ResponseTagsHandler = null;
        internal VIPAImpl.ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;
        internal VIPAImpl.ResponseCLessHandlerDelegate ResponseContactlessHandler = null;

        // optimize serial port read buffer size based on expected response
        private const int unchainedResponseMessageSize = 1024;
        private const int chainedResponseMessageSize = unchainedResponseMessageSize * 10;
        private const int portReadIdleDelayMs = 10;
        private const int chainedCommandMinimumLength = 0xFE;
        private const int chainedCommandPayloadLength = 0xF8;

        private bool IsChainedMessageResponse { get; set; }

        private bool IsChainedResponseCommand(VIPACommand command) =>
            ((VIPACommandType)(command.cla << 8 | command.ins) == VIPACommandType.ResetDevice) ||
            ((VIPACommandType)(command.cla << 8 | command.ins) == VIPACommandType.DisplayHTML && command.data != null &&
            Encoding.UTF8.GetString(command.data).IndexOf(VIPACommand.ChainedResponseAnswerData, StringComparison.OrdinalIgnoreCase) >= 0);

        public SerialConnection(DeviceLogHandler deviceLogHandler)
        {
            serialParser = new VIPASerialParserImpl(deviceLogHandler, "COM1");
        }

        private void ReadExistingResponseBytes(byte[] buffer)
        {
            if (buffer.Length > 0)
            {
                int readLength = buffer.Length;
                serialParser.BytesRead(buffer, readLength);
                serialParser.ReadAndExecute(ResponseTagsHandler, ResponseTaglessHandler, ResponseContactlessHandler, IsChainedMessageResponse);
                serialParser.SanityCheck();
            }
        }

        public void SetTagHandlers(VIPAImpl.ResponseTagsHandlerDelegate responseTagsHandler,
            VIPAImpl.ResponseTaglessHandlerDelegate responseTaglessHandler,
            VIPAImpl.ResponseCLessHandlerDelegate responseContactlessHandler)
        {
            ResponseTagsHandler = responseTagsHandler;
            ResponseTaglessHandler = responseTaglessHandler;
            ResponseContactlessHandler = responseContactlessHandler;
        }

        public void SerialPort_DataReceived(byte[] buffer)
        {
            ReadExistingResponseBytes(buffer);
        }
    }
}
