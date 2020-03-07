using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Interactions
{
    class UTF8TextBodyFormatter : IBodyFormatter
    {
        public byte[] Read(string recordedBody, MediaTypeHeaderValue contentType) => Encoding.UTF8.GetBytes(recordedBody);


        public string Write(byte[] bodyContent, MediaTypeHeaderValue contentType) => Encoding.UTF8.GetString(bodyContent);
    }
}
