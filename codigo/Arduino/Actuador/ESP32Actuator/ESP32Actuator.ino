#include <WiFi.h>
#include "LED.h"

const char* ssid = "Flia.zubieta_s";
const char* password = "Zubieta1234";

const char* host = "192.168.1.13"; // IP de tu servidor
const int port = 5000;

LED blueLED(16);
LED greenLED(17);
LED yellowLED(18);
LED redLED(19);
WiFiClient client;
int blinksPerSecond = 3;
unsigned long lastPing = 0;
const unsigned long pingInterval = 2000; // 2 seconds

unsigned long lastPongTime = 0;
const unsigned long pongTimeout = 6000; // si no hay PONG en 6s, reconectar

void handleHeartbeat() {
  // Leer respuestas del servidor (PING:OK, etc.)
  while (client.available()) {
    String line = client.readStringUntil('\n');
    line.trim();
    if (line == "PING:OK") {
      lastPongTime = millis();
      Serial.println("PONG recibido");
    }
  }

  // Enviar PING periódico
  if (millis() - lastPing >= pingInterval) {
    if (client.connected()) {
      client.println("PING:SENSOR");
    }
    lastPing = millis();
  }

  // ✅ Detectar servidor muerto por timeout de PONG
  if (lastPongTime > 0 && millis() - lastPongTime > pongTimeout) {
    Serial.println("Servidor no responde, forzando reconexión...");
    client.stop();
    lastPongTime = 0;
  }
}


unsigned long lastWiFiAttempt = 0;
const unsigned long wifiRetryInterval = 5000; // 5 seconds

void ensureWiFi() {
  if (WiFi.status() == WL_CONNECTED) return;

  if (millis() - lastWiFiAttempt >= wifiRetryInterval) {
    Serial.println("WiFi desconectado. Intentando reconectar...");
    turnOffAllLEDS();

    WiFi.begin(ssid, password);

    lastWiFiAttempt = millis();
  }
}
unsigned long lastTcpAttempt = 0;
const unsigned long tcpRetryInterval = 3000; // 3 seconds

void ensureTCP() {
  if (client.connected()) return;

  if (millis() - lastTcpAttempt >= tcpRetryInterval) {
    Serial.println("TCP desconectado. Intentando reconectar...");
    turnOffAllLEDS();

    client.stop(); // limpia conexión previa

    if (client.connect(host, port)) {
      Serial.println("TCP reconectado");

      client.println("REGISTER:ACTUATOR"); // usa uno consistente
    } else {
      Serial.println("Error al conectar TCP");
    }

    lastTcpAttempt = millis();
  }
}
void ensureConnection() {
  ensureWiFi();

  if (WiFi.status() == WL_CONNECTED) {
    ensureTCP();
  }
}
void applyState(LED &led, int state) {
  if (state == 0) led.setState(LED::OFF);
  else if (state == 1) led.setState(LED::ON);
  else if (state == 2) led.setState(LED::BLINK); // tu implementación
}

void handleIncoming() {
  if (!client.connected()) return;

  while (client.available()) {
    String msg = client.readStringUntil('\n');
    msg.trim();

    if (msg.length() == 0) continue;

    Serial.println( msg);

    if (msg.startsWith("LED_CONFIG:")) {
      String data = msg.substring(11);

      int g, y, r, b;

      if (sscanf(data.c_str(), "%d,%d,%d,%d", &g, &y, &r, &b) == 4) {
        turnOffAllLEDS();

        applyState(greenLED, g);
        applyState(yellowLED, y);
        applyState(redLED, r);
        applyState(blueLED, b);
      } else {
        Serial.println(" Error parsing LED_CONFIG");
      }
    }
  }
}



void setup() {
  blueLED.setBlinksPerSecond(blinksPerSecond);
  greenLED.setBlinksPerSecond(blinksPerSecond);
  yellowLED.setBlinksPerSecond(blinksPerSecond);
  redLED.setBlinksPerSecond(blinksPerSecond);
  Serial.begin(115200);

  Serial.println();
  Serial.println("******************************************************");
  Serial.print("Conectando a ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }


  if (client.connect(host, port)) {
    client.println("REGISTER:ACTUATOR");
  } else {
    Serial.println("Error conectando al servidor en setup");
  }
  client.setTimeout(10);
}


void loop() {
  ensureConnection();
  handleHeartbeat();
  handleIncoming();
  updateAllLEDS();
  delay(20);
}
void turnOffAllLEDS()
{
  greenLED.setState(LED::OFF);
  yellowLED.setState(LED::OFF);
  redLED.setState(LED::OFF);
  blueLED.setState(LED::OFF);
}
void updateAllLEDS()
{
  greenLED.update();
  yellowLED.update();
  redLED.update();
  blueLED.update();
}