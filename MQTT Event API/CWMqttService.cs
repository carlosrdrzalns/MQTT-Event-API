﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using MQTTnet;

using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Text;
using System.Net.Http;

namespace MQTT_Event_API
{
    public class CWMqttService : IHostedService
    {

        private readonly ILogger<MqttService> _logger;
        private static readonly HttpClient client = new HttpClient();

        public CWMqttService(ILogger<MqttService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Coloca aquí el código de suscripción MQTT que proporcioné anteriormente
            await SubscribeToAdafruitBrokerAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Agrega aquí cualquier lógica para detener el servicio MQTT si es necesario
            return Task.CompletedTask;
        }

        public static async Task SubscribeToAdafruitBrokerAsync()
        {
            // Create an MQTT client
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            // Create client options
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("io.adafruit.com", 1883) // Replace with Adafruit broker URL and port
                .WithCredentials(Environment.GetEnvironmentVariable("aio_username"), Environment.GetEnvironmentVariable("aio_key")) // Replace with your Adafruit username and password
                .Build();

            

            // Handler for received messages
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                string message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                try
                {
                    using StringContent jsonContent = new StringContent(message, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://localhost:44379/WaterPump/postData", jsonContent);

                    Console.WriteLine("Received application message.");
                }
                catch (System.Exception exp)
                {
                    var a = exp;
                }

            };

            // Connect and subscribe
            MqttClientConnectResult result = await mqttClient.ConnectAsync(options, CancellationToken.None);

            var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(Environment.GetEnvironmentVariable("aio_topic"));
                    })
                .Build();
            

            MqttClientSubscribeResult resultSub= await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

            Console.WriteLine("MQTT client subscribed to topic.");
        }

    }

}
