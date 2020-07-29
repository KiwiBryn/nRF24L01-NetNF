namespace Radios.RF24
{
    using System;

    /// <summary>
    ///   Helper class for working with address widths
    /// </summary>
    public static class AddressWidth
    {
        private const int Min = 3;
        private const int Max = 5;

        /// <summary>
        ///   Gets the address width of an address
        /// </summary>
        /// <param name="address">Address to get the width of</param>
        /// <returns>Width of the address as a byte</returns>
        public static byte Get(byte[] address)
        {
            IsValid(address);
            return (byte) (address.Length - 2);
        }

        /// <summary>
        ///   Checks if an address has a valid width
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <returns>True if it is valid, false otherwise</returns>
        public static bool IsValid(byte[] address)
        {
            return IsValid(address.Length);
        }

        /// <summary>
        ///   Checks if an address width is valid
        /// </summary>
        /// <param name="addressWidth">The width of the address to check</param>
        /// <returns>True if it is valid, false otherwise</returns>
        public static bool IsValid(int addressWidth)
        {
            if (addressWidth < Min || addressWidth > Max)
            {
                return false;
            }
            return true;
        }
    }
}