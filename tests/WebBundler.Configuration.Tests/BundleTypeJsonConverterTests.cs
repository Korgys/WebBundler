using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Configuration;
using WebBundler.Core;

namespace WebBundler.Configuration.Tests;

[TestClass]
public sealed class BundleTypeJsonConverterTests
{
    [TestMethod]
    public void ReadsAndWritesBundleTypes()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new BundleTypeJsonConverter());

        Assert.AreEqual(BundleType.Css, JsonSerializer.Deserialize<BundleType>("\"CSS\"", options));
        Assert.AreEqual(BundleType.JavaScript, JsonSerializer.Deserialize<BundleType>("\"javascript\"", options));
        Assert.AreEqual("\"css\"", JsonSerializer.Serialize(BundleType.Css, options));
        Assert.AreEqual("\"js\"", JsonSerializer.Serialize(BundleType.JavaScript, options));
    }

    [TestMethod]
    public void RejectsUnsupportedBundleTypes()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new BundleTypeJsonConverter());

        try
        {
            JsonSerializer.Deserialize<BundleType>("\"less\"", options);
            Assert.Fail("Expected JsonException.");
        }
        catch (JsonException)
        {
        }
    }
}
