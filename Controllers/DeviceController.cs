using Microsoft.AspNetCore.Mvc;
using MySmartHomeAPI.Models;
using MySmartHomeAPI.Services;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                return BadRequest("Yeu cau khong hop le");
            }

            var result = _deviceService.ControlDevice(request);
            if (result)
            {
                return Ok(new { status = "Thay doi trang thai thanh cong" });

            }
            else
            {
                return StatusCode(500, new { status = "error", message = "Thiet bi dieu khien khong hop le" });
            }
        }

        [HttpGet("status/{id}")]
        public IActionResult GetDeviceStatus(int id)
        {
            var deviceStatus = _deviceService.GetDeviceStatus(id);
            if (deviceStatus == null)
            {
                return NotFound(new { status = "error", message = "Thiet bi khong tim thay" });
            }
            return Ok(deviceStatus);
        }

        [HttpGet("status")]
        public IActionResult GetAllDeviceStatus()
        {
            List<DeviceStatusResponse> devicesStatus = _deviceService.GetAllDeviceStatus();
            return Ok(devicesStatus);
        }

        [HttpGet("ws")]
        public async Task<IActionResult> GetWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _deviceService.SetWebSocket(webSocket);

                // Đọc và xử lý thông điệp từ WebSocket
                await HandleWebSocketAsync(webSocket);

                // Kết thúc yêu cầu HTTP sau khi thiết lập WebSocket
                return new EmptyResult();
            }
            else
            {
                HttpContext.Response.StatusCode = 400; // Bad Request
                return new EmptyResult();
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");

                // Xử lý thông điệp và gửi phản hồi
                var responseMessage = Encoding.UTF8.GetBytes($"Echo: {message}");
                await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
