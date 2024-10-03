namespace CSharp_Kubernetes.Overwatch.Updater;

class Manifest
{
    internal static Manifest Load()
    {
        var file = ".manifest";
        if (!File.Exists(file))
        {
            File.Create(file);
            return new Manifest(Array.Empty<string>());
        }

        return new Manifest(File.ReadAllLines(file));
    }
    
    private Dictionary<string, string> _properties;

    private Manifest(string[] values)
    {
        _properties = new Dictionary<string, string>();
        foreach (string value in values)
        {
            if (value.Contains('='))
            {
                var val = value.Split('=');
                _properties[val[0]] = val[1];
            }
            else _properties[value] = "true";
        }
    }

    public Dictionary<string, string>.KeyCollection Properties
    {
        get => _properties.Keys;
    }

    public string? this[string key]
    {
        get
        {
            _properties.TryGetValue(key, out string? val);
            return val;
        }
    }
}