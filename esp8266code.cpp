#include <ESP8266WiFi.h>
#include <PubSubClient.h> // Thư viện MQTT
#include <ArduinoJson.h>
#include <WiFiClientSecure.h>
#include <DHT.h>  // Thư viện cảm biến DHT

#define DHTPIN D1  // Chân nối cảm biến DHT
#define DHTTYPE DHT22  // Loại cảm biến (DHT11 hoặc DHT22)

DHT dht(DHTPIN, DHTTYPE);  // Khởi tạo cảm biến DHT

// WiFi và MQTT thông tin kết nối
const char* ssid = "S99 Ultra";
const char* password = "123321@@@@";
const char* mqttServer = "b5b0a733da9d4bc5a9435dc3adf32503.s1.eu.hivemq.cloud";
const int mqttPort = 8883;
const char* mqttUser = "viethoang";
const char* mqttPassword = "24102003@hH";

// Khởi tạo chân động cơ và relay
const int motorIn1Pin = D6;
const int motorIn2Pin = D7;
const int motorSpeedPin = D0;
const int relay1Pin = D2;
const int relay2Pin = D3;
const int relay3Pin = D4;
const int buzzerRelayPin = D5;

WiFiClientSecure espClient;
PubSubClient mqttClient(espClient);

unsigned long lastMsg = 0;
#define MSG_BUFFER_SIZE (50)
char msg[MSG_BUFFER_SIZE];

bool isDoorOpen = false;  // Trạng thái cửa: true là mở, false là đóng

/****** root certificate *********/
static const char *root_ca PROGMEM = R"EOF(
-----BEGIN CERTIFICATE-----
MIIFazCCA1OgAwIBAgIRAIIQz7DSQONZRGPgu2OCiwAwDQYJKoZIhvcNAQELBQAw
...
-----END CERTIFICATE-----
)EOF";

void setup() {
  Serial.begin(9600);

  // Cấu hình các chân
  pinMode(relay1Pin, OUTPUT);
  pinMode(relay2Pin, OUTPUT);
  pinMode(relay3Pin, OUTPUT);
  pinMode(buzzerRelayPin, OUTPUT);
  pinMode(motorIn1Pin, OUTPUT);
  pinMode(motorIn2Pin, OUTPUT);
  pinMode(motorSpeedPin, OUTPUT);
  
  // Khởi động cảm biến DHT
  dht.begin();

  connectToWiFi();

  #ifdef ESP8266
    espClient.setInsecure();
  #else
    espClient.setCACert(root_ca);
  #endif

  mqttClient.setServer(mqttServer, mqttPort);
  mqttClient.setCallback(mqttCallback);
  
  connectToMqtt();
}

void loop() {
  if (!mqttClient.connected()) {
    reconnectMqtt();
  }
  mqttClient.loop();

  // Gửi dữ liệu nhiệt độ và độ ẩm lên MQTT mỗi 10 giây
  unsigned long now = millis();
  if (now - lastMsg > 10000) {
    lastMsg = now;
    sendSensorData();
  }
}

// Hàm kết nối WiFi
void connectToWiFi() {
  Serial.print("\nConnecting to ");
  Serial.println(ssid);

  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi connected\nIP address: ");
  Serial.println(WiFi.localIP());
}

// Hàm kết nối MQTT
void connectToMqtt() {
  while (!mqttClient.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP8266Client-";   
    clientId += String(random(0xffff), HEX);
    if (mqttClient.connect(clientId.c_str(), mqttUser, mqttPassword)) {
      Serial.println("connected");
      mqttClient.subscribe("home/device/1/control");
      mqttClient.subscribe("home/device/2/control");
      mqttClient.subscribe("home/device/3/control");
      mqttClient.subscribe("home/device/4/control");
    } else {
      Serial.print("failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" try again in 5 seconds");
      delay(5000);
    }
  }
}

// Hàm xử lý khi nhận được tin nhắn từ MQTT
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Nhận tin nhắn trên topic: ");
  Serial.println(topic);

  String payloadString;
  for (int i = 0; i < length; i++) {
    payloadString += (char)payload[i];
  }
  
  Serial.println("Nội dung tin nhắn:");
  Serial.println(payloadString);

  DynamicJsonDocument doc(2048);
  deserializeJson(doc, payloadString);

  int id = doc["Id"];
  String status = doc["Status"];

  switch(id) {
    case 1: 
      controlRelay(relay1Pin, status);
      break;
    case 2: 
      controlRelay(relay2Pin, status);
      break;
    case 3: 
      controlRelay(relay3Pin, status);
      break;
    case 4: 
      controlMotor(status);
      break;
    default:
      break;
  }
}

// Điều khiển relay
void controlRelay(int relayPin, const String& status) {
  if (status == "on") {
    digitalWrite(relayPin, HIGH); 
    Serial.print("Relay ");
    Serial.print(relayPin);
    Serial.println(" bật");
  } else {
    digitalWrite(relayPin, LOW); 
    Serial.print("Relay ");
    Serial.print(relayPin);
    Serial.println(" tắt");
  }
}

// Điều khiển động cơ cửa
void controlMotor(const String& status) {
  if (status == "open" && !isDoorOpen) {
    digitalWrite(motorIn1Pin, HIGH);
    digitalWrite(motorIn2Pin, LOW);
    analogWrite(motorSpeedPin, 150); 
    Serial.println("Đang mở cửa");
    soundBuzzer();
    delay(600); 
    stopMotor(); 
    isDoorOpen = true;  
  }
  else if (status == "close" && isDoorOpen) {
    digitalWrite(motorIn1Pin, LOW);
    digitalWrite(motorIn2Pin, HIGH);
    analogWrite(motorSpeedPin, 150); 
    Serial.println("Đang đóng cửa");
    soundBuzzer();
    delay(360); 
    stopMotor(); 
    isDoorOpen = false;
  } else {
    Serial.println("Hành động không hợp lệ, cửa đã ở trạng thái mong muốn.");
  }
}

void stopMotor() {
  digitalWrite(motorIn1Pin, LOW);
  digitalWrite(motorIn2Pin, LOW);
  analogWrite(motorSpeedPin, 0); 
  Serial.println("Dừng động cơ");
}

// Kêu còi
void soundBuzzer() {
  for(int i = 0; i < 2; i++){
    digitalWrite(buzzerRelayPin, HIGH);
    Serial.println("Còi kêu");
    delay(200);
    digitalWrite(buzzerRelayPin, LOW);
    delay(100);
  }
}

// Hàm gửi dữ liệu nhiệt độ và độ ẩm lên MQTT
void sendSensorData() {
  float temperature = dht.readTemperature();
  float humidity = dht.readHumidity();

  if (isnan(temperature) || isnan(humidity)) {
    Serial.println("Lỗi đọc dữ liệu từ cảm biến DHT!");
    return;
  }

  DynamicJsonDocument doc(1024);
  doc["temperature"] = temperature;
  doc["humidity"] = humidity;

  char buffer[256];
  serializeJson(doc, buffer);

  mqttClient.publish("home/sensors/temperature_humidity", buffer);
  Serial.println("Dữ liệu cảm biến đã gửi lên MQTT:");
  Serial.println(buffer);
}
