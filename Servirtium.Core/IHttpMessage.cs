using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    interface IHttpMessage
    {
        IEnumerable<(string Name, string Value)> Headers { get; }
        byte[]? Body { get; }
        MediaTypeHeaderValue? ContentType { get; }
    }

    interface IRequestMessage : IHttpMessage
    {
        HttpMethod Method { get; }
    }

    interface IResponseMessage : IHttpMessage
    {
        HttpStatusCode StatusCode { get; }
    }
}
