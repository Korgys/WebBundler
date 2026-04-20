using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Core;
using WebBundler.Minification;

namespace WebBundler.Minification.Tests;

[TestClass]
public sealed class DefaultAssetMinifiersTests
{
    [TestMethod]
    public void CreatesTheDefaultCssAndJavaScriptMinifiers()
    {
        var minifiers = DefaultAssetMinifiers.Create().ToArray();
        var supportedTypes = minifiers.Select(minifier => minifier.SupportedType).ToArray();
        var expectedTypes = new[] { BundleType.Css, BundleType.JavaScript };

        Assert.HasCount(2, minifiers);
        CollectionAssert.AreEqual(expectedTypes, supportedTypes);
    }
}
