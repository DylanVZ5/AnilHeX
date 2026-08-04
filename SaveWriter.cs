using System;
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

    /// <summary>
    /// Guarda el árbol de objetos Ruby de vuelta en el archivo .rxdata / .dat
    /// </summary>
    public bool Save()
    {
        try
        {
            // 1. Crear copia de seguridad preventiva
            string backupPath = _savePath + ".bak";
            if (File.Exists(_savePath))
            {
                File.Copy(_savePath, backupPath, overwrite: true);
            }

            // 2. Serializar los datos modificados
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

    /// <summary>
    /// Modifica los atributos de un Pokémon específico del equipo por su índice (0 a 5).
    /// </summary>
    public bool ModifyPartyPokemon(int index, int? level = null, bool? isShiny = null, int[] ivs = null, int[] evs = null)
    {
        RubyObject trainerObj = FindObjectWithAttribute(_rootRubyData, "@party");
        if (trainerObj == null) return false;

        if (trainerObj.Get("@party") is List<object> partyList && index >= 0 && index < partyList.Count)
        {
            if (partyList[index] is RubyObject pokeRo)
            {
                // Modificar Nivel
                if (level.HasValue)
                {
                    pokeRo.Set("@level", level.Value);
                }

                // Modificar Shiny
                if (isShiny.HasValue)
                {
                    pokeRo.Set("@shiny", isShiny.Value);
                }

                // Modificar IVs
                if (ivs != null && ivs.Length == 6)
                {
                    UpdateStatsAttribute(pokeRo, "@iv", "@ivs", ivs);
                }

                // Modificar EVs
                if (evs != null && evs.Length == 6)
                {
                    UpdateStatsAttribute(pokeRo, "@ev", "@evs", evs);
                }

                return true;
            }
        }
        return false;
    }

    private void UpdateStatsAttribute(RubyObject pokeRo, string attr1, string attr2, int[] newStats)
    {
        string targetAttr = pokeRo.Attributes.ContainsKey(attr1) ? attr1 : attr2;
        var currentData = pokeRo.Get(targetAttr);

        if (currentData is List<object> list)
        {
            // Formato array: [HP, Atk, Def, SpAtk, SpDef, Speed]
            list[0] = newStats[0];
            list[1] = newStats[1];
            list[2] = newStats[2];
            list[3] = newStats[5]; // Speed
            list[4] = newStats[3]; // SpAtk
            list[5] = newStats[4]; // SpDef
        }
        else if (currentData is Dictionary<object, object> dict)
        {
            // Formato hash
            foreach (var key in dict.Keys)
            {
                string kStr = key.ToString().Replace(":", "").Replace("@", "").ToUpper();
                if (kStr == "HP" || kStr == "0") dict[key] = newStats[0];
                else if (kStr == "ATTACK" || kStr == "ATK" || kStr == "1") dict[key] = newStats[1];
                else if (kStr == "DEFENSE" || kStr == "DEF" || kStr == "2") dict[key] = newStats[2];
                else if (kStr == "SPEED" || kStr == "SPD" || kStr == "3") dict[key] = newStats[5];
                else if (kStr.Contains("SP") && kStr.Contains("ATK")) dict[key] = newStats[3];
                else if (kStr.Contains("SP") && kStr.Contains("DEF")) dict[key] = newStats[4];
            }
        }
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
        else if (node is List<object> list)
        {
            foreach (var item in list)
            {
                var found = FindObjectWithAttribute(item, attrName, visited);
                if (found != null) return found;
            }
        }
        return null;
    }
}