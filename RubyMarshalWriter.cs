using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class RubyMarshalWriter
{
    private readonly BinaryWriter _writer;
    private readonly Dictionary<string, int> _symbolTable = new Dictionary<string, int>();

    public RubyMarshalWriter(Stream stream)
    {
        _writer = new BinaryWriter(stream);
    }

    public void WriteHeader()
    {
        _writer.Write((byte)4); // Major
        _writer.Write((byte)8); // Minor
    }

    public void WriteValue(object value)
    {
        if (value == null)
        {
            _writer.Write((byte)'0');
        }
        else if (value is bool b)
        {
            _writer.Write((byte)(b ? 'T' : 'F'));
        }
        else if (value is int i)
        {
            _writer.Write((byte)'i');
            WriteFixnum(i);
        }
        else if (value is long l)
        {
            _writer.Write((byte)'i');
            WriteFixnum((int)l);
        }
        else if (value is string s)
        {
            _writer.Write((byte)'"');
            WriteStringBytes(Encoding.UTF8.GetBytes(s));
        }
        else if (value is RubySymbol sym)
        {
            WriteSymbol(sym.Name);
        }
        else if (value is List<object> list)
        {
            _writer.Write((byte)'[');
            WriteFixnum(list.Count);
            foreach (var item in list)
            {
                WriteValue(item);
            }
        }
        else if (value is Dictionary<object, object> dict)
        {
            _writer.Write((byte)'{');
            WriteFixnum(dict.Count);
            foreach (var kvp in dict)
            {
                WriteValue(kvp.Key);
                WriteValue(kvp.Value);
            }
        }
        else if (value is RubyObject ro)
        {
            WriteRubyObject(ro);
        }
        else
        {
            throw new NotSupportedException($"Tipo de dato no soportado en Marshal: {value.GetType()}");
        }
    }

    private void WriteRubyObject(RubyObject ro)
    {
        _writer.Write((byte)'o');
        WriteSymbol(ro.ClassName);
        WriteFixnum(ro.Attributes.Count);

        foreach (var kvp in ro.Attributes)
        {
            string attrName = kvp.Key;
            if (!attrName.StartsWith("@")) attrName = "@" + attrName;

            WriteSymbol(attrName);
            WriteValue(kvp.Value);
        }
    }

    private void WriteSymbol(string name)
    {
        if (_symbolTable.TryGetValue(name, out int index))
        {
            _writer.Write((byte)';');
            WriteFixnum(index);
        }
        else
        {
            _writer.Write((byte)':');
            byte[] bytes = Encoding.UTF8.GetBytes(name);
            WriteStringBytes(bytes);
            _symbolTable[name] = _symbolTable.Count;
        }
    }

    private void WriteFixnum(int value)
    {
        if (value == 0)
        {
            _writer.Write((byte)0);
            return;
        }

        if (value > 0 && value < 123)
        {
            _writer.Write((byte)(value + 5));
            return;
        }

        if (value < 0 && value > -124)
        {
            _writer.Write((byte)(value - 5));
            return;
        }

        byte[] bytes = BitConverter.GetBytes(value);
        int len = 4;
        while (len > 1 && bytes[len - 1] == (value < 0 ? (byte)0xFF : (byte)0x00))
        {
            len--;
        }

        if (value < 0)
        {
            _writer.Write((sbyte)(-len));
        }
        else
        {
            _writer.Write((byte)len);
        }

        for (int i = 0; i < len; i++)
        {
            _writer.Write(bytes[i]);
        }
    }

    private void WriteStringBytes(byte[] bytes)
    {
        WriteFixnum(bytes.Length);
        _writer.Write(bytes);
    }
}