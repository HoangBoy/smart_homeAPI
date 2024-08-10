using MySmartHomeAPI.Models;

namespace MySmartHomeAPI.Services
{
    public interface IDeviceService
    {
        bool ControlDevice(DeviceControlRequest request);
        DeviceStatusResponse GetDeviceStatus(int id);
          List<DeviceStatusResponse> GetAllDeviceStatus(); // Phương thức mới

          
    }
}
