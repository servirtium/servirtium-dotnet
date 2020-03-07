using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Servirtium.Core.Interactions
{
    class TextAndBinaryBodyFormatter : IBodyFormatter
    {
        //Logic copied from https://github.com/servirtium/servirtium-java/blob/master/core/src/main/java/com/paulhammant/servirtium/ServirtiumServer.java
        private static bool IsTextMediaType(MediaTypeHeaderValue mediaType)=> mediaType.MediaType.StartsWith("text/") ||
                mediaType.MediaType.StartsWith("image/svg") ||
                mediaType.MediaType.StartsWith("multipart/form-data") ||
                mediaType.MediaType.StartsWith("application/json") ||
                mediaType.MediaType.StartsWith("application/xml") ||
                (mediaType.MediaType.StartsWith("application/") && mediaType.MediaType.Contains("script")) ||
                mediaType.MediaType.StartsWith("application/xhtml+xml");

        public TextAndBinaryBodyFormatter(IBodyFormatter textBodyFormatter, IBodyFormatter binaryBodyFormatter)
        {
            _textBodyFormatter = textBodyFormatter;
            _binaryBodyFormatter = binaryBodyFormatter;
        }

        private readonly IBodyFormatter _textBodyFormatter, _binaryBodyFormatter;
        public byte[] Read(string recordedBody, MediaTypeHeaderValue contentType)
        {
            if (IsTextMediaType(contentType))
            {
                return _textBodyFormatter.Read(recordedBody, contentType);
            }
            else
            {
                return _binaryBodyFormatter.Read(recordedBody, contentType);
            }
        }


        public string Write(byte[] bodyContent, MediaTypeHeaderValue contentType)
        {
            if (IsTextMediaType(contentType))
            {
                return _textBodyFormatter.Write(bodyContent, contentType);
            }
            else
            {
                return _binaryBodyFormatter.Write(bodyContent, contentType);
            }
        }
    }
}
