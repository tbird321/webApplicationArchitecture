using System;
using System.Linq;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// Path normalisation is the only part of invalidation that can be tested without AWS, and it
/// is where the mistakes are: a path missing its leading slash silently clears nothing, and a
/// long list silently costs money. CloudFront reports success either way.
/// </summary>
public class CloudFrontCacheTests
{
    [Fact]
    public void NoPaths_MeansWholeSite()
    {
        Assert.Equal(new[] { "/*" }, CloudFrontCache.NormalizePaths(null!));
        Assert.Equal(new[] { "/*" }, CloudFrontCache.NormalizePaths(Array.Empty<string>()));
        Assert.Equal(new[] { "/*" }, CloudFrontCache.NormalizePaths(new[] { "", "   " }));
    }

    [Theory]
    [InlineData("about-us/", "/about-us/")]
    [InlineData("/about-us/", "/about-us/")]
    [InlineData("  /about-us/  ", "/about-us/")]
    [InlineData("sitemap.xml", "/sitemap.xml")]
    public void AddsLeadingSlash(string input, string expected)
    {
        // CloudFront accepts a path without a leading slash and then matches nothing.
        Assert.Equal(new[] { expected }, CloudFrontCache.NormalizePaths(new[] { input }));
    }

    [Theory]
    [InlineData("https://www.cesletter.info/about-us/", "/about-us/")]
    [InlineData("http://www.cesletter.info/sitemap.xml", "/sitemap.xml")]
    [InlineData("https://www.cesletter.info/", "/")]
    public void AcceptsAFullUrl(string input, string expected)
    {
        // You have the URL in your hand when you have just looked at the page.
        Assert.Equal(new[] { expected }, CloudFrontCache.NormalizePaths(new[] { input }));
    }

    [Fact]
    public void Deduplicates()
    {
        var result = CloudFrontCache.NormalizePaths(new[]
        {
            "/about-us/", "about-us/", "https://www.cesletter.info/about-us/", "/faq/"
        });
        Assert.Equal(new[] { "/about-us/", "/faq/" }, result);
    }

    [Fact]
    public void WildcardCollapsesEverythingElse()
    {
        // "/*" already covers the rest, and collapsing drops the bill from N paths to one.
        var result = CloudFrontCache.NormalizePaths(new[] { "/about-us/", "/*", "/faq/" });
        Assert.Equal(new[] { "/*" }, result);
    }

    [Fact]
    public void RejectsTooManyPaths_WithAnActionableMessage()
    {
        var many = Enumerable.Range(0, CloudFrontCache.MaxPaths + 1).Select(i => $"/page-{i}/").ToArray();
        var ex = Assert.Throws<ArgumentException>(() => CloudFrontCache.NormalizePaths(many));
        Assert.Contains("/*", ex.Message);
    }

    [Fact]
    public void AcceptsExactlyTheLimit()
    {
        var atLimit = Enumerable.Range(0, CloudFrontCache.MaxPaths).Select(i => $"/page-{i}/").ToArray();
        Assert.Equal(CloudFrontCache.MaxPaths, CloudFrontCache.NormalizePaths(atLimit).Count);
    }

    [Fact]
    public void SiteDomainLookup_CoversEveryRegisteredSite()
    {
        // The invalidate endpoint resolves the domain from here rather than the database, so an
        // id present in the registry must always yield a usable domain.
        foreach (var id in StaticPageRenderer.KnownSiteIds)
        {
            Assert.True(StaticPageRenderer.TryGetSiteDomain(id, out var domain), $"website {id} has no domain");
            Assert.False(string.IsNullOrWhiteSpace(domain));
            Assert.Contains(".", domain);
        }
    }

    [Fact]
    public void SiteDomainLookup_FailsLoudlyForAnUnknownSite()
    {
        Assert.False(StaticPageRenderer.TryGetSiteDomain(9999, out var domain));
        Assert.Null(domain);
    }
}
