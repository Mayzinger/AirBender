﻿using System;
using System.Runtime.Serialization;

namespace AirBender.Sokka.Server.Exceptions
{
    public class AirBenderHostResetFailedException : Exception
    {
        public AirBenderHostResetFailedException()
        {
        }

        public AirBenderHostResetFailedException(string message) : base(message)
        {
        }

        public AirBenderHostResetFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderHostResetFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
