using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Servirtium.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Servirtium.AspNetCore.Tests
{
    public class AspNetCoreServirtiumRequestHandlerTest
    {
        private readonly Mock<IInteractionMonitor> _mockMonitor;

        private readonly Mock<IInteractionTransforms> _mockTransform;

        private readonly Mock<IHeaderDictionary> _mockRequestHeaders, _mockResponseHeaders;

        private readonly Mock<IEnumerator<KeyValuePair<string, StringValues>>> _mockRequestHeaderEnumerator, _mockResponseHeaderEnumerator;

        private readonly Mock<Stream> _mockRequestBody, _mockResponseBody;

        private readonly Mock<Action<HttpStatusCode>> _mockStatusCodeSetter;

        private IInteraction _interactionToTransform;

        private readonly ICollection<IInteraction.Note> _notes = new IInteraction.Note[0];

        private ServiceResponse _responseWithNoHeaders = new ServiceResponse(Encoding.UTF8.GetBytes("Rock your body right."), MediaTypeHeaderValue.Parse("text/plain"), HttpStatusCode.OK, new (string, string)[0]);

        public AspNetCoreServirtiumRequestHandlerTest()
        {
            _mockMonitor = new Mock<IInteractionMonitor>();
            _mockMonitor.Setup(m => m.GetServiceResponseForRequest(It.IsAny<Uri>(), It.IsAny<IInteraction>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(_responseWithNoHeaders));

            _mockTransform = new Mock<IInteractionTransforms>();
            _mockRequestHeaderEnumerator = new Mock<IEnumerator<KeyValuePair<string, StringValues>>>();
            _mockRequestHeaders = new Mock<IHeaderDictionary>();
            _mockRequestHeaders.Setup(rh => rh.GetEnumerator()).Returns(_mockRequestHeaderEnumerator.Object);

            _mockResponseHeaderEnumerator = new Mock<IEnumerator<KeyValuePair<string, StringValues>>>();
            _mockResponseHeaders = new Mock<IHeaderDictionary>();
            _mockResponseHeaders.Setup(rh => rh.GetEnumerator()).Returns(_mockResponseHeaderEnumerator.Object);

            _mockTransform.Setup(t => t.TransformClientRequestForRealService(It.IsAny<IInteraction>())).Returns<IInteraction>((i) =>
            {
                _interactionToTransform = i;
                return i;
            });
            _mockTransform.Setup(t => t.TransformRealServiceResponseForClient(It.IsAny<ServiceResponse>())).Returns<ServiceResponse>((sr) => sr);

            _mockRequestBody = new Mock<Stream>();
            _mockResponseBody = new Mock<Stream>();

            _mockStatusCodeSetter = new Mock<Action<HttpStatusCode>>();
        }

        private void HandleNoBodyRequest(AspNetCoreServirtiumRequestHandler handler)
        {
            handler.HandleRequest(new Uri("http://a.mock.service"), "endpoint", HttpMethod.Get.Method, _mockRequestHeaders.Object, null, null, _mockStatusCodeSetter.Object, _mockResponseHeaders.Object, _mockResponseBody.Object, _notes).Wait();
        }

        [Fact]
        public void HandleRequest_RequestWithNoBodyOrHeaders_TransformsInteractionBuiltFromInputs()
        {
            HandleNoBodyRequest(new AspNetCoreServirtiumRequestHandler(_mockMonitor.Object, _mockTransform.Object, new InteractionCounter()));
            _mockTransform.Verify(t => t.TransformClientRequestForRealService(It.IsAny<IInteraction>()));
            Assert.Equal(HttpMethod.Get, _interactionToTransform.Method);
            Assert.Equal("endpoint", _interactionToTransform.Path);
            Assert.Empty(_interactionToTransform.RequestHeaders);
            Assert.False(_interactionToTransform.HasRequestBody); 
        }
    }
}
