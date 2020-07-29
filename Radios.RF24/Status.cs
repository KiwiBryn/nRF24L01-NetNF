namespace Radios.RF24
{
    /// <summary>
    ///   Status for the radio
    /// </summary>
    public class Status
    {
        private byte _reg;

        /// <summary>
        ///   True if data is ready to be read, false otherwise
        /// </summary>
        public bool DataReady           { get { return (_reg & (1 << Bits.RX_DR)) > 0; } }

        /// <summary>
        ///   True if data was sent successfully, false otherwise
        /// </summary>
        public bool DataSent            { get { return (_reg & (1 << Bits.TX_DS)) > 0; } }

        /// <summary>
        ///   True if the resend limit has been reached, false otherwise
        /// </summary>
        public bool ResendLimitReached  { get { return (_reg & (1 << Bits.MAX_RT)) > 0; } }

        /// <summary>
        ///   True if the transfer buffer is full, false otherwise
        /// </summary>
        public bool TxFull              { get { return (_reg & (1 << Bits.TX_FULL)) > 0; } }

        /// <summary>
        ///   Returns the data pipe used
        /// </summary>
        public byte DataPipe            { get { return (byte)((_reg >> 1) & 7); } }

        /// <summary>
        ///   True if a data pipe was not used, false otherwise
        /// </summary>
        public bool DataPipeNotUsed     { get { return DataPipe == 6; } }

        /// <summary>
        ///   True if the receive buffer is empty, false otherwise
        /// </summary>
        public bool RxEmpty             { get { return DataPipe == 7; } }

        /// <summary>
        ///   Create a new Status object
        /// </summary>
        /// <param name="reg">byte from the register with the status information</param>
        public Status(byte reg)
        {
            _reg = reg;
        }

        /// <summary>
        ///   Updates the status
        /// </summary>
        /// <param name="reg">byte from the register with the status information to update to</param>
        public void Update(byte reg)
        {
            _reg = reg;
        }

        /// <summary>
        ///   Converts data within the Status object to a string
        /// </summary>
        /// <returns>string representing the Status</returns>
        public override string ToString()
        {
            return "DataReady: " + DataReady +
                   ", DateSent: " + DataSent +
                   ", ResendLimitReached: " + ResendLimitReached +
                   ", TxFull: " + TxFull +
                   ", RxEmpty: " + RxEmpty +
                   ", DataPipe: " + DataPipe +
                   ", DataPipeNotUsed: " + DataPipeNotUsed;
        }
    }
}