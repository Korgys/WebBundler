using WebBundler.Configuration;
using WebBundler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebBundler.Configuration.Tests;

[TestClass]
public sealed class BundleConfigurationValidatorTests
{
    [TestMethod]
    public void AcceptsAValidConfiguration()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void AcceptsManifestOutput()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            ManifestOutput = "wwwroot/dist/webbundler.manifest.json",
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void RejectsBlankManifestOutput()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            ManifestOutput = " ",
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void RejectsManifestOutputConflictsWithBundleOutput()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            ManifestOutput = "wwwroot/dist/site.min.css",
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void RejectsDuplicateOutputs()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                },
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/theme.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void RejectsEmptyBundleCollections()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles = []
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void WarnsAboutUnusualOutputExtensions()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.txt",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Warning));
    }

    [TestMethod]
    public void RejectsUnsupportedVersionAndMissingFields()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 2,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = string.Empty,
                    Inputs = [],
                    Type = BundleType.JavaScript
                }
            ]
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }
}
