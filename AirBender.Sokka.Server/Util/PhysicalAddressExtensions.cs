﻿using System.Linq;
using System.Net.NetworkInformation;
using AirBenderHost = AirBender.Sokka.Server.Host.AirBenderHost;

namespace AirBender.Sokka.Server.Util
{
    public static class PhysicalAddressExtensions
    {
        /// <summary>
        ///     Converts a <see cref="PhysicalAddress"/> to a human readyble hex string.
        /// </summary>
        /// <param name="address">The <see cref="PhysicalAddress"/> object to transform.</param>
        /// <returns>The hex string.</returns>
        public static string AsFriendlyName(this PhysicalAddress address)
        {
            if (address == null)
                return string.Empty;

            if (address.Equals(PhysicalAddress.None))
                return "00:00:00:00:00:00";

            var bytes = address.GetAddressBytes();

            return $"{bytes[0]:X2}:{bytes[1]:X2}:{bytes[2]:X2}:{bytes[3]:X2}:{bytes[4]:X2}:{bytes[5]:X2}";
        }

        /// <summary>
        ///     Reverses the byte order of a <see cref="PhysicalAddress"/> content.
        /// </summary>
        /// <param name="address">The <see cref="PhysicalAddress"/> object to transform.</param>
        /// <returns>The reversed <see cref="AirBenderHost.BdAddr"/>.</returns>
        internal static AirBenderHost.BdAddr ToNativeBdAddr(this PhysicalAddress address)
        {
            return new AirBenderHost.BdAddr()
            {
                Address = address.GetAddressBytes().Reverse().ToArray()
            };
        }
    }
}
