using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DotNetContrib.Net.Http.Hsts.Domain;
using DotNetContrib.Net.Http.Hsts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetContrib.Net.Http.Hsts
{
    public class HstsHandler : DelegatingHandler
    {             
        private readonly ILogger<HstsHandler> _logger;
        private readonly IHstsStore _store;

        public HstsHandler(IHstsStore store, ILogger<HstsHandler> logger = null, HttpMessageHandler innerHandler = null) 
            : base(innerHandler)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? NullLogger<HstsHandler>.Instance;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // https://tools.ietf.org/html/rfc6797#section-8
            
            // MUST check for http scheme
            if (string.Equals(request.RequestUri.Scheme, HttpTransportScheme.Http, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Request to {RequestUri} is using http scheme.", request.RequestUri);
                
                // 8.3 - 1. Extract from the URI any substring described by the host component of the authority component of the URI.
                var hostComponent = request.RequestUri.IdnHost;
                if (IPAddress.TryParse(hostComponent, out _))
                {
                    // 8.3 - 3. Else, if the substring is non-null and syntactically matches the IP-literal or IPv4address productions from Section 3.2.2 of [RFC3986], then there is no match with any Known HSTS Host.
                    _logger.LogDebug("Request to {RequestUri} syntactically matches an IP address. No further HSTS processing is required before making the request.", request.RequestUri);
                }
                else
                {
                    // 8.3 - 4. Otherwise, the substring is a given domain name, which MUST be matched against the UA's Known HSTS Hosts using the procedure in Section 8.2 ("Known HSTS Host Domain Name Matching").
                    if (_store.TryFind(hostComponent, out var hstsDomain) && hstsDomain.AppliesTo(hostComponent))
                    {
                        _logger.LogDebug("Request to {RequestUri} requires HSTS according to store.", request.RequestUri);
                        
                        var newRequestUri = new UriBuilder(request.RequestUri);
                        
                        // 8.3 - 5. (...) The UA MUST replace the URI scheme with "https" [RFC2818]
                        newRequestUri.Scheme = HttpTransportScheme.Https;
                        
                        // 8.3 - 5. (...) if the URI contains an explicit port component of "80", then the UA MUST convert the port component to be "443"
                        if (newRequestUri.Port == 80)
                        {
                            newRequestUri.Port = 443;
                        }

                        // Update URI
                        _logger.LogDebug("Request to {RequestUri} has been rewritten to {NewRequestUri}.", request.RequestUri, newRequestUri.Uri);
                        request.RequestUri = newRequestUri.Uri;
                    }
                }
            }
            
            // Fetch response
            var response = await base.SendAsync(request, cancellationToken);

            // 8.1. Strict-Transport-Security Response Header Field Processing
            if (response.Headers.TryGetValues("Strict-Transport-Security", out var hstsHeaders))
            {                
                var hstsHeader = hstsHeaders.FirstOrDefault();
                if (!string.IsNullOrEmpty(hstsHeader) && NameValueWithParametersHeaderValue.TryParse(hstsHeader, out var parsedValue))
                {
                    _logger.LogDebug("Request to {RequestUri} returned Strict-Transport-Security header value: {HeaderValue}", request.RequestUri, parsedValue);
                    
                    // TODO: Either I am stupid, or .NET does not have a nice way of parsing *just the parameters*
                    parsedValue.Parameters.Add(new NameValueHeaderValue(parsedValue.Name, parsedValue.Value));
                    
                    // Check individual parts
                    var includeSubdomains = parsedValue.Parameters.Any(p =>
                        string.Equals(p.Name, "includeSubdomains", StringComparison.OrdinalIgnoreCase));

                    var permanent = parsedValue.Parameters.Any(p =>
                        string.Equals(p.Name, "preload", StringComparison.OrdinalIgnoreCase));
                    
                    int.TryParse(parsedValue.Parameters.FirstOrDefault(p =>
                        string.Equals(p.Name, "max-age", StringComparison.OrdinalIgnoreCase))?.Value?.Replace("\"", string.Empty), out var maxAge);

                    if (_store.Update(response.RequestMessage.RequestUri.IdnHost,
                        includeSubdomains,
                        permanent,
                        maxAge))
                    {
                        _logger.LogDebug("Updated {HostComponent} with Strict-Transport-Security header value: {HeaderValue}", response.RequestMessage.RequestUri.IdnHost, parsedValue);
                    }
                }
            }
            
            // Return response
            return response;
        }
    }
}