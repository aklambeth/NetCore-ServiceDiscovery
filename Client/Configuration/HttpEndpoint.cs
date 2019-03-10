using System;
using System.Net;

namespace TestClient.Configuration
{
    public class HttpEndpoint
    {
        public string Address { get; set; }

        public int Port { get; set; }

        public Uri ToUri()
        {
            return new UriBuilder("http", Address, Port).Uri;
        }
    }
}
