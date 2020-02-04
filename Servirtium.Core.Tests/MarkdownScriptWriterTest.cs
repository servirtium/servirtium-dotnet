using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests
{
    public class SampleFileLoader 
    {
        public readonly IDictionary<string, string> scripts = new Dictionary<string, string>();
        public SampleFileLoader() 
        { 
            foreach(var sample in new[] { 
                "single_get.md", "single_post.md","single_put.md", "single_delete.md", "single_error.md", "single_error_nobody.md","conversation.md","outofsequence_conversation_validate.md" })
            {
                scripts[sample] =  File.ReadAllText($"markdown_conversations{Path.DirectorySeparatorChar}{sample}");
            }
        }
    }

    [Collection("Tests using markdown_conversations.")]
    public class MarkdownScriptWriterTest: IClassFixture<SampleFileLoader>
    {
        private readonly SampleFileLoader _loader;
        public MarkdownScriptWriterTest(SampleFileLoader loader)
        {
            _loader = loader;
        }

        private static readonly (string, string)[] _baselineMockRequestHeaders = new[]{
                    ("Accept", "text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2"),
                    ("User-Agent", "Servirtium-Testing"),
                    ("Connection", "keep-alive"),
                    ("Host", "mock.service-host.org")
                };

        private static readonly (string, string)[] _baselineMockResponseHeaders = new[]{
                    ("Content-Type", "text/plain"),
                    ("Connection", "keep-alive"),
                    ("Transfer-Encoding", "chunked")
                };

        private void CompareSampleMarkdown(string scriptName, IDictionary<int, IInteraction> interactions)
        {
            var sampleText = _loader.scripts[scriptName];
            var interactionText = new StringBuilder();
            new MarkdownScriptWriter().Write(new StringWriter(interactionText), interactions);
            Assert.Equal(sampleText.Replace("\n", Environment.NewLine), interactionText.ToString());
        }

        [Fact]
        public void Write_SingleGetRequest_WritesSingleInteraction()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            CompareSampleMarkdown("single_get.md", interactions);
        }

        [Fact]
        public void Write_SinglePostRequest_WritesSingleInteraction()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Post)
                .RequestHeaders(_baselineMockRequestHeaders)
                .RequestBody("{\"some\":\"json\"}", MediaTypeHeaderValue.Parse("application/json"))
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE POST RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            CompareSampleMarkdown("single_post.md", interactions);
        }

        [Fact]
        public void Write_SinglePutRequestReturnsResponseWithNoBody_WritesSingleInteractionWithNoResponsebody()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Put)
                .RequestHeaders(_baselineMockRequestHeaders)
                .RequestBody("{\"some\":\"json\"}", MediaTypeHeaderValue.Parse("application/json"))
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .Build();
            CompareSampleMarkdown("single_put.md", interactions);
        }

        [Fact]
        public void Write_SingleDeleteNoRequestOrResponseBody_WritesSingleInteractionWithNoRequestOrResponseBody()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Delete)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .Build();
            CompareSampleMarkdown("single_delete.md", interactions);
        }

        [Fact]
        public void Write_Error_WritesSingleInteraction()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.NotFound)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("NOTHIN' DOIN'", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();
            CompareSampleMarkdown("single_error.md", interactions);
        }

        [Fact]
        public void Write_ErrorWithNoBody_WritesErrorInteraction()
        {

            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service")
                .Method(HttpMethod.Delete)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.Forbidden)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .Build();
            CompareSampleMarkdown("single_error_nobody.md", interactions);
        }

        [Fact]
        public void Write_Conversation_WritesAllInteractionsInADictionary()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service_1")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            interactions[1] = new ImmutableInteraction.Builder()
                .Number(1)
                .Path("/a/mock/service_2")
                .Method(HttpMethod.Post)
                .RequestHeaders(_baselineMockRequestHeaders)
                .RequestBody("{\"some\":\"json\"}", MediaTypeHeaderValue.Parse("application/json"))
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE POST RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            interactions[2] = new ImmutableInteraction.Builder()
                .Number(2)
                .Path("/a/mock/service_3")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            CompareSampleMarkdown("conversation.md", interactions);
        }

        [Fact]
        public void Write_ConversationWithMissingInteractionNumbers_Throws()
        {

            var interactions = new Dictionary<int, IInteraction>();
            interactions[5] = new ImmutableInteraction.Builder()
                .Number(5)
                .Path("/a/mock/service_1")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            interactions[2] = new ImmutableInteraction.Builder()
                .Number(2)
                .Path("/a/mock/service_2")
                .Method(HttpMethod.Post)
                .RequestHeaders(_baselineMockRequestHeaders)
                .RequestBody("{\"some\":\"json\"}", MediaTypeHeaderValue.Parse("application/json"))
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE POST RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            var interactionText = new StringBuilder();
            Assert.Throws<ArgumentException>(()=>new MarkdownScriptWriter().Write(new StringWriter(interactionText), interactions));
        }



        [Fact]
        public void Write_ConversationWhereInteractionNumbersDontMatchDictionaryKeys_Throws()
        {
            var interactions = new Dictionary<int, IInteraction>();
            interactions[0] = new ImmutableInteraction.Builder()
                .Number(2)
                .Path("/a/mock/service_1")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            interactions[1] = new ImmutableInteraction.Builder()
                .Number(0)
                .Path("/a/mock/service_2")
                .Method(HttpMethod.Post)
                .RequestHeaders(_baselineMockRequestHeaders)
                .RequestBody("{\"some\":\"json\"}", MediaTypeHeaderValue.Parse("application/json"))
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE POST RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            interactions[2] = new ImmutableInteraction.Builder()
                .Number(1)
                .Path("/a/mock/service_3")
                .Method(HttpMethod.Get)
                .RequestHeaders(_baselineMockRequestHeaders)
                .StatusCode(HttpStatusCode.OK)
                .ResponseHeaders(_baselineMockResponseHeaders)
                .ResponseBody("SIMPLE GET RESPONSE", MediaTypeHeaderValue.Parse("text/plain"))
                .Build();

            var interactionText = new StringBuilder();
            Assert.Throws<ArgumentException>(() => new MarkdownScriptWriter().Write(new StringWriter(interactionText), interactions));
        }
    }
}
