#include <WiFi.h>
#include "SonarSensor.h"

const char* ssid = "Flia.zubieta_s";
const char* password = "Zubieta1234";

const char* host = "192.168.1.13"; // IP de tu servidor
const int port = 5000;
SonarSensor sonar(26, 27);

WiFiClient client;
unsigned long lastPing = 0;
const unsigned long pingInterval = 2000; // 2 seconds
float lastDist = 0.0;unsigned long lastPongTime = 0;
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

    WiFi.begin(ssid, password); // ❗ NO disconnect()

    lastWiFiAttempt = millis();
  }
}
unsigned long lastTcpAttempt = 0;
const unsigned long tcpRetryInterval = 3000; // 3 seconds

void ensureTCP() {
  if (client.connected()) return;

  if (millis() - lastTcpAttempt >= tcpRetryInterval) {
    Serial.println("TCP desconectado. Intentando reconectar...");

    client.stop(); // limpia conexión previa

    if (client.connect(host, port)) {
      Serial.println("TCP reconectado");

      client.println("REGISTER:SENSOR"); // usa uno consistente
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



void setup() {
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
    client.println("REGISTER:SENSOR");
  } else {
    Serial.println("Error conectando al servidor en setup");
  }
  client.setTimeout(10);
}

unsigned long lastSend = 0;
const unsigned long sendInterval = 300;
void loop() {
  ensureConnection();
  handleHeartbeat();

  float dist = sonar.getDistanceCm();

Serial.print("distancia Medida:");
Serial.println(dist);
Serial.print("WiFi: ");
Serial.println(WiFi.status() == WL_CONNECTED ? "OK" : "NO");

Serial.print("TCP: ");
Serial.println(client.connected() ? "OK" : "NO");

if (millis() - lastSend > sendInterval || abs(lastDist-dist)>=1) {
  if (client.connected()) {
    Serial.print("distancia EnviadaServ:");
    Serial.println(dist);

    client.print("DISTANCE:");
    client.println(dist, 2);

    lastDist = dist;
    lastSend = millis();
  }
}
delay(200);
}