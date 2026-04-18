namespace WebBundler.Core;

public interface IAssetMinifier
{
    BundleType SupportedType { get; }

    string Minify(string content);
}
