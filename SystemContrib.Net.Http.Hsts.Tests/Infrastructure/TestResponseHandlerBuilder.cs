using System.Net;
using System.Net.Http;

namespace MaartenBalliauw.Extensions.Http.Hsts.Tests.Infrastructure
{
    public static class TestResponseHandlerBuilder
    {
        public static TestResponseHandler Build()
        {
            return new TestResponseHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                return response;
            });
        }
        
        public static TestResponseHandler BuildWithHstsHeader(string expectedHstsHeader)
        {
            return new TestResponseHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Headers.Add("Strict-Transport-Security", expectedHstsHeader);
                return response;
            });
        }
    }
}