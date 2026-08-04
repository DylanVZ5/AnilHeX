using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SaveWriter
{
    private readonly string _savePath;
    private readonly object _rootRubyData;

    public SaveWriter(string savePath, object rootRubyData)
    {
        _savePath = savePath;
        _rootRubyData = rootRubyData;
    }

    public bool Save()
    {
        try
        {
            string backupPath = _savePath + ".bak";
            if (File.Exists(_savePath)) File.Copy(_savePath, backupPath, overwrite: true);

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

    public bool ModifyPartyPokemon(int index, int? level = null, bool? isShiny = null, int[] ivs = null, int[] evs = null)
    {
        if (_rootRubyData == null)
        {
            Console.WriteLine("[-] ERROR: _rootRubyData es null. SaveParser no asignó el modelo original.");
            return false;
        }

        RubyObject trainerObj = FindObjectByClass(_rootRubyData, "PokeBattle_Trainer") 
                             ?? FindObjectByClass(_rootRubyData, "Player")
                             ?? FindObjectWithAttribute(_rootRubyData, "@party");

        if (trainerObj == null)
        {
            Console.WriteLine("[-] ERROR: No se encontró la clase del Entrenador en la partida.");
            return false;
        }

        var partyData = trainerObj.Get("@party");
        if (partyData == null)
        {
            Console.WriteLine("[-] ERROR: El entrenador no tiene el atributo @party.");
            return false;
        }

        if (!(partyData is IList partyList))
        {
            Console.WriteLine($"[-] ERROR: @party no es una lista válida, es de tipo: {partyData.GetType()}");
            return false;
        }

        if (index < 0 || index >= partyList.Count)
        {
            Console.WriteLine($"[-] ERROR: Índice {index} fuera de rango (Equipo tiene {partyList.Count} Pokémon).");
            return false;
        }

        if (!(partyList[index] is RubyObject pokeRo))
        {
            Console.WriteLine($"[-] ERROR: El elemento en el índice {index} no es un Pokémon (RubyObject).");
            return false;
        }

        // Si superamos todos los chequeos, aplicamos los cambios
        if (level.HasValue) pokeRo.Set("@level", level.Value);
        if (isShiny.HasValue) pokeRo.Set("@shiny", isShiny.Value);
        if (ivs != null && ivs.Length == 6) UpdateStatsAttribute(pokeRo, "@iv", "@ivs", ivs);
        if (evs != null && evs.Length == 6) UpdateStatsAttribute(pokeRo, "@ev", "@evs", evs);
        
        return true;
    }

    private void UpdateStatsAttribute(RubyObject pokeRo, string attr1, string attr2, int[] newStats)
    {
        string targetAttr = pokeRo.Attributes.ContainsKey(attr1) ? attr1 : attr2;
        var currentData = pokeRo.Get(targetAttr);

        if (currentData is IList list && list.Count >= 6)
        {
            list[0] = newStats[0]; list[1] = newStats[1]; list[2] = newStats[2];
            list[3] = newStats[5]; list[4] = newStats[3]; list[5] = newStats[4]; 
        }
        else if (currentData is IDictionary dict)
        {
            var keys = new List<object>();
            foreach (var key in dict.Keys) keys.Add(key);

            foreach (var key in keys)
            {
                string kStr = key.ToString().Replace(":", "").Replace("@", "").ToUpper();
                if (kStr == "HP" || kStr == "0") dict[key] = newStats[0];
                else if (kStr == "ATTACK" || kStr == "ATK" || kStr == "1") dict[key] = newStats[1];
                else if (kStr == "DEFENSE" || kStr == "DEF" || kStr == "2") dict[key] = newStats[2];
                else if (kStr == "SPEED" || kStr == "SPD" || kStr == "3") dict[key] = newStats[5];
                else if (kStr.Contains("SP") && kStr.Contains("ATK") || kStr == "4") dict[key] = newStats[3];
                else if (kStr.Contains("SP") && kStr.Contains("DEF") || kStr == "5") dict[key] = newStats[4];
            }
        }
    }

    private RubyObject FindObjectByClass(object node, string className, HashSet<object> visited = null)
    {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        if (node is RubyObject ro)
        {
            if (ro.ClassName != null && ro.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                return ro;
            foreach (var val in ro.Attributes.Values)
            {
                var found = FindObjectByClass(val, className, visited);
                if (found != null) return found;
            }
        }
        else if (node is IList list)
        {
            foreach (var item in list)
            {
                var found = FindObjectByClass(item, className, visited);
                if (found != null) return found;
            }
        }
        else if (node is IDictionary dict)
        {
            foreach (var entry in dict.Values)
            {
                var found = FindObjectByClass(entry, className, visited);
                if (found != null) return found;
            }
        }
        return null;
    }

    private RubyObject FindObjectWithAttribute(object node, string attrName, HashSet<object> visited = null)
    {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        if (node is RubyObject ro)
        {
            if (ro.Attributes.ContainsKey(attrName)) return ro;
            foreach (var val in ro.Attributes.Values)
            {
                var found = FindObjectWithAttribute(val, attrName, visited);
                if (found != null) return found;
            }
        }
        else if (node is IList list)
        {
            foreach (var item in list)
            {
                var found = FindObjectWithAttribute(item, attrName, visited);
                if (found != null) return found;
            }
        }
        else if (node is IDictionary dict)
        {
            foreach (var item in dict.Values)
            {
                var found = FindObjectWithAttribute(item, attrName, visited);
                if (found != null) return found;
            }
        }
        return null;
    }
}