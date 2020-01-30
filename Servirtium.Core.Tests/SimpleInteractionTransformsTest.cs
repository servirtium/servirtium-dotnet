using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using static Servirtium.Core.SimpleInteractionTransforms;

namespace Servirtium.Core.Tests
{
    public class SimpleInteractionTransformsTest
    {
        IInteraction _baseInteraction = new MarkdownInteraction.Builder()
            .Number(1337)
            .Path("down/the/garden")
            .RequestHeaders(new[] { ("mock-header", "mock-value"), ("another-mock-header", "another-mock-value") })
            .Build();



        [Fact]
        public void TransformClientRequestForRealService_NoRequestHeadersToRemoveAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed =new SimpleInteractionTransforms(new Uri("http://mock.com"))
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.Number, transformed.Number);
            Assert.Equal(_baseInteraction.Path, transformed.Path);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders);
            Assert.Null(transformed.RequestContentType);
            Assert.Null(transformed.RequestBody);
        }

        [Fact]
        public void TransformClientRequestForRealService_RequestHeadersToRemoveNotPresentAndNoHostRequestHeader_ReturnsUnchangedClone()
        {
            var transformed = new SimpleInteractionTransforms(new Uri("http://mock.com"), new Regex[] { new Regex("missing-mock-header"), new Regex("another-missing-mock-header") }, new Regex[0])
                .TransformClientRequestForRealService(_baseInteraction);
            Assert.Equal(_baseInteraction.Number, transformed.Number);
            Assert.Equal(_baseInteraction.Path, transformed.Path);
            Assert.Equal(_baseInteraction.RequestHeaders, transformed.RequestHeaders);
            Assert.Null(transformed.RequestContentType);
            Assert.Null(transformed.RequestBody);
        }
    }
}
