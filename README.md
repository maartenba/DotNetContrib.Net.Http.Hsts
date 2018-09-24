# DotNetContrib.Net.Http.Hsts

HTTP Strict Transport Security (HSTS) support for System.Net.HttpClient, enforcing HTTPS when required by the remote host or preload list.

## Installation (NuGet)

	Install-Package DotNetContrib.Net.Http.Hsts

## Usage

To enable HTTP Strict Transport Security (HSTS) support for System.Net.HttpClient, we will need a couple of things:

* An `IHstsStore`, e.g. `InMemoryHstsStore`, to keep track of domains that need HSTS.
* A `System.Net.HttpClient` which uses `HstsHandler`

In code:

```csharp
var store = new InMemoryHstsStore();
var client = new HttpClient(new HstsHandler(store));

// will request HTTP URL:
var response1 = await client.GetAsync("http://www.github.com");

// will request HTTPS URL, since host returned HSTS details:
var response2 = await client.GetAsync("http://www.github.com");
```

## Pre-defining hosts that require HTTPS

HTTP Strict Transport Security (HSTS) is best when enabled by default for certain URLs that are known to require HTTPS. The `IHstsStore` supports marking a domain as requiring HTTPS:

```csharp
store.Update("example.org", true, true, int.MaxValue);

// will always request HTTPS URL:
var response = await client.GetAsync("http://www.example.org");
```
