using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{

    public interface IHttpMessage
    {
        public static IEnumerable<(string Name, string Value)> FixContentLengthHeader(IEnumerable<(string Name, string Value)> currentHeaders, byte[] body, bool createContentLengthHeader)
        {
            IEnumerable<(string, string)> headersWithAdjustedContentLength = currentHeaders.Select(h =>
            {
                if (h.Name == "Content-Length" || h.Name == "content-length")
                {
                    createContentLengthHeader = false;
                    return (h.Name, body.Length.ToString());
                }
                else return h;
            }).ToArray();
            if (createContentLengthHeader)
            {
                headersWithAdjustedContentLength = headersWithAdjustedContentLength.Append(("Content-Length", body.Length.ToString()));
            }
            return headersWithAdjustedContentLength;
        }

        IEnumerable<(string Name, string Value)> Headers { get; }

        byte[]? Body { get; }

        MediaTypeHeaderValue? ContentType { get; }

        bool HasBody => Body != null && ContentType != null;

    }

    public interface IRequestMessage : IHttpMessage
    {
        Uri Url { get; }
        HttpMethod Method { get; }
    }

    public interface IResponseMessage : IHttpMessage
    {
        HttpStatusCode StatusCode { get; }
    }
}
