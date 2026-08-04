using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class SaveParser
{
    private string _savePath;
    private PBSReader _pbs;

    private static readonly string[] StandardNatures = new string[]
    {
        "HARDY", "LONELY", "BRAVE", "ADAMANT", "NAUGHTY",
        "BOLD", "DOCILE", "RELAXED", "IMPISH", "LAX",
        "TIMID", "HASTY", "SERIOUS", "JOLLY", "NAIVE",
        "MODEST", "MILD", "QUIET", "BASHFUL", "RASH",
        "CALM", "GENTLE", "SASSY", "CAREFUL", "QUIRKY"
    };

    public SaveParser(string path, PBSReader pbs = null)
    {
        _savePath = path;
        _pbs = pbs;
    }

    public SaveDataModel ParseSave()
    {
        SaveDataModel model = new SaveDataModel();
        byte[] fileBytes = File.ReadAllBytes(_savePath);

        if (fileBytes.Length < 2)
            throw new Exception("El archivo de guardado está vacío o corrupto.");

        Stream decompressedStream = null;
        MemoryStream memStream = null;

        try
        {
            if (fileBytes[0] == 0x04 && fileBytes[1] == 0x08)
            {
                decompressedStream = new MemoryStream(fileBytes);
            }
            else
            {
                bool success = false;
                try
                {
                    using (var sourceStream = new MemoryStream(fileBytes))
                    using (var zlib = new ZLibStream(sourceStream, CompressionMode.Decompress))
                    {
                        memStream = new MemoryStream();
                        zlib.CopyTo(memStream);
                        memStream.Position = 0;
                        decompressedStream = memStream;
                        success = true;
                    }
                }
                catch { }

                if (!success)
                {
                    try
                    {
                        using (var sourceStream = new MemoryStream(fileBytes, 2, fileBytes.Length - 2))
                        using (var deflate = new DeflateStream(sourceStream, CompressionMode.Decompress))
                        {
                            memStream = new MemoryStream();
                            deflate.CopyTo(memStream);
                            memStream.Position = 0;
                            decompressedStream = memStream;
                            success = true;
                        }
                    }
                    catch { }
                }

                if (!success)
                {
                    using (var sourceStream = new MemoryStream(fileBytes))
                    using (var deflate = new DeflateStream(sourceStream, CompressionMode.Decompress))
                    {
                        memStream = new MemoryStream();
                        deflate.CopyTo(memStream);
                        memStream.Position = 0;
                        decompressedStream = memStream;
                    }
                }
            }

            using (decompressedStream)
            {
                RubyMarshalReader reader = new RubyMarshalReader(decompressedStream);
                object rootData = reader.ReadValue();

                // ASIGNACIÓN CRÍTICA PARA EL SAVEWRITER
                model.RootData = rootData;

                RubyObject trainerObj = FindObjectByClass(rootData, "PokeBattle_Trainer") 
                                       ?? FindObjectByClass(rootData, "Player")
                                       ?? FindObjectWithAttribute(rootData, "@party");

                if (trainerObj != null && trainerObj.Get("@party") is IList partyList)
                {
                    foreach (var pokeObj in partyList)
                    {
                        if (pokeObj is RubyObject ro)
                            model.Party.Add(MapRubyToPokemon(ro));
                    }
                }

                RubyObject storageObj = FindObjectByClass(rootData, "PokemonStorage") 
                                      ?? FindObjectWithAttribute(rootData, "@boxes");

                if (storageObj != null && storageObj.Get("@boxes") is IList boxesList)
                {
                    for (int b = 0; b < boxesList.Count; b++)
                    {
                        if (boxesList[b] is RubyObject boxRo)
                        {
                            PCBox box = new PCBox
                            {
                                BoxIndex = b + 1,
                                Name = boxRo.Get("@name")?.ToString() ?? $"Caja {b + 1}"
                            };

                            var pokesInBox = boxRo.Get("@pokemon") ?? boxRo.Get("@pokemons");
                            if (pokesInBox is IList slotList)
                            {
                                for (int s = 0; s < slotList.Count && s < 30; s++)
                                {
                                    if (slotList[s] is RubyObject pokeRo)
                                    {
                                        box.Slots[s] = MapRubyToPokemon(pokeRo);
                                    }
                                }
                            }

                            model.Boxes.Add(box);
                        }
                    }
                }
            }
        }
        finally
        {
            if (decompressedStream != memStream)
            {
                memStream?.Dispose();
            }
        }

        return model;
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
        return null;
    }

    private Pokemon MapRubyToPokemon(RubyObject ro)
    {
        Pokemon p = new Pokemon();

        string speciesRaw = ro.Get("@species")?.ToString() ?? "Desconocido";
        p.Species = _pbs != null ? _pbs.GetName(_pbs.Species, speciesRaw, speciesRaw) : speciesRaw;
        
        p.Nickname = ro.Get("@name")?.ToString() ?? p.Species;
        p.Level = Convert.ToInt32(ro.Get("@level") ?? 1);

        int genderByte = Convert.ToInt32(ro.Get("@gender") ?? 0);
        p.Gender = genderByte == 0 ? "Macho ♂" : (genderByte == 1 ? "Hembra ♀" : "Sin Género ⚲");

        p.IsShiny = Convert.ToBoolean(ro.Get("@shiny") ?? false);
        p.Form = Convert.ToInt32(ro.Get("@form") ?? 0);
        
        string abilityRaw = ro.Get("@ability")?.ToString() ?? "Desconocida";
        p.Ability = _pbs != null ? _pbs.GetName(_pbs.Abilities, abilityRaw, abilityRaw) : abilityRaw;
        
        p.Nature = GetPokemonNature(ro);

        string itemRaw = ro.Get("@item")?.ToString() ?? "Ninguno";
        p.HeldItem = _pbs != null ? _pbs.GetName(_pbs.Items, itemRaw, itemRaw) : itemRaw;
        
        p.Happiness = Convert.ToInt32(ro.Get("@happiness") ?? 0);
        p.PokeBall = ro.Get("@poke_ball")?.ToString() ?? "Pokéball";

        ExtractStatsData(ro.Get("@iv") ?? ro.Get("@ivs"), p.IVs);
        ExtractStatsData(ro.Get("@ev") ?? ro.Get("@evs"), p.EVs);

        if (ro.Get("@moves") is IList moveList)
        {
            foreach (var mObj in moveList)
            {
                if (mObj is RubyObject mRo)
                {
                    string moveRaw = mRo.Get("@id")?.ToString() ?? "Vacío";
                    p.Moves.Add(new PokemonMove
                    {
                        Name = _pbs != null ? _pbs.GetName(_pbs.Moves, moveRaw, moveRaw) : moveRaw,
                        PP = Convert.ToInt32(mRo.Get("@pp") ?? 0),
                        PPUp = Convert.ToInt32(mRo.Get("@ppup") ?? 0)
                    });
                }
            }
        }

        p.ObtainLevel = Convert.ToInt32(ro.Get("@obtain_level") ?? 0);
        p.ObtainMap = ro.Get("@obtain_map")?.ToString() ?? "Desconocido";

        return p;
    }

    private string GetPokemonNature(RubyObject ro)
    {
        object rawNature = ro.Get("@nature") ?? ro.Get("@initial_nature");
        if (rawNature != null)
        {
            string strNat = rawNature.ToString().Replace(":", "").Replace("@", "").Trim();
            
            if (_pbs != null && !int.TryParse(strNat, out _))
            {
                return _pbs.GetName(_pbs.Natures, strNat, strNat);
            }

            if (int.TryParse(strNat, out int idx) && idx >= 0 && idx < StandardNatures.Length)
                return StandardNatures[idx];

            if (!string.IsNullOrWhiteSpace(strNat))
                return strNat;
        }

        object rawPid = ro.Get("@personalID") ?? ro.Get("@personal_id") ?? ro.Get("@pid");
        if (rawPid != null)
        {
            long pid = Convert.ToInt64(rawPid);
            int natureIdx = (int)(Math.Abs(pid) % 25);
            return StandardNatures[natureIdx];
        }

        return "HARDY";
    }

    private void ExtractStatsData(object rawData, int[] targetArray)
    {
        if (rawData == null) return;

        if (rawData is IList list && list.Count >= 6)
        {
            targetArray[0] = Convert.ToInt32(list[0] ?? 0);
            targetArray[1] = Convert.ToInt32(list[1] ?? 0);
            targetArray[2] = Convert.ToInt32(list[2] ?? 0);
            targetArray[3] = Convert.ToInt32(list[4] ?? 0);
            targetArray[4] = Convert.ToInt32(list[5] ?? 0);
            targetArray[5] = Convert.ToInt32(list[3] ?? 0);
        }
        else if (rawData is IDictionary dict)
        {
            foreach (DictionaryEntry kvp in dict)
            {
                string rawKey = kvp.Key?.ToString()?.ToUpper()?.Replace(":", "")?.Replace("@", "")?.Trim() ?? "";
                int val = Convert.ToInt32(kvp.Value ?? 0);

                if (rawKey == "HP" || rawKey == "0") targetArray[0] = val;
                else if (rawKey == "ATTACK" || rawKey == "ATK" || rawKey == "1") targetArray[1] = val;
                else if (rawKey == "DEFENSE" || rawKey == "DEF" || rawKey == "2") targetArray[2] = val;
                else if (rawKey == "SPEED" || rawKey == "SPD" || rawKey == "SPE" || rawKey == "3") targetArray[5] = val;
                else if (rawKey == "SPECIAL_ATTACK" || rawKey == "SPATK" || rawKey == "SPECIALATTACK" || rawKey == "SP_ATTACK" || rawKey == "4") targetArray[3] = val;
                else if (rawKey == "SPECIAL_DEFENSE" || rawKey == "SPDEF" || rawKey == "SPECIALDEFENSE" || rawKey == "SP_DEFENSE" || rawKey == "5") targetArray[4] = val;
            }
        }
    }
}