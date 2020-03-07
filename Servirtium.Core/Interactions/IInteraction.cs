using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public interface IInteraction
    {

        public struct Note
        {
            public enum NoteType
            { 
                Text,
                Code
            }

            public Note(NoteType type, string title, string content)
            {
                Type = type;
                Title = title;
                Content = content;
            }

            public NoteType Type { get; }
            public string Title { get; }
            public string Content { get; }
        }

        public int Number { get; }

        public IEnumerable<Note> Notes { get; }

        public HttpMethod Method { get; }

        public string Path { get; }

        public IEnumerable<(string Name, string Value)> RequestHeaders { get; }

        public (string Content, MediaTypeHeaderValue Type)? RequestBody { get; }

        public HttpStatusCode StatusCode { get; }

        public IEnumerable<(string Name, string Value)> ResponseHeaders { get; }

        public (string Content, MediaTypeHeaderValue Type)? ResponseBody { get; }
    }

    class NoopInteraction : IInteraction
    {
        public int Number => default;

        public IEnumerable<IInteraction.Note> Notes => new IInteraction.Note[0];

        public HttpMethod Method => HttpMethod.Get;

        public string Path => String.Empty;


        public IEnumerable<(string, string)> RequestHeaders => new (string,string)[0];

        public (string Content, MediaTypeHeaderValue Type)? RequestBody => default;

        public HttpStatusCode StatusCode => default;
        
        public IEnumerable<(string Name, string Value)> ResponseHeaders => new (string, string)[0];

        public (string Content, MediaTypeHeaderValue Type)? ResponseBody => default;
    }

    public static class Interaction
    {
        public static IInteraction Noop { get; }=new NoopInteraction();
    }
}
