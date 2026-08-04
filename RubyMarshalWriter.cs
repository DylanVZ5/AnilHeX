#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class RubyMarshalWriter
{
    private readonly BinaryWriter _writer;
    private readonly Dictionary<string, int> _symbolTable = new Dictionary<string, int>();
    // CRÍTICO: El caché de objetos para no duplicar memoria y evitar recursividad infinita
    private readonly Dictionary<object, int> _objectCache = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

    public RubyMarshalWriter(Stream stream)
    {
        _writer = new BinaryWriter(stream);
    }

    public void WriteHeader()
    {
        _writer.Write((byte)4);
        _writer.Write((byte)8);
    }

    public void WriteValue(object? value)
    {
        if (value == null) { _writer.Write((byte)'0'); return; }
        if (value is bool b) { _writer.Write((byte)(b ? 'T' : 'F')); return; }
        if (value is int i) { _writer.Write((byte)'i'); WriteFixnum(i); return; }
        if (value is RubySymbol sym) { WriteSymbol(sym.Name); return; }

        // Si el objeto ya se escribió antes, escribimos un enlace '@'
        if (_objectCache.TryGetValue(value, out int index))
        {
            _writer.Write((byte)'@');
            WriteFixnum(index);
            return;
        }

        // Si es nuevo, lo registramos en el caché
        _objectCache[value] = _objectCache.Count;

        if (value is string s)
        {
            _writer.Write((byte)'"');
            WriteStringBytes(Encoding.UTF8.GetBytes(s));
        }
        else if (value is double d)
        {
            _writer.Write((byte)'f');
            string fs = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            WriteStringBytes(Encoding.UTF8.GetBytes(fs));
        }
        else if (value is long l)
        {
            _writer.Write((byte)'l');
            char sign = l < 0 ? '-' : '+';
            _writer.Write((byte)sign);
            long absL = Math.Abs(l);
            var words = new List<ushort>();
            while (absL > 0)
            {
                words.Add((ushort)(absL & 0xFFFF));
                absL >>= 16;
            }
            WriteFixnum(words.Count);
            foreach (var word in words) _writer.Write(BitConverter.GetBytes(word));
        }
        else if (value is System.Numerics.BigInteger bi)
        {
            _writer.Write((byte)'l');
            char sign = bi.Sign < 0 ? '-' : '+';
            _writer.Write((byte)sign);
            System.Numerics.BigInteger absL = System.Numerics.BigInteger.Abs(bi);
            var words = new List<ushort>();
            while (absL > 0)
            {
                words.Add((ushort)(absL & 0xFFFF));
                absL >>= 16;
            }
            WriteFixnum(words.Count);
            foreach (var word in words) _writer.Write(BitConverter.GetBytes(word));
        }
        else if (value is IList list)
        {
            _writer.Write((byte)'[');
            WriteFixnum(list.Count);
            foreach (var item in list) WriteValue(item);
        }
        else if (value is RubyHash rh)
        {
            _writer.Write((byte)(rh.HasDefault ? '}' : '{'));
            WriteFixnum(rh.Count);
            
            // CORRECCIÓN: Iterar usando var (se resuelve como KeyValuePair<object, object>)
            foreach (var kvp in rh) 
            { 
                WriteValue(kvp.Key); 
                WriteValue(kvp.Value); 
            }
            
            if (rh.HasDefault) WriteValue(rh.DefaultValue);
        }
        else if (value is IDictionary dict)
        {
            _writer.Write((byte)'{');
            WriteFixnum(dict.Count);
            foreach (DictionaryEntry kvp in dict) { WriteValue(kvp.Key); WriteValue(kvp.Value); }
        }
        else if (value is RubyUserDefined rud)
        {
            _writer.Write((byte)'u');
            WriteSymbol(rud.ClassName);
            WriteStringBytes(rud.Data);
        }
        else if (value is RubyClass rc)
        {
            _writer.Write((byte)'c');
            byte[] bytes = Encoding.UTF8.GetBytes(rc.Name);
            WriteStringBytes(bytes);
        }
        else if (value is RubyModule rm)
        {
            _writer.Write((byte)'m');
            byte[] bytes = Encoding.UTF8.GetBytes(rm.Name);
            WriteStringBytes(bytes);
        }
        else if (value is RubyObject ro)
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
        else
        {
            throw new NotSupportedException($"Tipo de dato no soportado en Marshal: {value.GetType()}");
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
        if (value == 0) { _writer.Write((byte)0); return; }
        if (value > 0 && value < 123) { _writer.Write((byte)(value + 5)); return; }
        if (value < -4 && value > -124) { _writer.Write((byte)(value - 5)); return; }

        byte[] bytes = BitConverter.GetBytes(value);
        int len = 4;
        while (len > 1 && bytes[len - 1] == (value < 0 ? 0xFF : 0x00)) len--;

        if (value < 0) _writer.Write((sbyte)(-len));
        else _writer.Write((byte)len);

        for (int i = 0; i < len; i++) _writer.Write(bytes[i]);
    }

    private void WriteStringBytes(byte[] bytes)
    {
        WriteFixnum(bytes.Length);
        _writer.Write(bytes);
    }
}