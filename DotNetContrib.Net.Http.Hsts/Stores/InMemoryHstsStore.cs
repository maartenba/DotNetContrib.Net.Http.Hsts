using System;
using System.Collections.Concurrent;
using DotNetContrib.Net.Http.Hsts.Domain;
using DotNetContrib.Net.Http.Hsts.Interfaces;

namespace DotNetContrib.Net.Http.Hsts.Stores
{
    // TODO: Probably needs a more performant inner store
    public class InMemoryHstsStore : IHstsStore
    {
        private ConcurrentDictionary<string, HstsDomain> _innerStore = new ConcurrentDictionary<string, HstsDomain>(StringComparer.OrdinalIgnoreCase);
        
        // TODO: Perf optimization in finding matching domain - probably store domains reversed as com.example.foo, then search by components
        public bool TryFind(string hostComponent, out HstsDomain hstsDomain)
        {
            // Fetch applicable domain (direct match)
            if (_innerStore.TryGetValue(hostComponent, out var domain) && domain.AppliesTo(hostComponent))
            {
                hstsDomain = domain;
                return true;
            }

            // Fetch applicable domain (full search)
            foreach (var item in _innerStore)
            {
                if (item.Value.AppliesTo(hostComponent))
                {
                    hstsDomain = item.Value;
                    return true;
                }
            }

            // No match
            hstsDomain = null;
            return false;
        }

        public bool Update(string hostComponent, bool includeSubdomains, bool permanent, long maxAge)
        {
            if (maxAge == 0)
            {
                return _innerStore.TryRemove(hostComponent, out _);
            }
            else
            {
                if (!TryFind(hostComponent, out var existing))
                {
                    var referenceTime = DateTimeOffset.UtcNow;
                    _innerStore[hostComponent] = new HstsDomain(hostComponent, includeSubdomains, permanent, referenceTime, referenceTime.AddSeconds(maxAge));
                    return true;
                }
                else
                {
                    if (existing.IncludeSubdomains != includeSubdomains || existing.Permanent != permanent)
                    {
                        _innerStore[hostComponent] = existing.ApplyUpdate(includeSubdomains, permanent);
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}