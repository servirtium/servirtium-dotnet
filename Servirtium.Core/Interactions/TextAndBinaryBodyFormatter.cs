using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Servirtium.Core.Interactions
{
    class TextAndBinaryBodyFormatter : IBodyFormatter
    {
        private readonly ILogger<TextAndBinaryBodyFormatter> _logger;
        
        //Logic copied from https://github.com/servirtium/servirtium-java/blob/master/core/src/main/java/com/paulhammant/servirtium/ServirtiumServer.java
        public static bool IsTextMediaType(MediaTypeHeaderValue mediaType)=> mediaType.MediaType.StartsWith("text/") ||
                mediaType.MediaType.StartsWith("image/svg") ||
                mediaType.MediaType.StartsWith("multipart/form-data") ||
                mediaType.MediaType.StartsWith("application/json") ||
                mediaType.MediaType.StartsWith("application/xml") ||
                (mediaType.MediaType.StartsWith("application/") && mediaType.MediaType.Contains("script")) ||
                mediaType.MediaType.StartsWith("application/xhtml+xml");

        
        
        public TextAndBinaryBodyFormatter(IBodyFormatter textBodyFormatter, IBodyFormatter binaryBodyFormatter, ILoggerFactory? loggerFactory = null)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TextAndBinaryBodyFormatter>();
            _textBodyFormatter = textBodyFormatter;
            _binaryBodyFormatter = binaryBodyFormatter;
        }

        private readonly IBodyFormatter _textBodyFormatter, _binaryBodyFormatter;
        public byte[] Read(string recordedBody, MediaTypeHeaderValue contentType)
        {
            if (IsTextMediaType(contentType))
            {
                _logger.LogDebug($"Reading '{contentType}' body as text");
                return _textBodyFormatter.Read(recordedBody, contentType);
            }
            else
            {
                _logger.LogDebug($"Reading '{contentType}' body as binary");
                return _binaryBodyFormatter.Read(recordedBody, contentType);
            }
        }


        public string Write(byte[] bodyContent, MediaTypeHeaderValue contentType)
        {
            if (IsTextMediaType(contentType))
            {
                _logger.LogDebug($"Writing '{contentType}' body as text");
                return _textBodyFormatter.Write(bodyContent, contentType);
            }
            else
            {
                _logger.LogDebug($"Writing '{contentType}' body as binary");
                return _binaryBodyFormatter.Write(bodyContent, contentType);
            }
        }
    }
}
