//---------------------------------------------------------------------------------
// Copyright (c) July 2020 The nRF24L01P library was created by 
// Jakub Bartkowiak https://github.com/gralin, then ported to 
// Windows 10 IoT Core by Josh https://github.com/techfooninja, 
// then ported to nanoFramework by me https://github.com/kiwibryn.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Over time I will try to add more supported nanoFramework powered devices 
//
// #define NETDUINO3_WIFI   // nanoff --target NETDUINO3_WIFI --update
//---------------------------------------------------------------------------------
#define NETDUINO3_WIFI
namespace devMobile.IoT.FieldGateway.nanoFramework.nRF24Client
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading;

   using Radios.RF24;

   class Program
   {
      private const string BaseStationAddress = "Base1";
      private const string DeviceAddress = "Dev01";

      static void Main()
      {
         RF24 radio = new RF24();
         byte messageCount = System.Byte.MaxValue;

         try
         {
            radio.OnDataReceived += Radio_OnDataReceived;
            radio.OnTransmitFailed += Radio_OnTransmitFailed;
            radio.OnTransmitSuccess += Radio_OnTransmitSuccess;

#if NETDUINO3_WIFI
            radio.Initialize("SPI2", PinNumber('A', 2), PinNumber('A', 1), PinNumber('A', 3));
#endif
            radio.Address = Encoding.UTF8.GetBytes(DeviceAddress);

            radio.Channel = 15;
            //radio.PowerLevel = PowerLevel.Max;
            //radio.PowerLevel = PowerLevel.High;
            //radio.PowerLevel = PowerLevel.Low;
            radio.PowerLevel = PowerLevel.Minimum;
            radio.DataRate = DataRate.DR250Kbps;
            //radio.DataRate = DataRate.DR1Mbps;
            //radio.DataRate = DataRate.DR2Mbps;
            radio.IsEnabled = true;

            radio.IsAutoAcknowledge = true;
            radio.IsDyanmicAcknowledge = false;
            radio.IsDynamicPayload = true;

            Debug.WriteLine($"Address: {Encoding.UTF8.GetString(radio.Address, 0, radio.Address.Length)}");
            Debug.WriteLine($"PowerLevel: {radio.PowerLevel}");
            Debug.WriteLine($"IsAutoAcknowledge: {radio.IsAutoAcknowledge}");
            Debug.WriteLine($"Channel: {radio.Channel}");
            Debug.WriteLine($"DataRate: {radio.DataRate}");
            Debug.WriteLine($"IsDynamicAcknowledge: {radio.IsDyanmicAcknowledge}");
            Debug.WriteLine($"IsDynamicPayload: {radio.IsDynamicPayload}");
            Debug.WriteLine($"IsEnabled: {radio.IsEnabled}");
            Debug.WriteLine($"Frequency: {radio.Frequency}");
            Debug.WriteLine($"IsInitialized: {radio.IsInitialized}");
            Debug.WriteLine($"IsPowered: {radio.IsPowered}");

            while (true)
            {
               string payload = $"hello {messageCount}";
               messageCount -= 1;

               Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX {payload.Length} byte message {payload}");
               radio.SendTo(Encoding.UTF8.GetBytes(BaseStationAddress), Encoding.UTF8.GetBytes(payload));

               Thread.Sleep(30000);
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

      private static void Radio_OnDataReceived(byte[] data)
      {
         try
         {
            // display as hex
            Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-RX Hex Length {data.Length} Payload {BitConverter.ToString(data)}");

            // Remove unprintable characters from messages
            for (int index = 0; index < data.Length; index++)
            {
               if ((data[index] < 0x20) || (data[index] > 0x7E))
               {
                  data[index] = 0x20;
               }
            }

            string messageText = Encoding.UTF8.GetString(data, 0, data.Length);

            Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-RX {messageText.Length} byte messsage {messageText}");
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

      private static void Radio_OnTransmitSuccess()
      {
         Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX Succeeded!");
      }

      private static void Radio_OnTransmitFailed()
      {
         Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX failed!");
      }

#if NETDUINO3_WIFI
      static int PinNumber(char port, byte pin)
      {
         if (port < 'A' || port > 'J')
            throw new ArgumentException();

         return ((port - 'A') * 16) + pin;
      }
#endif
   }
}
