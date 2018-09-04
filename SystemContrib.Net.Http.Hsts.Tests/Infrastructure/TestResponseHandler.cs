using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SystemContrib.Net.Http.Hsts.Tests.Infrastructure
{
    public class TestResponseHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _expectedResponseFactory;
        
        public TestResponseHandler(Func<HttpRequestMessage, HttpResponseMessage> expectedResponseFactory)
        {
            _expectedResponseFactory = expectedResponseFactory;
        }
        
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_expectedResponseFactory(request));
        }
    }
}