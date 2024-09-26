using System.Collections.Generic;
using MQTTnet;
using MQTTnet.Client;

using MQTTnet.Protocol;
using MySmartHomeAPI.Models;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MySmartHomeAPI.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly Dictionary<int, DeviceStatusResponse> _devices;
        private static WebSocket _webSocket;
        private IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;

        public DeviceService()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("pm-1727059483580")
                .WithTcpServer("b5b0a733da9d4bc5a9435dc3adf32503.s1.eu.hivemq.cloud", 8883)
                .WithCredentials("viethoang", "24102003@hH")
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    AllowUntrustedCertificates = false,
                    IgnoreCertificateChainErrors = false,
                    IgnoreCertificateRevocationErrors = false
                })
                .WithCleanSession()
                .Build();

            _devices = new Dictionary<int, DeviceStatusResponse>
            {
                { 1, new DeviceStatusResponse { Id = 1, Name = "Den phong khach", Status = "on", Message = "Thiet bi dang tat" } },
                { 2, new DeviceStatusResponse { Id = 2, Name = "Quat phong ngu", Status = "on", Message = "Thiet bi dang tat" } },
                { 3, new DeviceStatusResponse { Id = 3, Name = "Binh nong lanh", Status = "on", Message = "Thiet bi dang tat" } },
                { 4, new DeviceStatusResponse { Id = 4, Name = "Cua cuon", Status = "off", Message = "Thiet bi dang tat" } }
            };

            // Kết nối tới MQTT Server
            ConnectToMqttServer();
        }
        private async Task ConnectToMqttServer()
        {
            try
            {
                // Kết nối tới MQTT Server
                await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                Console.WriteLine("Đã kết nối tới MQTT Server.");

                // Đăng ký các topic
                await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f =>
                    {
                        f.WithTopic("home/device/+/status");
                        f.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
                    })
                    .Build(), CancellationToken.None);

                Console.WriteLine("Đã đăng ký topic.");

                // Gửi trạng thái của tất cả các thiết bị
                foreach (var device in _devices.Values)
                {
                    await PublishMqttMessage(device);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi kết nối MQTT: {ex.Message}");
            }

            _mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine("Mất kết nối MQTT, đang thử kết nối lại...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ConnectToMqttServer();
            };
        }



        private async Task PublishMqttMessage(DeviceStatusResponse device)
        {
            if (_mqttClient.IsConnected)
            {
                var topic = $"home/device/{device.Id}/control";
                var messagePayload = JsonSerializer.Serialize(device);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(messagePayload)) // Chuyển đổi chuỗi JSON thành byte
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce) // Đảm bảo tin nhắn được gửi ít nhất một lần
                    .WithRetainFlag(false) // Không giữ lại tin nhắn
                    .Build();

                await _mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine($"Đã gửi lệnh tới MQTT: {messagePayload}");
            }
        }
        public bool ControlDevice(DeviceControlRequest request)
        {
            if (_devices.ContainsKey(request.Id))
            {
                var device = _devices[request.Id];

                // Cập nhật trạng thái
                device.Status = request.Status;

                // Cập nhật thông báo dựa trên loại thiết bị và trạng thái mới
                if (device.Name.Contains("Cua"))
                {
                    if (request.Status == "open")
                    {
                        device.Message = $"{device.Name} dang mo";
                    }
                    else if (request.Status == "closed")
                    {
                        device.Message = $"{device.Name} dang dong";
                    }
                }
                else
                {
                    if (request.Status == "on")
                    {
                        device.Message = $"Thiet bi {device.Name} da bat";
                    }
                    else if (request.Status == "off")
                    {
                        device.Message = $"Thiet bi {device.Name} da tat";
                    }
                }

                // Cập nhật lại trong Dictionary
                _devices[request.Id] = device;

                // Gửi thông báo qua WebSocket
                Task.Run(() => SendWebSocketMessage(new DeviceStatusResponse
                {
                    Id = device.Id,
                    Name = device.Name,
                    Status = device.Status,
                    Message = device.Message
                }));

                // Gửi tin nhắn MQTT và đợi cho đến khi tin nhắn được gửi thành công
                Task.Run(async () => await PublishMqttMessage(new DeviceStatusResponse
                {
                    Id = device.Id,
                    Name = device.Name,
                    Status = device.Status,
                    Message = device.Message
                }));

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

        public List<DeviceStatusResponse> GetAllDeviceStatus()
        {
            return new List<DeviceStatusResponse>(_devices.Values);
        }

        private async Task SendWebSocketMessage(DeviceStatusResponse deviceStatus)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(deviceStatus));
                await _webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void SetWebSocket(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }
    }
}
