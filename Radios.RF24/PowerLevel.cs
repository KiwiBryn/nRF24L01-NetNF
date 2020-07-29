namespace Radios.RF24
{
    /// <summary>
    ///   Power levels the radio can operate with
    /// </summary>
    public enum PowerLevel : byte
    {
        /// <summary>
        ///   Minimum power setting for the radio
        /// </summary>
        Minimum = 0,

        /// <summary>
        ///   Low power setting for the radio
        /// </summary>
        Low,

        /// <summary>
        ///   High power setting for the radio
        /// </summary>
        High,

        /// <summary>
        ///   Max power setting for the radio
        /// </summary>
        Max,

        /// <summary>
        ///   Error with the power setting
        /// </summary>
        Error
    }
}
