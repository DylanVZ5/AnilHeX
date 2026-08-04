#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class RubyMarshalReader
{
    private readonly BinaryReader _reader;
    private readonly List<string> _symbolTable = new List<string>();
    private readonly List<object> _objectCache = new List<object>(); 

    public RubyMarshalReader(Stream stream)
    {
        _reader = new BinaryReader(stream);
        byte major = _reader.ReadByte();
        byte minor = _reader.ReadByte();
        if (major != 4 || minor != 8) throw new InvalidDataException($"Versión no soportada: {major}.{minor}");
    }

    public object? ReadValue()
    {
        long pos = _reader.BaseStream.Position;
        byte type = _reader.ReadByte();

        switch ((char)type)
        {
            case '0': case 'n': return null;
            case 'T': return true;
            case 'F': return false;
            case 'i': return ReadFixnum();
            case 'l': 
                char sign = (char)_reader.ReadByte();
                int shortsCount = ReadFixnum();
                byte[] bignumBytes = _reader.ReadBytes(shortsCount * 2);
                System.Numerics.BigInteger bigInt = 0;
                System.Numerics.BigInteger multiplier = 1;
                for (int b = 0; b < bignumBytes.Length; b += 2)
                {
                    ushort word = BitConverter.ToUInt16(bignumBytes, b);
                    bigInt += word * multiplier;
                    multiplier *= 65536;
                }
                if (sign == '-') bigInt = -bigInt;
                object bignumResult = (bigInt >= long.MinValue && bigInt <= long.MaxValue) ? (long)bigInt : bigInt;
                _objectCache.Add(bignumResult);
                return bignumResult;
            case '"': 
                string str = Encoding.UTF8.GetString(ReadStringBytes());
                _objectCache.Add(str);
                return str;
            case ':': return ReadSymbol();
            case ';': 
                int symIndex = ReadFixnum();
                if (_symbolTable != null && symIndex >= 0 && symIndex < _symbolTable.Count)
                    return new RubySymbol(_symbolTable[symIndex]);
                return new RubySymbol($"symbol_{symIndex}");
            case '[': 
                int count = ReadFixnum();
                var list = new List<object?>();
                _objectCache.Add(list); 
                for (int i = 0; i < count; i++) list.Add(ReadValue());
                return list;
            case '{': 
                int hashCount = ReadFixnum();
                var dict = new Dictionary<object, object?>();
                _objectCache.Add(dict); 
                for (int i = 0; i < hashCount; i++)
                {
                    var key = ReadValue() ?? "null_key";
                    var val = ReadValue();
                    dict[key] = val;
                }
                return dict;
            case '}': 
                int hashDefCount = ReadFixnum();
                var dictDef = new RubyHash { HasDefault = true };
                _objectCache.Add(dictDef); 
                for (int i = 0; i < hashDefCount; i++)
                {
                    var key = ReadValue() ?? "null_key";
                    var val = ReadValue();
                    dictDef[key] = val;
                }
                dictDef.DefaultValue = ReadValue() ?? new object(); 
                return dictDef;
            case 'o': 
                object? classObj = ReadValue();
                string className = classObj is RubySymbol rs ? rs.Name : classObj?.ToString() ?? "Unknown";
                var obj = new RubyObject(className);
                _objectCache.Add(obj); 
                int attrCount = ReadFixnum();
                for (int i = 0; i < attrCount; i++)
                {
                    object? attrSymObj = ReadValue(); 
                    object? attrVal = ReadValue();
                    string attrName = attrSymObj is RubySymbol s ? s.Name : attrSymObj?.ToString() ?? "Unknown";
                    obj.Set(attrName, attrVal);
                }
                return obj;

            // CORRECCIÓN CRÍTICA: Se encapsulan las variables invisibles
            case 'I': 
                var wrappedVal = ReadValue();
                var wrapper = new RubyWrapper(wrappedVal ?? new object());
                int ivarCount = ReadFixnum();
                for (int i = 0; i < ivarCount; i++)
                {
                    var sym = ReadValue() as RubySymbol;
                    var val = ReadValue();
                    if (sym != null) wrapper.InstanceVariables[sym] = val ?? new object();
                }
                return wrapper;

            case '@': return _objectCache[ReadFixnum()];
            case 'u': 
                object? uClassObj = ReadValue();
                string uClassName = uClassObj is RubySymbol urs ? urs.Name : uClassObj?.ToString() ?? "Unknown";
                var uObj = new RubyUserDefined(uClassName, ReadStringBytes()); 
                _objectCache.Add(uObj);
                return uObj;
            case 'c': 
                var cObj = new RubyClass(Encoding.UTF8.GetString(_reader.ReadBytes(ReadFixnum())));
                _objectCache.Add(cObj);
                return cObj;
            case 'm': 
                var mObj = new RubyModule(Encoding.UTF8.GetString(_reader.ReadBytes(ReadFixnum())));
                _objectCache.Add(mObj);
                return mObj;
            case 'f': 
                double fVal = double.Parse(Encoding.UTF8.GetString(ReadStringBytes()), System.Globalization.CultureInfo.InvariantCulture);
                _objectCache.Add(fVal);
                return fVal;
            default:
                throw new NotSupportedException($"Tipo no soportado: '{(char)type}' (0x{type:X2}) en 0x{pos:X}");
        }
    }

    private RubySymbol ReadSymbol()
    {
        string name = Encoding.UTF8.GetString(ReadStringBytes());
        _symbolTable.Add(name);
        return new RubySymbol(name);
    }

    private int ReadFixnum()
    {
        sbyte b = _reader.ReadSByte();
        if (b == 0) return 0;
        if (b > 4) return b - 5;
        if (b < -4) return b + 5;
        int len = b < 0 ? -b : b;
        byte[] bytes = _reader.ReadBytes(len);
        int result = 0;
        if (b > 0) for (int i = 0; i < len; i++) result |= (bytes[i] << (i * 8));
        else { result = -1; for (int i = 0; i < len; i++) { result &= ~(0xFF << (i * 8)); result |= (bytes[i] << (i * 8)); } }
        return result;
    }

    private byte[] ReadStringBytes() => _reader.ReadBytes(ReadFixnum());
}