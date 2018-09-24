# DotNetContrib.Net.Http.Hsts

HTTP Strict Transport Security (HSTS) support for System.Net.HttpClient, enforcing HTTPS when required by the remote host or preload list.

## Installation (NuGet)

	Install-Package DotNetContrib.Net.Http.Hsts -Prerelease

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

// will request HTTPS URL, even when HTTP is provided, since host returned HSTS details:
var response2 = await client.GetAsync("http://www.github.com");
```

## Pre-defining hosts that require HTTPS

HTTP Strict Transport Security (HSTS) is best when enabled by default for certain URLs that are known to require HTTPS. The `IHstsStore` supports marking a domain as requiring HTTPS:

```csharp
store.Update("example.org", true, true, int.MaxValue);

// will always request HTTPS URL, even when HTTP is provided:
var response = await client.GetAsync("http://www.example.org");
```

No [HSTS preload lists](https://github.com/maartenba/DotNetContrib.Net.Http.Hsts/issues/1) are currently included. [Feel free to contribute this](https://github.com/maartenba/DotNetContrib.Net.Http.Hsts/issues/1).

## Background

This project was created as an example solution for [an issue that was logged in the `dotnet/corefx` repository ("HSTS support for HttpClient")](https://github.com/dotnet/corefx/issues/31882).

Various server-side middlewares for ASP.NET exist, emitting HTTP Strict Transport Security (HSTS) headers. While browsers respect these, the .NET `HttpClient` does not. And while the [applicable RFC (6797)](https://tools.ietf.org/html/rfc6797) only talks about browsers, having support in `HttpClient` makes a lot of sense as well, especially when users are able to enter API endpoint URLs that will get called.

One example is in [the NuGet client](https://github.com/NuGet/Home/issues/6940), where packages can be pushed to NuGet.org via a URL that users of `NuGet.exe` provide through the `-Source` parameter. When a user provided a non-HTTPS URL, there is a big chance credentials are sent unencryped. An example request:

    GET http://api.nuget.org/v3/index.json 1.1
    X-NuGet-ApiKey: dba23c47-139c-4626-84f1-f72a308209c1
    
    ...

While NuGet.org enforces HSTS through the use of the `Strict-Transport-Security` for browsers, the `HttpClient` can happily make calls over HTTP.

Using `DotNetContrib.Net.Http.Hsts`, this type of request can be easily prevented:

```csharp
var store = new InMemoryHstsStore();
var client = new HttpClient(new HstsHandler(store));

// always require HTTPS for NuGet.org
store.Update("nuget.org", true, true, int.MaxValue);

// will always request HTTPS URL, even when HTTP is provided:
var response = await client.GetAsync("http://api.nuget.org/v3/index.json");
```

Additionally, for hosts that are not hardcoded using `store.Update()`, `DotNetContrib.Net.Http.Hsts` will respect HSTS headers sent by the server.

```csharp
var store = new InMemoryHstsStore();
var client = new HttpClient(new HstsHandler(store));

// will request HTTP URL first time:
var response1 = await client.GetAsync("http://www.github.com");

// will request HTTPS URL, even when HTTP is provided, since host returned HSTS details:
var response2 = await client.GetAsync("http://www.github.com");
```

More background and discussion is available [in the original "HSTS support for HttpClient" issue](https://github.com/dotnet/corefx/issues/31882).