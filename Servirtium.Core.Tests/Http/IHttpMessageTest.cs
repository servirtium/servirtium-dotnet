using Servirtium.Core.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Http
{
    public class IHttpMessageTest
    {

        [Fact]
        public void FixContentLengthHeader_ValidBodyAndCapitalisedContentLengthHeader_ReturnsHeadersWithContentLengthSetToBodyLength()
        {

            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "A gazillion"),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), false);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "The updated body.".Length.ToString()),
                    ("third", "headers")}
                , revised);
        }

        [Fact]
        public void FixContentLengthHeader_ValidBodyAndLowercaseContentLengthHeader_ReturnsHeadersWithContentLengthSetToBodyLength()
        {
            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("content-length", "A gazillion"),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), false);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("content-length", "The updated body.".Length.ToString()),
                    ("third", "headers")}
                , revised);
            
        }

        [Fact]
        public void FixContentLengthHeader_ContentLengthHeaderWithNonStandardCasing_ReturnsHeadersWithContentLengthSetToBodyLength()
        {

            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("CONTENT-LENGTH", "A gazillion"),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), false);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("CONTENT-LENGTH", "The updated body.".Length.ToString()),
                    ("third", "headers")}
                , revised);
        }

        [Fact]
        public void FixContentLengthHeader_CreateContentLengthHeaderFlagNotSetAndNotPresentInInput_ReturnsHeadersWithoutContentLength()
        {

            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), false);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("third", "headers")}
                , revised);
        }

        [Fact]
        public void FixContentLengthHeader_CreateContentLengthHeaderFlagSetAndNotPresentInInput_AddsContentLengthSetToBodyLength()
        {

            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), true);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("third", "headers"),
                    ("Content-Length", "The updated body.".Length.ToString())}
                , revised);
        }

        [Fact]
        public void FixContentLengthHeader_CreateContentLengthHeaderFlagSetAndPresentInInput_ChangesContentLengthSetToBodyLength()
        {

            var revised = IHttpMessage.FixContentLengthHeader(new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "The updated body.".Length.ToString()),
                    ("third", "headers")}, Encoding.UTF8.GetBytes("The updated body."), false);
            Assert.Equal(
                new[]{
                    ("first", "the"),
                    ("second", "original"),
                    ("Content-Length", "The updated body.".Length.ToString()),
                    ("third", "headers") }
                , revised);
        }
    }
}
