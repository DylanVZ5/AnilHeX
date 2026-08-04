using System;
using System.Collections.Generic;

public class RubyObject
{
    public string ClassName { get; set; }
    // IMPORTANTE: Se quitó el IgnoreCase. Ruby diferencia entre @hp y @HP.
    public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

    public RubyObject(string className = null) { ClassName = className; }

    public object Get(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName)) return null;
        string cleanKey = attributeName.StartsWith("@") ? attributeName : "@" + attributeName;
        if (Attributes.TryGetValue(cleanKey, out var value)) return value;
        if (Attributes.TryGetValue(attributeName, out value)) return value;
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
    public RubySymbol(string name) { Name = name; }
    public override string ToString() => Name;
}

public class RubyUserDefined
{
    public string ClassName { get; set; }
    public byte[] Data { get; set; }
    public RubyUserDefined(string className, byte[] data) { ClassName = className; Data = data; }
}

public class RubyClass
{
    public string Name { get; set; }
    public RubyClass(string name) { Name = name; }
}

public class RubyModule
{
    public string Name { get; set; }
    public RubyModule(string name) { Name = name; }
}

public class RubyHash : Dictionary<object, object>
{
    public object DefaultValue { get; set; }
    public bool HasDefault { get; set; }
}

public class RubyWrapper
{
    public object WrappedObject { get; set; }
    public Dictionary<RubySymbol, object> InstanceVariables { get; set; } = new Dictionary<RubySymbol, object>();
    public RubyWrapper(object wrapped) { WrappedObject = wrapped; }
}