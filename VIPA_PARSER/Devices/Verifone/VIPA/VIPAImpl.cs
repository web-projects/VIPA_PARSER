using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VIPA_PARSER.Common.XO.Common.DAL;
using VIPA_PARSER.Common.XO.Requests.DAL;
using VIPA_PARSER.Common.XO.Responses.DAL;
using VIPA_PARSER.Devices.Common;
using VIPA_PARSER.Devices.Common.Helpers;
using VIPA_PARSER.Devices.Common.Helpers.Templates;
using VIPA_PARSER.Devices.Verifone.Connection;
using VIPA_PARSER.Devices.Verifone.Helpers;
using VIPA_PARSER.Devices.Verifone.VIPA.Helpers;
using VIPA_PARSER.Devices.Verifone.VIPA.Interfaces;
using VIPA_PARSER.Devices.Verifone.VIPA.TagLengthValue;
using static VIPA_PARSER.Devices.Common.Types;

namespace VIPA_PARSER.Devices.Verifone.VIPA
{
    public class VIPAImpl : IVipa
    {
        public DeviceInformation DeviceInformation { get; set; }
        private SerialConnection VerifoneConnection;

        // EMV Workflow
        //public TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)> DecisionRequiredInformation = null;

        public delegate void ResponseTagsHandlerDelegate(List<TLV> tags, int responseCode, bool cancelled = false);
        internal ResponseTagsHandlerDelegate ResponseTagsHandler = null;

        public delegate void ResponseTaglessHandlerDelegate(byte[] data, int dataLength, int responseCode, bool cancelled = false);
        internal ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;

        public delegate void ResponseCLessHandlerDelegate(List<TLV> tags, int responseCode, int pcb, bool cancelled = false);
        internal ResponseCLessHandlerDelegate ResponseCLessHandler = null;

        public TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)> DeviceIdentifier = null;

        public event DeviceLogHandler DeviceLogHandler;

        public void Connect(DeviceLogHandler deviceLogHandler, DeviceInformation deviceInformation)
        {
            DeviceLogHandler = deviceLogHandler;
            DeviceInformation = deviceInformation;
            VerifoneConnection = new SerialConnection(deviceLogHandler);
        }

        public void Disconnect()
        {

        }

        public void Dispose()
        {

        }

        #region --- Utilities ---

        public byte IntToBCDByte(int numericValue)
        {
            return BCDConversion.IntToBCDByte(numericValue);
        }

        public byte[] IntToBCD(int numericValue, int byteSize = 6)
        {
            return BCDConversion.IntToBCD(numericValue, byteSize);
        }

        public int BCDToInt(byte[] bcd)
        {
            return BCDConversion.BCDToInt(bcd);
        }

        private void DeviceLogger(LogLevel logLevel, string message) =>
            DeviceLogHandler?.Invoke(logLevel, $"{StringValueAttribute.GetStringValue(DeviceType.Verifone)}[{DeviceInformation?.Model}, {DeviceInformation?.SerialNumber}, {DeviceInformation?.ComPort}]: {{{message}}}");

        #endregion --- Utilities ---

        #region --- Template Processing ---
        private void EETemplateProcessing(LinkDALRequestIPA5Object cardInfo, LinkDeviceResponse deviceResponse, TLV tag)
        {
            //Verifone payment devices with multiple devices return an EE template for each device, e.g. UX301 has a UX100 pinpad.
            //The first EE template has the payment device serial #/model. Second EE is the pin pad device.

            foreach (TLV dataTag in tag.InnerTags)
            {
                if (dataTag.Tag == EETemplate.TerminalName && string.IsNullOrWhiteSpace(deviceResponse.Model))
                {
                    deviceResponse.Model = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == EETemplate.SerialNumber && string.IsNullOrWhiteSpace(deviceResponse.SerialNumber))
                {
                    deviceResponse.SerialNumber = Encoding.UTF8.GetString(dataTag.Data);
                    DeviceInformation.SerialNumber = deviceResponse.SerialNumber ?? string.Empty;
                }
                else if (dataTag.Tag == EETemplate.TamperStatus)
                {
                    //DF8101 = 00 no tamper detected
                    //DF8101 = 01 tamper detected
                    cardInfo.TamperStatus = BitConverter.ToString(dataTag.Data).Replace("-", "");
                }
                else if (dataTag.Tag == EETemplate.ArsStatus)
                {
                    //DF8102 = 00 ARS not active
                    //DF8102 = 01 ARS active
                    cardInfo.ArsStatus = BitConverter.ToString(dataTag.Data).Replace("-", "");
                }
            }

            // VOS Versions: sequentially paired TAG/VALUE sets
            List<TLV> vosVersions = tag.InnerTags.Where(x => x.Tag == EFTemplate.EMVLibraryName || x.Tag == EFTemplate.EMVLibraryVersion).ToList();

            if (vosVersions is { } && vosVersions.Count > 0)
            {
                // Vault
                int vaultVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKVault, StringComparison.OrdinalIgnoreCase));
                if (vaultVersionIndex != -1 && vosVersions.Count > vaultVersionIndex + 1 && vosVersions.ElementAt(vaultVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    //DeviceInformation.VOSVersions.ADKVault = Encoding.UTF8.GetString(vosVersions.ElementAt(vaultVersionIndex + 1).Data);
                }
                // AppManager
                int appManagerVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKAppManager, StringComparison.OrdinalIgnoreCase));
                if (appManagerVersionIndex != -1 && vosVersions.Count > appManagerVersionIndex + 1 && vosVersions.ElementAt(appManagerVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    //DeviceInformation.VOSVersions.ADKAppManager = Encoding.UTF8.GetString(vosVersions.ElementAt(appManagerVersionIndex + 1).Data);
                }
                // OpenProtocol
                int openProtocolVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKOpenProtocol, StringComparison.OrdinalIgnoreCase));
                if (openProtocolVersionIndex != -1 && vosVersions.Count > openProtocolVersionIndex + 1 && vosVersions.ElementAt(openProtocolVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    //DeviceInformation.VOSVersions.ADKOpenProtocol = Encoding.UTF8.GetString(vosVersions.ElementAt(openProtocolVersionIndex + 1).Data);
                }
                // SRED
                int sREDVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKSRED, StringComparison.OrdinalIgnoreCase));
                if (sREDVersionIndex != -1 && vosVersions.Count > sREDVersionIndex + 1 && vosVersions.ElementAt(sREDVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    //DeviceInformation.VOSVersions.ADKSRED = Encoding.UTF8.GetString(vosVersions.ElementAt(sREDVersionIndex + 1).Data);
                }
            }

            //TODO: this would have to be soft-coded for other countries/currencies
            cardInfo.TerminalCountryCode = BitConverter.ToString(EETemplate.CountryCodeUS).Replace("-", "");
        }
        #endregion --- Template Processing ---

        #region --- response handlers ---
        private void GetDeviceInfoResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceIdentifier?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new LinkDeviceResponse
            {
                // TODO: rework to be values reflecting actual device capabilities
                CardWorkflowControls = new LinkCardWorkflowControls
                {
                    CardCaptureTimeout = 90,
                    ManualCardTimeout = 5,
                    DebitEnabled = false,
                    EMVEnabled = false,
                    ContactlessEnabled = false,
                    ContactlessEMVEnabled = false,
                    CVVEnabled = false,
                    VerifyAmountEnabled = false,
                    AVSEnabled = false,
                    SignatureEnabled = false
                }
            };

            LinkDALRequestIPA5Object cardInfo = new LinkDALRequestIPA5Object();

            foreach (TLV tag in tags)
            {
                if (tag.Tag == EETemplate.EETemplateTag)
                {
                    EETemplateProcessing(cardInfo, deviceResponse, tag);
                }
                else if (tag.Tag == EETemplate.TerminalId)
                {
                    deviceResponse.TerminalId = Encoding.UTF8.GetString(tag.Data);
                }
                else if (tag.Tag == EFTemplate.EFTemplateTag)
                {
                    bool isEMVKernel = false;
                    foreach (TLV dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == EFTemplate.WhiteListHash)
                        {
                            cardInfo.WhiteListHash = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == EFTemplate.FirmwareVersion && string.IsNullOrWhiteSpace(deviceResponse.FirmwareVersion))
                        {
                            deviceResponse.FirmwareVersion = Encoding.UTF8.GetString(dataTag.Data);
                            DeviceInformation.FirmwareVersion = deviceResponse.FirmwareVersion;
                        }
                        else if (dataTag.Tag == EFTemplate.EMVLibraryName)
                        {
                            string libraryName = Encoding.UTF8.GetString(dataTag.Data);
                            isEMVKernel = libraryName.Equals(EFTemplate.ADKEVMKernel, StringComparison.OrdinalIgnoreCase);
                        }
                        else if (dataTag.Tag == EFTemplate.EMVLibraryVersion && isEMVKernel)
                        {
                            DeviceInformation.EMVL2KernelVersion = Encoding.UTF8.GetString(dataTag.Data);
                            isEMVKernel = false;
                        }
                    }
                }
                else if (tag.Tag == E6Template.E6TemplateTag)
                {
                    deviceResponse.PowerOnNotification = new LinkDevicePowerOnNotification();

                    List<TLV> _tags = TLV.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag == E6Template.TransactionStatus)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag == E6Template.TransactionStatusMessage)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatusMessage = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EETemplate.TerminalId)
                        {
                            deviceResponse.PowerOnNotification.TerminalID = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags?.Count > 0)
                {
                    DeviceInfoObject deviceInfoObject = new DeviceInfoObject
                    {
                        LinkDeviceResponse = deviceResponse,
                        LinkDALRequestIPA5Object = cardInfo
                    };
                    DeviceIdentifier?.TrySetResult((deviceInfoObject, responseCode));
                }
            }
            else
            {
                // log error responses for device troubleshooting purposes
                DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4} - HANDLER 002", responseCode));
                DeviceIdentifier?.TrySetResult((null, responseCode));
            }
        }
        #endregion --- response handlers ---

        #region --- interface ---
        public void ProcessCommand(string vipaResponse)
        {
            DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            ResponseTagsHandler += GetDeviceInfoResponseHandler;
            VerifoneConnection.SetTagHandlers(ResponseTagsHandler, null, null);
            VerifoneConnection.SerialPort_DataReceived(ConversionHelper.HexToByteArray(vipaResponse));

            //deviceResponse = DeviceIdentifier.Task.Result;
            ResponseTagsHandler -= GetDeviceInfoResponseHandler;
        }
        #endregion --- interface ---
    }
}
