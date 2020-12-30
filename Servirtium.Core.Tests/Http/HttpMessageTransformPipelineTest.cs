using Servirtium.Core.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace Servirtium.Core.Http.Tests
{
    public class TestHttpMessageTransform : IHttpMessageTransforms
    {
        private readonly string _append; 

        public TestHttpMessageTransform(string append) 
        {
            _append = append;
        }

        public IRequestMessage TransformClientRequestForRealService(IRequestMessage clientRequest) => new ServiceRequest.Builder()
            .From(clientRequest)
            .Body($"{Encoding.UTF8.GetString(clientRequest.Body.Value.Content)}-{_append}", clientRequest.Body.Value.Type)
            .Build();

        public IResponseMessage TransformRealServiceResponseForClient(IResponseMessage serviceResponse) => new ServiceResponse.Builder()
            .From(serviceResponse)
            .Body($"{Encoding.UTF8.GetString(serviceResponse.Body.Value.Content)}-{_append}", serviceResponse.Body.Value.Type)
            .Build();
    }

    public class HttpMessageTransformPipelineTest
    {
        private static readonly ServiceRequest REQUEST = new ServiceRequest.Builder()
            .Body("BODY", MediaTypeHeaderValue.Parse("text/plain"))
            .Build();

        private static readonly ServiceResponse RESPONSE = new ServiceResponse.Builder()
            .Body("BODY", MediaTypeHeaderValue.Parse("text/plain"))
            .Build();

        [Fact]
        public void TransformClientRequestForRealService_NoTransforms_ReturnsUnmodifiedRequest()
        {
            Assert.Equal(REQUEST, new HttpMessageTransformPipeline().TransformClientRequestForRealService(REQUEST));
        }

        [Fact]
        public void TransformClientRequestForRealService_OneTransform_DelegatesToTransform()
        {
            Assert.Equal(
                new ServiceRequest.Builder().From(REQUEST).Body("BODY-A", MediaTypeHeaderValue.Parse("text/plain")).Build(), 
                new HttpMessageTransformPipeline(new TestHttpMessageTransform("A")).TransformClientRequestForRealService(REQUEST)
            );
        }

        [Fact]
        public void TransformClientRequestForRealService_MultipleTransform_AppliesTransformsInParameterOrder()
        {
            Assert.Equal(
                new ServiceRequest.Builder().From(REQUEST).Body("BODY-A-B-C", MediaTypeHeaderValue.Parse("text/plain")).Build(),
                new HttpMessageTransformPipeline(
                    new TestHttpMessageTransform("A"), new TestHttpMessageTransform("B"), new TestHttpMessageTransform("C"))
                        .TransformClientRequestForRealService(REQUEST)
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_NoTransforms_ReturnsUnmodifiedRequest()
        {
            Assert.Equal(RESPONSE, new HttpMessageTransformPipeline().TransformRealServiceResponseForClient(RESPONSE));
        }

        [Fact]
        public void TransformRealServiceResponseForClient_OneTransform_DelegatesToTransform()
        {
            Assert.Equal(
                new ServiceResponse.Builder().From(RESPONSE).Body("BODY-A", MediaTypeHeaderValue.Parse("text/plain")).Build(),
                new HttpMessageTransformPipeline(new TestHttpMessageTransform("A")).TransformRealServiceResponseForClient(RESPONSE)
            );
        }

        [Fact]
        public void TransformRealServiceResponseForClient_MultipleTransform_AppliesTransformsInParameterOrder()
        {
            Assert.Equal(
                new ServiceResponse.Builder().From(RESPONSE).Body("BODY-A-B-C", MediaTypeHeaderValue.Parse("text/plain")).Build(),
                new HttpMessageTransformPipeline(
                    new TestHttpMessageTransform("A"), new TestHttpMessageTransform("B"), new TestHttpMessageTransform("C"))
                        .TransformRealServiceResponseForClient(RESPONSE)
            );
        }
    }
}
