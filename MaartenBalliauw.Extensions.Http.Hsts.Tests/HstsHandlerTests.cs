using System;
using System.Net.Http;
using System.Threading.Tasks;
using MaartenBalliauw.Extensions.Http.Hsts.Tests.Infrastructure;
using Xunit;

namespace MaartenBalliauw.Extensions.Http.Hsts.Tests
{
    public class HstsHandlerTests
    {
        [Theory]
        [InlineData("testpreloadedrequest.com")]
        [InlineData("www.testpreloadedrequest.com")]
        public async Task TestPreloadedRequest(string preloadHostComponent)
        {
            var store = new InMemoryHstsStore();
            store.Update(preloadHostComponent, true, true, int.MaxValue);
                        
            using (var client = new HttpClient(new HstsHandler(store, innerHandler: TestResponseHandlerBuilder.Build())))
            {
                var httpUri = new Uri("http://" + preloadHostComponent);
                var httpsUri = new Uri("https://" + preloadHostComponent);
                
                // Request will always be made to HTTPS, even when attempting HTTP
                var response = await client.GetAsync(httpUri);
                Assert.Equal(httpsUri, response.RequestMessage.RequestUri);
            }
        }
        
        [Theory]
        [InlineData("testnonpreloadedrequest.org")]
        public async Task TestNonPreloadedRequest(string hostComponent)
        {
            using (var client = new HttpClient(new HstsHandler(new InMemoryHstsStore(), innerHandler: 
                TestResponseHandlerBuilder.BuildWithHstsHeader("max-age=31536000; includeSubdomains"))))
            {
                var httpUri = new Uri("http://" + hostComponent);
                var httpsUri = new Uri("https://" + hostComponent);
                
                // First response will be made to HTTP
                var response1 = await client.GetAsync(httpUri);
                Assert.Equal(httpUri, response1.RequestMessage.RequestUri); 
                
                // Second response will be made to HTTPS
                var response2 = await client.GetAsync(httpUri);
                Assert.Equal(httpsUri, response2.RequestMessage.RequestUri); 
            }
        }
        
        [Theory]
        [InlineData("testnonhstsrequest.org")]
        public async Task TestNonHstsRequest(string hostComponent)
        {
            using (var client = new HttpClient(new HstsHandler(new InMemoryHstsStore(), innerHandler: TestResponseHandlerBuilder.Build())))
            {
                var httpUri = new Uri("http://" + hostComponent);
                
                // Response will be made to HTTP
                var response = await client.GetAsync(httpUri);
                Assert.Equal(httpUri, response.RequestMessage.RequestUri);
            }
        }
        
        [Theory]
        [InlineData("testsubdomainrequest.org", true)]
        [InlineData("testsubdomainrequest.org", false)]
        public async Task TestSubdomainRequest(string hostComponent, bool includeSubdomains)
        {
            using (var client = new HttpClient(new HstsHandler(new InMemoryHstsStore(), innerHandler: includeSubdomains
                ? TestResponseHandlerBuilder.BuildWithHstsHeader("max-age=31536000; includeSubdomains")
                : TestResponseHandlerBuilder.BuildWithHstsHeader("max-age=31536000"))))
            {
                var httpUri = new Uri("http://" + hostComponent);
                
                var httpUriForSubdomain = new Uri("http://foo." + hostComponent);
                var httpsUriForSubdomain = new Uri("https://foo." + hostComponent);
                
                // Make request to root domain to fetch HSTS header
                var response1 = await client.GetAsync(httpUri);
                Assert.Equal(httpUri, response1.RequestMessage.RequestUri);
                
                // Make request to subdomain and check for HTTP/HTTPS
                var response2 = await client.GetAsync(httpUriForSubdomain);
                if (!includeSubdomains)
                {
                    Assert.Equal(httpUriForSubdomain, response2.RequestMessage.RequestUri);
                } 
                else 
                {
                    Assert.Equal(httpsUriForSubdomain, response2.RequestMessage.RequestUri);
                }
            }
        }
        
        [Theory]
        [InlineData("http://testportspecificrequest.org/", "https://testportspecificrequest.org/")]
        [InlineData("http://testportspecificrequest.org:80/", "https://testportspecificrequest.org:443/")]
        [InlineData("http://testportspecificrequest.org:1234/", "https://testportspecificrequest.org:1234/")]
        [InlineData("http://1.2.3.4:1234/", "http://1.2.3.4:1234/")]
        public async Task TestPortSpecificRequest(string originalUrl, string rewrittenUrl)
        {
            using (var client = new HttpClient(new HstsHandler(new InMemoryHstsStore(), innerHandler: 
                TestResponseHandlerBuilder.BuildWithHstsHeader("max-age=31536000; includeSubdomains"))))
            {
                var originalUri = new Uri(originalUrl);
                var rewrittenUri = new Uri(rewrittenUrl);
                                
                // Make first request to fetch HSTS header
                var response1 = await client.GetAsync(originalUri);
                Assert.Equal(originalUri, response1.RequestMessage.RequestUri);
                                
                // Make second request and validate rewrite
                var response2 = await client.GetAsync(originalUri);
                Assert.Equal(rewrittenUri, response2.RequestMessage.RequestUri);
            }
        }
    }
}