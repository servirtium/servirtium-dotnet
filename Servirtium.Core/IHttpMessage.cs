using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    public interface IHttpMessage
    {
        IEnumerable<(string Name, string Value)> Headers { get; }
        byte[]? Body { get; }
        MediaTypeHeaderValue? ContentType { get; }
    }

    public interface IRequestMessage : IHttpMessage
    {
        HttpMethod Method { get; }
    }

    public interface IResponseMessage : IHttpMessage
    {
        HttpStatusCode StatusCode { get; }
    }
}
