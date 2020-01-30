using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core
{
    public interface IInteraction
    {

        public int Number { get; }

        public HttpMethod Method { get; }

        public string Path { get; }

        public MediaTypeHeaderValue? RequestContentType { get; }

        public IEnumerable<(string, string)> RequestHeaders { get; }
        public bool HasRequestBody => RequestBody != null && RequestContentType != null;

        public string? RequestBody { get; }

        public HttpStatusCode StatusCode { get; }

        public MediaTypeHeaderValue? ResponseContentType { get; }

        public IEnumerable<(string, string)> ResponseHeaders { get; }

        public string? ResponseBody { get; }

        public bool HasResponseBody=> ResponseBody != null && ResponseContentType != null;

    }

    class NoopInteraction : IInteraction
    {
        public int Number => default;

        public HttpMethod Method => HttpMethod.Get;

        public string Path => String.Empty;

        public MediaTypeHeaderValue? RequestContentType => default;

        public IEnumerable<(string, string)> RequestHeaders => new (string,string)[0];

        public string? RequestBody => default;

        public HttpStatusCode StatusCode => default;

        public MediaTypeHeaderValue? ResponseContentType => default;

        public IEnumerable<(string, string)> ResponseHeaders => new (string, string)[0];

        public string? ResponseBody => default;
    }

    public static class Interaction
    {
        public static IInteraction Noop { get; }=new NoopInteraction();
    }
}
