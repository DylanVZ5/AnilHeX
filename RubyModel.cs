using System;
using System.Collections.Generic;

public class RubyObject
{
    public string ClassName { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public RubyObject(string className = null)
    {
        ClassName = className;
    }

    public object Get(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName)) return null;

        string cleanKey = attributeName.StartsWith("@") ? attributeName : "@" + attributeName;

        if (Attributes.TryGetValue(cleanKey, out var value))
            return value;

        if (Attributes.TryGetValue(attributeName, out value))
            return value;

        return null;
    }

    public void Set(string attributeName, object value)
    {
        if (string.IsNullOrEmpty(attributeName)) return;

        string cleanKey = attributeName.StartsWith("@") ? attributeName : "@" + attributeName;
        Attributes[cleanKey] = value;
    }
}

public class RubySymbol
{
    public string Name { get; set; }

    public RubySymbol(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}