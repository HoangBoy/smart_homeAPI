using System.Collections.Generic;
using MySmartHomeAPI.Models;

namespace MySmartHomeAPI.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly Dictionary<int, DeviceStatusResponse> _devices;

        public DeviceService()
        {
            _devices = new Dictionary<int, DeviceStatusResponse>
            {
                   { 1, new DeviceStatusResponse { Id = 1, Name = "Đèn phòng khách", Status = "on", Message = "Thiết bị đang tắt" } },
                { 2, new DeviceStatusResponse { Id = 2, Name = "Quạt phòng ngủ", Status = "on", Message = "Thiết bị đang tắt" } },
                { 3, new DeviceStatusResponse { Id = 3, Name = "Bình nóng lạnh", Status = "on", Message = "Thiết bị đang tắt" } },
                { 4, new DeviceStatusResponse { Id = 4, Name = "Cửa cuốn", Status = "off", Message = "Thiết bị đang tắt" } }
            };
        }

        public bool ControlDevice(DeviceControlRequest request)
{
    if (_devices.ContainsKey(request.Id))
    {
        var device = _devices[request.Id];
        
        // Cập nhật trạng thái
        device.Status = request.Status;
        
        // Cập nhật thông báo dựa trên loại thiết bị và trạng thái mới
        if (device.Name.Contains("Cửa")) // Giả sử tên thiết bị chứa từ "Cửa" là cửa
        {
            // Cập nhật thông báo cho cửa
            if (request.Status == "open")
            {
                device.Message = $"{device.Name} đang mở";
            }
            else if (request.Status == "closed")
            {
                device.Message = $"{device.Name} đang đóng";
            }
                device.Message = $"Cửa {device.Name} trạng thái đặt thành {request.Status}";
            
        }
        else
        {
            // Cập nhật thông báo cho thiết bị khác
            if (request.Status == "on")
            {
                device.Message = $"Thiết bị {device.Name} đã bật";
            }
            else if (request.Status == "off")
            {
                device.Message = $"Thiết bị {device.Name} đã tắt";
            }
          
                device.Message = $"Thiết bị {device.Name} trạng thái đặt thành {request.Status}";
            
        }
        
        // Cập nhật lại trong Dictionary
        _devices[request.Id] = device;
        
        // In ra để kiểm tra
        Console.WriteLine($"Updated Device: {device.Id}, Status: {device.Status}, Message: {device.Message}");
        
        return true;
    }
    return false;
}


        public DeviceStatusResponse GetDeviceStatus(int id)
        {
            return _devices.ContainsKey(id) ? _devices[id] : null;
        }

        public List<DeviceStatusResponse> GetAllDeviceStatus() // Triển khai mới
        {
            return new List<DeviceStatusResponse>(_devices.Values);
        }
    }

    
}
