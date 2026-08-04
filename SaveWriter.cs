using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SaveWriter
{
    private readonly string _savePath;
    private readonly object _rootRubyData;

    public SaveWriter(string savePath, object rootRubyData) { _savePath = savePath; _rootRubyData = rootRubyData; }

    public bool Save()
    {
        try
        {
            string backupPath = _savePath + ".bak";
            if (File.Exists(_savePath)) File.Copy(_savePath, backupPath, overwrite: true);

            // ESCRIBIMOS DIRECTAMENTE SIN COMPRIMIR (Como lo requiere Añil)
            using (FileStream fs = new FileStream(_savePath, FileMode.Create, FileAccess.Write))
            {
                RubyMarshalWriter writer = new RubyMarshalWriter(fs);
                writer.WriteHeader();
                writer.WriteValue(_rootRubyData);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error al guardar la partida: {ex.Message}");
            return false;
        }
    }

    public bool ModifyPartyPokemon(int index, string newNickname = null)
    {
        if (_rootRubyData == null) return false;

        RubyObject trainerObj = FindObjectByClass(_rootRubyData, "PokeBattle_Trainer") ?? FindObjectByClass(_rootRubyData, "Player") ?? FindObjectWithAttribute(_rootRubyData, "@party");
        if (trainerObj == null) return false;

        var partyData = trainerObj.Get("@party");
        if (partyData is RubyWrapper rwParty) partyData = rwParty.WrappedObject;

        if (partyData is IList partyList && index >= 0 && index < partyList.Count)
        {
            var pokeData = partyList[index];
            if (pokeData is RubyWrapper rwPoke) pokeData = rwPoke.WrappedObject;

            if (pokeData is RubyObject pokeRo)
            {
                // Solo cambiaremos el nombre para comprobar que la escritura funciona
                if (!string.IsNullOrEmpty(newNickname))
                {
                    // En Ruby, los strings deben envolverse para mantener la compatibilidad de encoding
                    var nameWrapper = new RubyWrapper(newNickname);
                    nameWrapper.InstanceVariables[new RubySymbol("E")] = true; 
                    pokeRo.Set("@name", nameWrapper);
                }
                
                return true;
            }
        }
        return false;
    }

    private RubyObject FindObjectByClass(object node, string className, HashSet<object> visited = null)
    {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        if (node is RubyWrapper rw) return FindObjectByClass(rw.WrappedObject, className, visited);

        if (node is RubyObject ro)
        {
            if (ro.ClassName != null && ro.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase)) return ro;
            foreach (var val in ro.Attributes.Values) { var found = FindObjectByClass(val, className, visited); if (found != null) return found; }
        }
        else if (node is IList list) { foreach (var item in list) { var found = FindObjectByClass(item, className, visited); if (found != null) return found; } }
        else if (node is IDictionary dict) { foreach (var entry in dict.Values) { var found = FindObjectByClass(entry, className, visited); if (found != null) return found; } }
        return null;
    }

    private RubyObject FindObjectWithAttribute(object node, string attrName, HashSet<object> visited = null)
    {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        if (node is RubyWrapper rw) return FindObjectWithAttribute(rw.WrappedObject, attrName, visited);

        if (node is RubyObject ro)
        {
            if (ro.Attributes.ContainsKey(attrName)) return ro;
            foreach (var val in ro.Attributes.Values) { var found = FindObjectWithAttribute(val, attrName, visited); if (found != null) return found; }
        }
        else if (node is IList list) { foreach (var item in list) { var found = FindObjectWithAttribute(item, attrName, visited); if (found != null) return found; } }
        else if (node is IDictionary dict) { foreach (var item in dict.Values) { var found = FindObjectWithAttribute(item, attrName, visited); if (found != null) return found; } }
        return null;
    }
}