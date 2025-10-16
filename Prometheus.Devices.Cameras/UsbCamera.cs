using Prometheus.Devices.Core.Connections;

namespace DeviceWrappers.Devices.Camera
{
    /// <summary>
    /// USB-камера
    /// </summary>
    public class UsbCamera : GenericCamera
    {
        public int VendorId { get; }
        public int ProductId { get; }

        public UsbCamera(string deviceId, string deviceName, int vendorId, int productId)
            : base(deviceId, deviceName, new UsbConnection(vendorId, productId))
        {
            VendorId = vendorId;
            ProductId = productId;
        }

        public static UsbCamera Create(int vendorId, int productId, string name = null)
        {
            string deviceId = $"USB_{vendorId:X4}_{productId:X4}";
            string deviceName = name ?? $"USB Camera (VID={vendorId:X4}, PID={productId:X4})";
            return new UsbCamera(deviceId, deviceName, vendorId, productId);
        }
    }
}

