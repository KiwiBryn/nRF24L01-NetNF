namespace Radios.RF24
{
    /// <summary>
    ///   The address slots on the radio
    /// </summary>
    public enum AddressSlot
    {
        /// <summary>
        ///   Represents slot 0 on the radio
        /// </summary>
        Zero = Registers.RX_ADDR_P0,

        /// <summary>
        ///   Represents slot 1 on the radio
        /// </summary>
        One = Registers.RX_ADDR_P1,

        /// <summary>
        ///   Represents slot 2 on the radio
        /// </summary>
        Two = Registers.RX_ADDR_P2,

        /// <summary>
        ///   Represents slot 3 on the radio
        /// </summary>
        Three = Registers.RX_ADDR_P3,

        /// <summary>
        ///   Represents slot 4 on the radio
        /// </summary>
        Four = Registers.RX_ADDR_P4,

        /// <summary>
        ///   Represents slot 5 on the radio
        /// </summary>
        Five = Registers.RX_ADDR_P5,
    }
}