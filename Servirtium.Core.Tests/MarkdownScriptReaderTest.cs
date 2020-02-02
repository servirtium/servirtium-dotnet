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
    public class MarkdownScriptReaderTest
    {
        private static IDictionary<int, IInteraction> ReadSampleMarkdown(string scriptName) => 
            new MarkdownScriptReader().Read(
                File.OpenText($"markdown_conversations{Path.DirectorySeparatorChar}{scriptName}")
            );


        [Fact]
        public void Read_SingleGetRequest_ReturnsSingleInteraction()
        {
            var interactions = ReadSampleMarkdown("single_get.md");
            Assert.Equal(1, interactions.Count);
            var interaction = interactions[0];
            Assert.Equal(0, interaction.Number);
            Assert.Equal(HttpMethod.Get, interaction.Method);
            Assert.Equal("/a/mock/service", interaction.Path);
            Assert.Equal(new[]{
                    ("Accept", "text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2"),
                    ("User-Agent", "Servirtium-Testing"),
                    ("Connection", "keep-alive"),
                    ("Host", "mock.service-host.org")
                },
                interaction.RequestHeaders
                );
            Assert.False(interaction.HasRequestBody);
            Assert.Equal(HttpStatusCode.OK, interaction.StatusCode);
            Assert.Equal(new[]{
                    ("Content-Type", "text/plain"),
                    ("Connection", "keep-alive"),
                    ("Transfer-Encoding", "chunked")
                },
                interaction.ResponseHeaders
                );
            Assert.Equal(MediaTypeHeaderValue.Parse("text/plain"), interaction.ResponseContentType);
            Assert.Equal("SIMPLE GET RESPONSE", interaction.ResponseBody);
        }
    }
}
