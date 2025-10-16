using DeviceWrappers.Core.Connections;

namespace DeviceWrappers.Devices.Printer
{
    /// <summary>
    /// USB принтер
    /// </summary>
    public class UsbPrinter : GenericPrinter
    {
        public int VendorId { get; }
        public int ProductId { get; }

        public UsbPrinter(string deviceId, string deviceName, int vendorId, int productId)
            : base(deviceId, deviceName, new UsbConnection(vendorId, productId))
        {
            VendorId = vendorId;
            ProductId = productId;
        }

        public static UsbPrinter Create(int vendorId, int productId, string name = null)
        {
            string deviceId = $"USB_PRINTER_{vendorId:X4}_{productId:X4}";
            string deviceName = name ?? $"USB Printer (VID={vendorId:X4}, PID={productId:X4})";
            return new UsbPrinter(deviceId, deviceName, vendorId, productId);
        }
    }
}

