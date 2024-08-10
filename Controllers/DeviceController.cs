using Microsoft.AspNetCore.Mvc;
using MySmartHomeAPI.Models;
using MySmartHomeAPI.Services;
using System.Collections.Generic;

namespace MySmartHomeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpPost("control")]
        public IActionResult ControlDevice([FromBody] DeviceControlRequest request)
        {
            if (request == null)
            {
                return BadRequest("Yêu cầu không hợp lệ");
            }

            var result = _deviceService.ControlDevice(request);
            if (result)
            {
                return Ok(new { status = "Thay đổi trạng thái thành công" });
            }
            else
            {
                return StatusCode(500, new { status = "error", message = "Thiết bị điều khiển không hợp lệ" });
            }
        }

     [HttpGet("status/{id}")]
public IActionResult GetDeviceStatus(int id)
{
    var deviceStatus = _deviceService.GetDeviceStatus(id);
    if (deviceStatus == null)
    {
        return NotFound(new { status = "error", message = "Thiết bị không tìm thấy" });
    }
    return Ok(deviceStatus);
}

[HttpGet("status")]
public IActionResult GetAllDeviceStatus()
{
    List<DeviceStatusResponse> devicesStatus = _deviceService.GetAllDeviceStatus();
    return Ok(devicesStatus);
}

    }
}