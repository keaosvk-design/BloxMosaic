using System.Drawing;
using System.Reflection;

namespace MultiBloxy;

internal sealed class ResourceCache : IDisposable
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly Dictionary<string, Icon> _icons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Image> _images = new(StringComparer.Ordinal);
    private bool _disposed;

    public Icon GetIcon(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_icons.TryGetValue(name, out Icon? icon))
        {
            return icon;
        }

        icon = LoadIcon(name);
        _icons.Add(name, icon);
        return icon;
    }

    public Image GetImage(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_images.TryGetValue(name, out Image? image))
        {
            return image;
        }

        image = LoadImage(name);
        _images.Add(name, image);
        return image;
    }

    public Image GetIconImage(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string cacheKey = $"icon:{name}";
        if (_images.TryGetValue(cacheKey, out Image? image))
        {
            return image;
        }

        image = GetIcon(name).ToBitmap();
        _images.Add(cacheKey, image);
        return image;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (Icon icon in _icons.Values)
        {
            icon.Dispose();
        }

        foreach (Image image in _images.Values)
        {
            image.Dispose();
        }

        _icons.Clear();
        _images.Clear();
        _disposed = true;
    }

    private Icon LoadIcon(string name)
    {
        using Stream? stream = GetResourceStream(name, "ico");
        if (stream is null)
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        using Icon source = new(stream);
        return (Icon)source.Clone();
    }

    private Image LoadImage(string name)
    {
        using Stream? stream = GetResourceStream(name, "png");
        if (stream is null)
        {
            return new Bitmap(1, 1);
        }

        using Image source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
        return new Bitmap(source);
    }

    private Stream? GetResourceStream(string name, string extension) =>
        _assembly.GetManifestResourceStream(
            $"{_assembly.GetName().Name}.Resources.{name}.{extension}");
}
