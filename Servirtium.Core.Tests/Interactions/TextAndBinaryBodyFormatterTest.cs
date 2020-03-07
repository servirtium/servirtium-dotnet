using Moq;
using Servirtium.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Tests.Interactions
{
    public class TextAndBinaryBodyFormatterTest
    {
        Mock<IBodyFormatter> _mockText = new Mock<IBodyFormatter>(), _mockBinary = new Mock<IBodyFormatter>();

        public enum Expected
        { 
            Text, Binary
        }

        public TextAndBinaryBodyFormatterTest()
        {
            _mockText.Setup(t => t.Read(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>())).Returns(Encoding.UTF8.GetBytes("Text Message Body"));
            _mockText.Setup(t => t.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>())).Returns("Recorded Text Body");
            _mockBinary.Setup(t => t.Read(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>())).Returns(Encoding.UTF8.GetBytes("Binary Message Body"));
            _mockBinary.Setup(t => t.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>())).Returns("Recorded Binary Body");
        }

        [Theory]
        [InlineData("text/plain", Expected.Text)]
        [InlineData("text/html", Expected.Text)]
        [InlineData("text/csv", Expected.Text)]
        [InlineData("text/css", Expected.Text)]
        [InlineData("image/png", Expected.Binary)]
        [InlineData("image/jpeg", Expected.Binary)]
        [InlineData("image/x-icon", Expected.Binary)]
        [InlineData("image/svg", Expected.Text)]
        [InlineData("application/octet-stream", Expected.Binary)]
        [InlineData("application/json", Expected.Text)]
        [InlineData("application/xml", Expected.Text)]
        [InlineData("application/pdf", Expected.Binary)]
        [InlineData("multipart/form-data", Expected.Text)]
        [InlineData("video/mp4", Expected.Binary)]
        [InlineData("audio/ogg", Expected.Binary)]
        [InlineData("font/ttf", Expected.Binary)]
        [InlineData("text/csv; charset=utf8", Expected.Text)]
        public void Read_Always_DelegatesToCorrectWriterBasedOnMediaType(string mediaTypeHeaderValue, Expected expectedType)
        {
            var mediaHeader = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            var result = new TextAndBinaryBodyFormatter(_mockText.Object, _mockBinary.Object).Read("Recorded Body", mediaHeader);
            if (expectedType == Expected.Text)
            {
                _mockText.Verify(t => t.Read("Recorded Body", mediaHeader));
                _mockBinary.Verify(t => t.Read(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>()), Times.Never());
                Assert.Equal("Text Message Body", Encoding.UTF8.GetString(result));
            }
            else
            {
                _mockBinary.Verify(t => t.Read("Recorded Body", mediaHeader));
                _mockText.Verify(t => t.Read(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>()), Times.Never());
                Assert.Equal("Binary Message Body", Encoding.UTF8.GetString(result));
            }
        }

        [Theory]
        [InlineData("text/plain", Expected.Text)]
        [InlineData("text/html", Expected.Text)]
        [InlineData("text/csv", Expected.Text)]
        [InlineData("text/css", Expected.Text)]
        [InlineData("image/png", Expected.Binary)]
        [InlineData("image/jpeg", Expected.Binary)]
        [InlineData("image/x-icon", Expected.Binary)]
        [InlineData("image/svg", Expected.Text)]
        [InlineData("application/octet-stream", Expected.Binary)]
        [InlineData("application/json", Expected.Text)]
        [InlineData("application/xml", Expected.Text)]
        [InlineData("application/pdf", Expected.Binary)]
        [InlineData("multipart/form-data", Expected.Text)]
        [InlineData("video/mp4", Expected.Binary)]
        [InlineData("audio/ogg", Expected.Binary)]
        [InlineData("font/ttf", Expected.Binary)]
        [InlineData("text/csv; charset=utf8", Expected.Text)]
        public void Write_Always_DelegatesToCorrectReaderBasedOnMediaType(string mediaTypeHeaderValue, Expected expectedType)
        {
            var mediaHeader = MediaTypeHeaderValue.Parse(mediaTypeHeaderValue);
            var inputBody = Encoding.UTF8.GetBytes("Message Body");
            var result= new TextAndBinaryBodyFormatter(_mockText.Object, _mockBinary.Object).Write(inputBody, mediaHeader);
            if (expectedType == Expected.Text)
            {
                _mockText.Verify(t => t.Write(inputBody, mediaHeader));
                _mockBinary.Verify(t => t.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>()), Times.Never());
                Assert.Equal("Recorded Text Body", result);
            }
            else
            {
                _mockBinary.Verify(t => t.Write(inputBody, mediaHeader));
                _mockText.Verify(t => t.Write(It.IsAny<byte[]>(), It.IsAny<MediaTypeHeaderValue>()), Times.Never());
                Assert.Equal("Recorded Binary Body", result);
            }
        }

    }
}
