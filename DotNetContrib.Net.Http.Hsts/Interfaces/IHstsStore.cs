using DotNetContrib.Net.Http.Hsts.Domain;

namespace DotNetContrib.Net.Http.Hsts.Interfaces
{
    public interface IHstsStore
    {
        bool TryFind(string hostComponent, out HstsDomain hstsDomain);
        bool Update(string hostComponent, bool includeSubdomains, bool permanent, long maxAge);
    }
}