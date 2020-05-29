using System;

namespace WirelessDisplayClient.Services
{
    public class WDCServiceException : Exception
    {
        public WDCServiceException(string msg) :base (msg)
        {
        }
    }
}