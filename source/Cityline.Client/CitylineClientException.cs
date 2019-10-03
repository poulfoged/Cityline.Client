using System;

namespace Cityline.Client
{
    public class CitylineClientException : Exception
    {
        public CitylineClientException(string message) : base(message)
        {

        }
    }
}