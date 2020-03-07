using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Interactions
{
    class Base64BinaryBodyFormatter : IBodyFormatter
    {
        public byte[] Read(string recordedBody, MediaTypeHeaderValue contentType) => Convert.FromBase64String(recordedBody);

        public string Write(byte[] bodyContent, MediaTypeHeaderValue contentType) => Convert.ToBase64String(bodyContent);
    }
}
