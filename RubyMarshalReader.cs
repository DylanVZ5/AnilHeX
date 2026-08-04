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

        if (major != 4 || minor != 8)
        {
            throw new InvalidDataException($"Versión de Ruby Marshal no soportada: {major}.{minor}");
        }
    }

    public object? ReadValue()
    {
        long pos = _reader.BaseStream.Position;
        byte type = _reader.ReadByte();

        switch ((char)type)
        {
            case '0': // nil
            case 'n': // nil alternativo
                return null;
            case 'T': // true
                return true;
            case 'F': // false
                return false;
            case 'i': // Fixnum
                return ReadFixnum();
            case 'l': // Bignum
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

            case '"': // String
                string str = Encoding.UTF8.GetString(ReadStringBytes());
                _objectCache.Add(str);
                return str;

            case ':': // Symbol
                return ReadSymbol();

            case ';': // Symbol reference
                int symIndex = ReadFixnum();
                if (_symbolTable != null && symIndex >= 0 && symIndex < _symbolTable.Count)
                    return new RubySymbol(_symbolTable[symIndex]);
                return new RubySymbol($"symbol_{symIndex}");

            case '[': // Array
                int count = ReadFixnum();
                var list = new List<object?>();
                _objectCache.Add(list); 
                for (int i = 0; i < count; i++)
                    list.Add(ReadValue());
                return list;

            case '{': // Hash normal
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

            case '}': // Hash con valor por defecto (NUEVO)
                int hashDefCount = ReadFixnum();
                var dictDef = new Dictionary<object, object?>();
                _objectCache.Add(dictDef); 
                for (int i = 0; i < hashDefCount; i++)
                {
                    var key = ReadValue() ?? "null_key";
                    var val = ReadValue();
                    dictDef[key] = val;
                }
                var defaultVal = ReadValue(); // Ruby Marshal añade el valor por defecto al final
                // Si en el futuro necesitas usar este defaultVal, puedes crear una clase personalizada.
                // Por ahora, lo leemos para mantener la sincronización.
                return dictDef;

            case 'o': // Ruby Object
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

            case 'I': // IVAR (Objetos envueltos)
                var wrappedVal = ReadValue();
                int ivarCount = ReadFixnum();
                for (int i = 0; i < ivarCount; i++)
                {
                    ReadValue(); // Nombre de la variable (Symbol)
                    ReadValue(); // Valor de la variable
                }
                return wrappedVal;

            case '@': // Enlace de Objeto (Object Link)
                int objIndex = ReadFixnum();
                return _objectCache[objIndex];

            case 'u': // User Defined (Color, Tone, Table)
                object? uClassObj = ReadValue();
                string uClassName = uClassObj is RubySymbol urs ? urs.Name : uClassObj?.ToString() ?? "Unknown";
                byte[] uData = ReadStringBytes();
                var uObj = new RubyObject(uClassName); 
                _objectCache.Add(uObj);
                return uObj;

            case 'c': // Class (NUEVO)
            case 'm': // Module (NUEVO)
                int cmLen = ReadFixnum();
                string cmName = Encoding.UTF8.GetString(_reader.ReadBytes(cmLen));
                var cmObj = new RubyObject(cmName);
                _objectCache.Add(cmObj);
                return cmObj;

            case 'f': // Float
                string floatStr = Encoding.UTF8.GetString(ReadStringBytes());
                double fVal = double.Parse(floatStr, System.Globalization.CultureInfo.InvariantCulture);
                _objectCache.Add(fVal);
                return fVal;

            default:
                throw new NotSupportedException(
                    $"Tipo no soportado o desincronización en offset 0x{pos:X}: '{(char)type}' (0x{type:X2}). Stream Position = 0x{_reader.BaseStream.Position:X}"
                );
        }
    }

    private RubySymbol ReadSymbol()
    {
        byte[] bytes = ReadStringBytes();
        string name = Encoding.UTF8.GetString(bytes);
        _symbolTable.Add(name);
        return new RubySymbol(name);
    }

    private int ReadFixnum()
    {
        sbyte b = _reader.ReadSByte();
        if (b == 0) return 0;
        if (b > 4) return b - 5;
        if (b < -4) return b + 5;

        int len = b;
        if (len < 0) len = -len;

        byte[] bytes = _reader.ReadBytes(len);
        int result = 0;

        if (b > 0)
        {
            for (int i = 0; i < len; i++)
                result |= (bytes[i] << (i * 8));
        }
        else
        {
            result = -1;
            for (int i = 0; i < len; i++)
            {
                result &= ~(0xFF << (i * 8));
                result |= (bytes[i] << (i * 8));
            }
        }
        return result;
    }

    private byte[] ReadStringBytes()
    {
        int len = ReadFixnum();
        return _reader.ReadBytes(len);
    }
}