﻿using System;
using System.Runtime.InteropServices;

namespace SokkaServer
{
    partial class AirBender
    {
        private const uint IOCTL_AIRBENDER_GET_HOST_BD_ADDR = 0xFFDC6000;
        private const uint IOCTL_AIRBENDER_HOST_RESET = 0xFFDC2004;

        [StructLayout(LayoutKind.Sequential)]
        private struct BD_ADDR
        {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AIRBENDER_GET_HOST_BD_ADDR
        {
            public BD_ADDR Host;
        }
    }
}
