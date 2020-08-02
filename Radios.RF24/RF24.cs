namespace Radios.RF24
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading;
   using Windows.Devices.Gpio;
   using Windows.Devices.Spi;

   /// <summary>
   ///   Driver class for Nordic nRF24L01+ tranceiver
   /// </summary>
   public class RF24
   {
      #region Delegates

      /// <summary>
      ///   Generic Event Handler for events
      /// </summary>
      public delegate void EventHandler();

      /// <summary>
      ///   Event Handler for when data is received
      /// </summary>
      /// <param name="data"></param>
      public delegate void OnDataReceivedHandler(byte[] data);

      #endregion Delegates

      #region Events

      /// <summary>
      ///   Occurs when data packet has been received
      /// </summary>
      public event OnDataReceivedHandler OnDataReceived = delegate { };

      /// <summary>
      ///   Occurs when ack has been received for send packet
      /// </summary>
      public event EventHandler OnTransmitSuccess = delegate { };

      /// <summary>
      ///   Occurs when no ack has been received for send packet
      /// </summary>
      public event EventHandler OnTransmitFailed = delegate { };

      #endregion Events

      #region Properties

      /// <summary>
      ///   Gets a value indicating whether module is enabled (RX or TX mode). Setting to true will enable the module,
      ///   false will disable it.
      /// </summary>
      public bool IsEnabled
      {
         get
         {
            return _cePin.Read() == GpioPinValue.High ? true : false;
         }
         set
         {
            _enabled = value;
            _cePin.Write(value ? GpioPinValue.High : GpioPinValue.Low);
         }
      }

      /// <summary>
      ///   Indicates whether or not the module is initialized
      /// </summary>
      public bool IsInitialized
      {
         get;
         private set;
      }

      /// <summary>
      ///   The channel that the radio will be operating on. If the channel is not supported, an
      ///   ArgumentOutOfRangeException is thrown.
      /// </summary>
      public byte Channel
      {
         get
         {
            return (byte)(Execute(Commands.R_REGISTER, Registers.RF_CH, new byte[1])[1] & 0x7F);
         }
         set
         {
            if (value > MAX_CHANNEL)
            {
               throw new ArgumentOutOfRangeException("Channel", "Channel cannot be greater than " + MAX_CHANNEL);
            }

            // Set radio channel
            Execute(Commands.W_REGISTER, Registers.RF_CH,
                    new[]
                    {
                            (byte) (value & 0x7F) // channel is 7 bits
                    });
         }
      }

      /// <summary>
      ///   Gets the module radio frequency [MHz]
      /// </summary>
      public int Frequency
      {
         get
         {
            return 2400 + Channel;
         }
      }

      /// <summary>
      ///   The DataRate for the module.
      /// </summary>
      public DataRate DataRate
      {
         get
         {
            byte regValue = Execute(Commands.R_REGISTER, Registers.RF_SETUP, new byte[1])[1];

            if ((regValue & (byte)(1 << Bits.RF_DR_LOW)) > 0)
            {
               return DataRate.DR250Kbps;
            }
            else if ((regValue & (byte)(1 << Bits.RF_DR_HIGH)) > 0)
            {
               return DataRate.DR2Mbps;
            }
            else
            {
               return DataRate.DR1Mbps;
            }
         }
         set
         {
            var regValue = Execute(Commands.R_REGISTER, Registers.RF_SETUP, new byte[1])[1];

            switch (value)
            {
               case DataRate.DR1Mbps:
                  regValue &= (byte)~(1 << Bits.RF_DR_LOW);  // 0
                  regValue &= (byte)~(1 << Bits.RF_DR_HIGH); // 0
                  break;

               case DataRate.DR2Mbps:
                  regValue &= (byte)~(1 << Bits.RF_DR_LOW);  // 0
                  regValue |= (byte)(1 << Bits.RF_DR_HIGH);  // 1
                  break;

               case DataRate.DR250Kbps:
                  regValue |= (byte)(1 << Bits.RF_DR_LOW);   // 1
                  regValue &= (byte)~(1 << Bits.RF_DR_HIGH); // 0
                  break;

               default:
                  throw new ArgumentOutOfRangeException("DataRate", "An invalid DataRate was specified");
            }

            Execute(Commands.W_REGISTER, Registers.RF_SETUP, new[] { regValue });
         }
      }

      /// <summary>
      ///   Enables or disables the Dynamic Payload feature for the radio
      /// </summary>
      public bool IsDynamicPayload
      {
         get
         {
            var regValue = Execute(Commands.R_REGISTER, Registers.FEATURE, new byte[1])[1];
            return ((regValue & (1 << Bits.EN_DPL)) > 0) && ((regValue & (1 << Bits.EN_ACK_PAY)) > 0);
         }
         set
         {
            uint enable = value ? 1u : 0u;

            // Enable dynamic payload length
            Execute(Commands.W_REGISTER, Registers.FEATURE,
                    new[]
                        {
                            (byte) (enable << Bits.EN_DPL |
                                    enable << Bits.EN_ACK_PAY)
                        });

            // Set dynamic payload length for pipes
            Execute(Commands.W_REGISTER, Registers.DYNPD,
                    new[]
                        {
                            (byte) (enable << Bits.DPL_P0 |
                                    enable << Bits.DPL_P1 |
                                    enable << Bits.DPL_P2 |
                                    enable << Bits.DPL_P3 |
                                    enable << Bits.DPL_P4 |
                                    enable << Bits.DPL_P5)
                        });
         }
      }

      /// <summary>
      ///   Enables or disables the Auto Ack feature for the radio
      /// </summary>
      public bool IsAutoAcknowledge
      {
         get
         {
            var regValue = Execute(Commands.R_REGISTER, Registers.EN_AA, new byte[1])[1];
            return (regValue & (
                         (byte)(1 << Bits.ENAA_P0 |
                                1 << Bits.ENAA_P1 |
                                1 << Bits.ENAA_P2 |
                                1 << Bits.ENAA_P3 |
                                1 << Bits.ENAA_P4 |
                                1 << Bits.ENAA_P5))) > 0;
         }
         set
         {
            uint enable = value ? 1u : 0u;

            // Set auto-ack
            Execute(Commands.W_REGISTER, Registers.EN_AA,
                    new[]
                        {
                            (byte) (enable << Bits.ENAA_P0 |
                                    enable << Bits.ENAA_P1 |
                                    enable << Bits.ENAA_P2 |
                                    enable << Bits.ENAA_P3 |
                                    enable << Bits.ENAA_P4 |
                                    enable << Bits.ENAA_P5)
                        });
         }
      }

      /// <summary>
      ///   Enables or disables the Dynamic Ack feature for the radio
      /// </summary>
      public bool IsDyanmicAcknowledge
      {
         get
         {
            var regValue = Execute(Commands.R_REGISTER, Registers.FEATURE, new byte[1])[1];
            return (regValue & (1 << Bits.EN_DYN_ACK)) > 0;
         }
         set
         {
            uint enable = value ? 1u : 0u;
            var regValue = Execute(Commands.R_REGISTER, Registers.FEATURE, new byte[1])[1];

            // Set dynamic-ack
            Execute(Commands.W_REGISTER, Registers.FEATURE,
                    new[]
                        {
                            (byte) (enable << Bits.EN_DYN_ACK)
                        });
         }
      }

      /// <summary>
      ///   The power level for the radio.
      /// </summary>
      public PowerLevel PowerLevel
      {
         get
         {
            byte regValue = Execute(Commands.R_REGISTER, Registers.RF_SETUP, new byte[1])[1];
            var newValue = (regValue & 0x06) >> 1;
            return (PowerLevel)newValue;
         }
         set
         {
            byte regValue = Execute(Commands.R_REGISTER, Registers.RF_SETUP, new byte[1])[1] &=(byte)0xF8;

            regValue |= (byte)((byte)value << 1);

            Execute(Commands.W_REGISTER, Registers.RF_SETUP,
                    new[]
                        {
                            (byte)regValue
                        });
         }
      }

      /// <summary>
      ///   Enables or disables power to the radio
      /// </summary>
      public bool IsPowered
      {
         get
         {
            var regValue = Execute(Commands.R_REGISTER, Registers.CONFIG, new byte[1])[1];
            return (regValue & (1 << Bits.PWR_UP)) > 0;
         }
         set
         {
            uint enable = value ? 1u : 0u;
            var regValue = Execute(Commands.R_REGISTER, Registers.CONFIG, new byte[1])[1];
            Execute(Commands.W_REGISTER, Registers.CONFIG,
                    new[]
                        {
                            (byte) (regValue | (enable << Bits.PWR_UP))
                        });
         }
      }

      /// <summary>
      ///   The address to be used for this radio. Must be between 3 and 5 bytes.
      ///   Otherwise an ArgumentException is raised. 
      /// </summary>
      public byte[] Address
      {
         get
         {
            var read = Execute(Commands.R_REGISTER, (byte)AddressSlot.Zero, new byte[_slot0Address.Length]);
            var result = new byte[read.Length - 1];
            Array.Copy(read, 1, result, 0, result.Length);
            return result;
         }
         set
         {
            AddressWidth.IsValid(value.Length);

            Execute(Commands.W_REGISTER, Registers.SETUP_AW,
                new[]
                    {
                            AddressWidth.Get(value)
                    });

            // Set module address
            _slot0Address = value;
            Execute(Commands.W_REGISTER, (byte)AddressSlot.Zero, value);
         }
      }

      #endregion Properties

      #region Public Members

      /// <summary>
      ///   Constructs a new RF24 object to represent this radio.
      /// </summary>     
      public RF24()
      {
      }

      /// <summary>
      ///   Initializes SPI connection and control pins
      /// </summary>
      /// <param name="chipEnablePin">
      ///   Number representing the chip enable pin. This pin will be set to drive output
      /// </param>
      /// <param name="chipSelectLine">
      ///   Number representing the chip select line. For RPi2, this is typically 0
      /// </param>
      /// <param name="interruptPin">
      ///   Number representing the interrupt pin. This should be a Pull-up pin, and will drive Input
      /// </param>
      public void Initialize(string spiPortName, int chipEnablePin, int chipSelectPin, int interruptPin, int clockFrequency = 2000000)
      {
         var gpio = GpioController.GetDefault();

         if (gpio == null)
         {
            Debug.WriteLine("GPIO Initialization failed.");
         }
         else
         {
            _cePin = gpio.OpenPin(chipEnablePin);
            _cePin.SetDriveMode(GpioPinDriveMode.Output);
            _cePin.Write(GpioPinValue.Low);

            _irqPin = gpio.OpenPin((byte)interruptPin);
            _irqPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            _irqPin.ValueChanged += irqPin_ValueChanged;
         }

         try
         {
            var settings = new SpiConnectionSettings(chipSelectPin)
            {
               ClockFrequency = clockFrequency,
               Mode = SpiMode.Mode0,
               SharingMode = SpiSharingMode.Shared,
            };

            _spiPort = SpiDevice.FromId(spiPortName, settings);
         }
         catch (Exception ex)
         {
            Debug.WriteLine("SPI Initialization failed. Exception: " + ex.Message);
            return;
         }

         // Module reset time
         Thread.Sleep(100);

         IsInitialized = true;

         // Set reasonable default values
         Address = Encoding.UTF8.GetBytes("NRF1");
         DataRate = DataRate.DR2Mbps;
         IsDynamicPayload = true;
         IsAutoAcknowledge = true;

         FlushReceiveBuffer();
         FlushTransferBuffer();
         ClearIrqMasks();
         SetRetries(5, 60);

         // Setup, CRC enabled, Power Up, PRX
         SetReceiveMode();
      }

      /// <summary>
      ///   Sets the delay time and count of retries if a transmission fails
      /// </summary>
      /// <param name="delay">
      ///   How long to wait between each retry, in multiples of 250us, max is 15. 0 means 250us, 15 means 4000us.
      /// </param>
      /// <param name="count">
      ///   How long to wait between each retry, in multiples of 250us, max is 15. 0 means 250us, 15 means 4000us.
      /// </param>
      public void SetRetries(byte delay, byte count)
      {
         Execute(Commands.W_REGISTER, Registers.SETUP_RETR,
                 new[]
                     {
                            (byte) ((delay & 0x0F) << Bits.ARD |
                                    (count & 0x0F) << Bits.ARC)
                     });
      }

      /// <summary>
      ///   Set one of 6 available module addresses
      /// </summary>
      /// <param name="slot">
      ///   The slot to write the address to
      /// </param>
      /// <param name="address">
      ///   The address to write. Must be between 3 and 5 bytes, otherwise an ArgumentException is thrown.
      /// </param>
      public void SetAddress(AddressSlot slot, byte[] address)
      {
         CheckIsInitialized();
         AddressWidth.IsValid(address);
         Execute(Commands.W_REGISTER, (byte)slot, address);

         if (slot == AddressSlot.Zero)
         {
            _slot0Address = address;
         }
      }

      /// <summary>
      ///   Read 1 of 6 available module addresses
      /// </summary>
      /// <param name="slot">
      ///   The slot to read from
      /// </param>
      /// <param name="width">
      ///   The width, in bytes, of the address
      /// </param>
      /// <returns>byte[] representing the address</returns>
      public byte[] GetAddress(AddressSlot slot, byte width)
      {
         CheckIsInitialized();
         AddressWidth.IsValid(width);
         var read = Execute(Commands.R_REGISTER, (byte)slot, new byte[width]);
         var result = new byte[read.Length - 1];
         Array.Copy(read, 1, result, 0, result.Length);
         return result;
      }

      /// <summary>
      ///   Executes a command in NRF24L01+ (for details see module datasheet)
      /// </summary>
      /// <param name = "command">Command</param>
      /// <param name = "addres">Register to write to or read from</param>
      /// <param name = "data">Data to write or buffer to read to</param>
      /// <returns>Response byte array. First byte is the status register</returns>
      public byte[] Execute(byte command, byte addres, byte[] data)
      {
         CheckIsInitialized();

         // This command requires module to be in power down or standby mode
         if (command == Commands.W_REGISTER)
            IsEnabled = false;

         // Create SPI Buffers with Size of Data + 1 (For Command)
         var writeBuffer = new byte[data.Length + 1];
         var readBuffer = new byte[data.Length + 1];

         // Add command and adres to SPI buffer
         writeBuffer[0] = (byte)(command | addres);

         // Add data to SPI buffer
         Array.Copy(data, 0, writeBuffer, 1, data.Length);

         // Do SPI Read/Write
         _spiPort.TransferFullDuplex(writeBuffer, readBuffer);

         // Enable module back if it was disabled
         if (command == Commands.W_REGISTER && _enabled)
            IsEnabled = true;

         // Return ReadBuffer
         return readBuffer;
      }

      /// <summary>
      ///   Gets module basic status information
      /// </summary>
      /// <returns>Status object representing the current status of the radio</returns>
      public Status GetStatus()
      {
         CheckIsInitialized();

         var readBuffer = new byte[1];
         _spiPort.TransferFullDuplex(new[] { Commands.NOP }, readBuffer);

         return new Status(readBuffer[0]);
      }

      /// <summary>
      ///   Send bytes to given address. This is a non blocking method.
      /// </summary>
      /// <param name="address">
      ///   Address to send bytes to
      /// </param>
      /// <param name="bytes">
      ///   Bytes to be sent
      /// </param>
      /// <param name="acknowledge">
      ///   Sets whether or not it is expected to receive an ack packet
      /// </param>
      public void SendTo(byte[] address, byte[] bytes, bool acknowledge = true)
      {
         // Chip enable low
         IsEnabled = false;

         // Setup PTX (Primary TX)
         SetTransmitMode();

         // Write transmit address to TX_ADDR register. 
         Execute(Commands.W_REGISTER, Registers.TX_ADDR, address);

         // Write transmit address to RX_ADDRESS_P0 (Pipe0) (For Auto ACK)
         Execute(Commands.W_REGISTER, Registers.RX_ADDR_P0, address);

         // Send payload
         Execute(acknowledge ? Commands.W_TX_PAYLOAD : Commands.W_TX_PAYLOAD_NO_ACK, 0x00, bytes);

         // Pulse for CE -> starts the transmission.
         IsEnabled = true;
      }


      #endregion Public Members

      #region Private Data

      private byte[] _slot0Address;
      private GpioPin _cePin;
      private GpioPin _irqPin;
      private SpiDevice _spiPort;
      private bool _enabled;
      private const byte MAX_CHANNEL = 127;

      #endregion Private Data

      #region Private Helpers

      private void SetTransmitMode()
      {
         Execute(Commands.W_REGISTER, Registers.CONFIG,
                 new[]
                     {
                            (byte) (1 << Bits.PWR_UP |
                                    1 << Bits.CRCO)
                     });
      }

      private void SetReceiveMode()
      {
         Execute(Commands.W_REGISTER, Registers.RX_ADDR_P0, _slot0Address);

         Execute(Commands.W_REGISTER, Registers.CONFIG,
                 new[]
                     {
                            (byte) (1 << Bits.PWR_UP |
                                    1 << Bits.CRCO |
                                    1 << Bits.PRIM_RX)
                     });
      }

      private void CheckIsInitialized()
      {
         if (!IsInitialized)
         {
            throw new InvalidOperationException("Initialize method needs to be called before this call");
         }
      }

      private void FlushReceiveBuffer()
      {
         // Flush RX FIFO
         Execute(Commands.FLUSH_RX, 0x00, new byte[0]);
      }

      private void FlushTransferBuffer()
      {
         // Flush TX FIFO 
         Execute(Commands.FLUSH_TX, 0x00, new byte[0]);
      }

      private void ClearIrqMasks()
      {
         // Clear IRQ Masks
         Execute(Commands.W_REGISTER, Registers.STATUS,
                 new[]
                     {
                            (byte) (1 << Bits.MASK_RX_DR |
                                    1 << Bits.MASK_TX_DS |
                                    1 << Bits.MAX_RT)
                     });
      }

      private void irqPin_ValueChanged(object sender, GpioPinValueChangedEventArgs args)
      {
         //Debug.WriteLine("Interrupt Triggered: " + args.Edge.ToString());

         if (args.Edge != GpioPinEdge.FallingEdge)
            return;

         if (!IsInitialized)
            return;

         if (!_enabled)
         {
            FlushReceiveBuffer();
            FlushTransferBuffer();
            return;
         }

         // Disable RX/TX
         IsEnabled = false;

         // Set PRX
         SetReceiveMode();

         // there are 3 rx pipes in rf module so 3 arrays should be enough to store incoming data
         // sometimes though more than 3 data packets are received somehow
         var payloads = new byte[6][];

         var status = GetStatus();
         byte payloadCount = 0;
         var payloadCorrupted = false;

         if (status.DataReady)
         {
            while (!status.RxEmpty)
            {
               // Read payload size
               var payloadLength = Execute(Commands.R_RX_PL_WID, 0x00, new byte[1]);

               // this indicates corrupted data
               if (payloadLength[1] > 32)
               {
                  payloadCorrupted = true;

                  // Flush anything that remains in buffer
                  FlushReceiveBuffer();
               }
               else
               {
                  if (payloadCount >= payloads.Length)
                  {
                     Debug.WriteLine("Unexpected payloadCount value = " + payloadCount);
                     FlushReceiveBuffer();
                  }
                  else
                  {
                     // Read payload data
                     payloads[payloadCount] = Execute(Commands.R_RX_PAYLOAD, 0x00, new byte[payloadLength[1]]);
                     payloadCount++;
                  }
               }

               // Clear RX_DR bit 
               var result = Execute(Commands.W_REGISTER, Registers.STATUS, new[] { (byte)(1 << Bits.RX_DR) });
               status.Update(result[0]);
            }
         }

         if (status.ResendLimitReached)
         {
            FlushTransferBuffer();

            // Clear MAX_RT bit in status register
            Execute(Commands.W_REGISTER, Registers.STATUS, new[] { (byte)(1 << Bits.MAX_RT) });
         }

         if (status.TxFull)
         {
            FlushTransferBuffer();
         }

         if (status.DataSent)
         {
            // Clear TX_DS bit in status register
            Execute(Commands.W_REGISTER, Registers.STATUS, new[] { (byte)(1 << Bits.TX_DS) });
            Debug.WriteLine("Data Sent!");
         }

         // Enable RX
         IsEnabled = true;

         if (payloadCorrupted)
         {
            Debug.WriteLine("Corrupted data received");
         }
         else if (payloadCount > 0)
         {
            if (payloadCount > payloads.Length)
               Debug.WriteLine("Unexpected payloadCount value = " + payloadCount);

            for (var i = 0; i < System.Math.Min(payloadCount, payloads.Length); i++)
            {
               var payload = payloads[i];
               var payloadWithoutCommand = new byte[payload.Length - 1];
               Array.Copy(payload, 1, payloadWithoutCommand, 0, payload.Length - 1);
               OnDataReceived(payloadWithoutCommand);
            }
         }
         else if (status.DataSent)
         {
            OnTransmitSuccess();
         }
         else
         {
            OnTransmitFailed();
         }
      }

      #endregion Private Helpers
   }
}
