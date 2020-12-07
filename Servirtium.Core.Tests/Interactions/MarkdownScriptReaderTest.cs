using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;

namespace Servirtium.Core.Tests.Interactions
{
    [Collection("Tests using markdown_conversations.")]
    public class MarkdownScriptReaderTest
    {
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

        private static IDictionary<int, IInteraction> ReadSampleMarkdown(string scriptName)
        {
            using (var reader = File.OpenText($"markdown_conversations{Path.DirectorySeparatorChar}{scriptName}"))
            {
                return new MarkdownScriptReader().Read(reader);
            }

        }


        [Theory]
        [InlineData("single_get.md")]
        [InlineData("single_get_italichttpmethod.md")]
        public void Read_SingleGetRequest_ReturnsSingleInteraction(string sampleFile)
        {
            var interactions = ReadSampleMarkdown(sampleFile);
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];

            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Empty(interaction.Notes);

            Assert.Equal(HttpMethod.Get, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            
            Assert.False(interaction.RequestBody.HasValue);

            Assert.Equal(HttpStatusCode.OK, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);

            var (content, type) = interaction.ResponseBody!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), type);
            Assert.Equal("SIMPLE GET RESPONSE", content);
        }


        [Theory]
        [InlineData("single_post.md")]
        [InlineData("single_post_indentedcodeblocks.md")]
        public void Read_SinglePostRequest_ReturnsSingleInteraction(string sampleFile)
        {
            var interactions = ReadSampleMarkdown(sampleFile);
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Equal(HttpMethod.Post, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            var (requestContent, requestType) = interaction.RequestBody!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("application/json"), requestType);
            Assert.Equal("{\"some\":\"json\"}", requestContent);

            Assert.Equal(HttpStatusCode.OK, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);
            var (content, type) = interaction.ResponseBody!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), type);
            Assert.Equal("SIMPLE POST RESPONSE", content);
        }

        [Fact]
        public void Read_SinglePutRequestReturnsResponseWithNoBody_ReturnsSingleInteractionWithNoResponsebody()
        {
            var interactions = ReadSampleMarkdown("single_put.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Equal(HttpMethod.Put, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            var (requestContent, requestType) = interaction.RequestBody!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("application/json"), requestType);
            Assert.Equal("{\"some\":\"json\"}", requestContent);

            Assert.Equal(HttpStatusCode.OK, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);
            Assert.False(interaction.ResponseBody.HasValue);
        }

        [Fact]
        public void Read_SingleDeleteNoRequestOrResponseBody_ReturnsSingleInteractionWithNoRequestOrResponseBody()
        {
            var interactions = ReadSampleMarkdown("single_delete.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Equal(HttpMethod.Delete, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            Assert.False(interaction.RequestBody.HasValue);

            Assert.Equal(HttpStatusCode.OK, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);
            Assert.False(interaction.ResponseBody.HasValue);
        }

        [Fact]
        public void Read_Error_ReturnsSingleInteraction()
        {
            var interactions = ReadSampleMarkdown("single_error.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];

            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Equal(HttpMethod.Get, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            Assert.False(interaction.RequestBody.HasValue);

            Assert.Equal(HttpStatusCode.NotFound, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);
            
            var (content, type) = interaction.ResponseBody!.Value;
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), type);
            Assert.Equal("NOTHIN' DOIN'", content);
        }

        [Fact]
        public void Read_ErrorWithNoBody_ReturnsErrorInteraction()
        {
            var interactions = ReadSampleMarkdown("single_error_nobody.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);

            Assert.Equal(HttpMethod.Delete, interaction.Method);
            Assert.Equal(_baselineMockRequestHeaders, interaction.RequestHeaders);
            Assert.False(interaction.RequestBody.HasValue);

            Assert.Equal(HttpStatusCode.Forbidden, interaction.StatusCode);
            Assert.Equal(_baselineMockResponseHeaders, interaction.ResponseHeaders);
            Assert.False(interaction.ResponseBody.HasValue);
        }

        [Fact]
        public void Read_Conversation_ReturnsAllInteractionsInADictionary()
        {
            var interactions = ReadSampleMarkdown("conversation.md");
            Assert.Equal(3, interactions.Count);

            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service_1", interaction.Path);
            Assert.Equal(HttpMethod.Get, interaction.Method);

            interaction = interactions[1];
            Assert.Equal(1, interaction.Number);
            Assert.Equal("/a/mock/service_2", interaction.Path);
            Assert.Equal(HttpMethod.Post, interaction.Method);

            interaction = interactions[2];
            Assert.Equal(2, interaction.Number);
            Assert.Equal("/a/mock/service_3", interaction.Path);
            Assert.Equal(HttpMethod.Get, interaction.Method);
        }

        [Fact]
        public void Read_Conversation_ReturnsDictionaryWithGaps()
        {
            var interactions = ReadSampleMarkdown("outofsequence_conversation.md");
            Assert.Equal(2, interactions.Count);

            var interaction = interactions[2];
            Assert.Equal(2, interaction.Number);
            Assert.Equal("/a/mock/service_2", interaction.Path);
            Assert.Equal(HttpMethod.Post, interaction.Method);

            interaction = interactions[5];
            Assert.Equal(5, interaction.Number);
            Assert.Equal("/a/mock/service_1", interaction.Path);
            Assert.Equal(HttpMethod.Get, interaction.Method);
        }

        [Fact]
        public void Read_TextNote_ReturnsInteractionWithTextNote()
        {
            var interactions = ReadSampleMarkdown("single_get_note.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];

            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);
            Assert.Single(interaction.Notes);
            var note = interaction.Notes.First();
            Assert.Equal(IInteraction.Note.NoteType.Text, note.Type);
            Assert.Equal("A Note", note.Title);
            Assert.Equal(@"Lots of
noteworthy things
to be
noting.", note.Content);
        }

        [Fact]
        public void Read_CodeNote_ReturnsInteractionWithCodeNote()
        {
            var interactions = ReadSampleMarkdown("single_get_codenote.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];

            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);
            Assert.Single(interaction.Notes);
            var note = interaction.Notes.First();
            Assert.Equal(IInteraction.Note.NoteType.Code, note.Type);
            Assert.Equal("A Code Note", note.Title);
            Assert.Equal(@"Lots of
noteworthy code
to be
running.", note.Content);
        }


        [Theory]
        [InlineData("single_get_notes.md")]
        [InlineData("single_get_notes_indentedcodeblocks.md")]
        public void Read_MultipleNotes_ReturnsInteractionWithCodeNote(string sampleFile)
        {
            var interactions = ReadSampleMarkdown(sampleFile);
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];

            Assert.Equal(0, interaction.Number);
            Assert.Equal("/a/mock/service", interaction.Path);
            Assert.Equal(3, interaction.Notes.Count());
            var note = interaction.Notes.First();
            Assert.Equal(IInteraction.Note.NoteType.Text, note.Type);
            Assert.Equal("A Note", note.Title);
            Assert.Equal(@"Lots of
noteworthy things
to be
noting.", note.Content);
            note = interaction.Notes.ElementAt(1);
            Assert.Equal(IInteraction.Note.NoteType.Code, note.Type);
            Assert.Equal("A Code Note", note.Title);
            Assert.Equal(@"Lots of
noteworthy code
to be
running.", note.Content);
            note = interaction.Notes.Last();
            Assert.Equal(IInteraction.Note.NoteType.Text, note.Type);
            Assert.Equal("Another Note", note.Title);
            Assert.Equal(@"Even more
noteworthy things
to be
noting.", note.Content);
        }


        [Fact]
        public void Read_InvalidScript_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new MarkdownScriptReader().Read(new StringReader("SOME CRAP.")));
        }


        [Fact]
        public void Read_InvalidInteractions_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new MarkdownScriptReader().Read(new StringReader(@"
## Interaction 0: GET /a/mock/service_1

CRAP

## Interaction 1: POST /a/mock/service_2

MORE CRAP

## Interaction 2: GET /a/mock/service_3

ALSO CRAP")));
        }
    }
}
