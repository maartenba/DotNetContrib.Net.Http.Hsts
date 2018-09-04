using System;

namespace SystemContrib.Net.Http.Hsts
{
    public class HstsDomain
    {
        public string Domain { get; }
        public bool IncludeSubdomains { get; }
        public bool Permanent { get; }

        public DateTimeOffset Created { get; }
        public DateTimeOffset Expires { get; }

        public HstsDomain(string domain, bool includeSubdomains, bool permanent, DateTimeOffset created, DateTimeOffset expires)
        {
            Domain = domain;
            IncludeSubdomains = includeSubdomains;
            Permanent = permanent;
            Created = created;
            Expires = expires;
        }

        public bool AppliesTo(string domain)
        {
            // Do we have a possible match?
            var possibleMatch = Domain == domain || (IncludeSubdomains && domain.EndsWith("." + Domain));
            if (!possibleMatch)
            {
                return false;
            }

            // No check on expiry needed when permanent
            if (Permanent)
            {
                return true;
            }

            // Use expiry
            return Expires >= DateTimeOffset.UtcNow;
        }

        public HstsDomain ApplyUpdate(bool includeSubdomains, bool permanent)
        {
            // https://tools.ietf.org/html/rfc6797#section-8.1.1
            // The UA MUST NOT modify the expiry time or the includeSubDomains directive of any superdomain matched Known HSTS Host.
            if (IncludeSubdomains != includeSubdomains || Permanent != permanent)
            {
                return new HstsDomain(Domain, IncludeSubdomains || includeSubdomains, permanent, Created, Expires);
            }

            return this;
        }
    }
}