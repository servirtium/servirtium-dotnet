using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    public interface IBodyFormatter
    {
        byte[] Read(string recordedBody, MediaTypeHeaderValue contentType);

        string Write(byte[] bodyContent, MediaTypeHeaderValue contentType);
    }
}
