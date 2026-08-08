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

    public SaveDataModel ParseSave(object existingRootData = null)
    {
        SaveDataModel model = new SaveDataModel();
        object rootData = existingRootData;

        // Si no le pasamos datos en memoria, lee el archivo de guardado físico
        if (rootData == null)
        {
            byte[] fileBytes = File.ReadAllBytes(_savePath);
            if (fileBytes.Length < 2) throw new Exception("El archivo de guardado está vacío o corrupto.");

            Stream decompressedStream = null;
            MemoryStream memStream = null;

            try
            {
                if (fileBytes[0] == 0x04 && fileBytes[1] == 0x08) { decompressedStream = new MemoryStream(fileBytes); }
                else
                {
                    bool success = false;
                    try {
                        using (var sourceStream = new MemoryStream(fileBytes))
                        using (var zlib = new ZLibStream(sourceStream, CompressionMode.Decompress)) {
                            memStream = new MemoryStream(); zlib.CopyTo(memStream); memStream.Position = 0;
                            decompressedStream = memStream; success = true;
                        }
                    } catch { }
                    if (!success) {
                        try {
                            using (var sourceStream = new MemoryStream(fileBytes, 2, fileBytes.Length - 2))
                            using (var deflate = new DeflateStream(sourceStream, CompressionMode.Decompress)) {
                                memStream = new MemoryStream(); deflate.CopyTo(memStream); memStream.Position = 0;
                                decompressedStream = memStream; success = true;
                            }
                        } catch { }
                    }
                    if (!success) {
                        using (var sourceStream = new MemoryStream(fileBytes))
                        using (var deflate = new DeflateStream(sourceStream, CompressionMode.Decompress)) {
                            memStream = new MemoryStream(); deflate.CopyTo(memStream); memStream.Position = 0;
                            decompressedStream = memStream;
                        }
                    }
                }

                using (decompressedStream)
                {
                    RubyMarshalReader reader = new RubyMarshalReader(decompressedStream);
                    rootData = reader.ReadValue();
                }
            }
            finally
            {
                if (decompressedStream != memStream) memStream?.Dispose();
            }
        }

        model.RootData = rootData;

        // --- EXTRACCIÓN DE EQUIPO ---
        RubyObject trainerObj = FindObjectByClass(rootData, "PokeBattle_Trainer") ?? FindObjectByClass(rootData, "Player") ?? FindObjectWithAttribute(rootData, "@party");
        if (trainerObj != null && Unwrap(trainerObj.Get("@party")) is IList partyList)
        {
            foreach (var pokeObj in partyList)
            {
                if (Unwrap(pokeObj) is RubyObject ro) model.Party.Add(MapRubyToPokemon(ro));
            }
        }

        // --- EXTRACCIÓN DE CAJAS ---
        RubyObject storageObj = FindObjectByClass(rootData, "PokemonStorage") ?? FindObjectWithAttribute(rootData, "@boxes");
        if (storageObj != null && Unwrap(storageObj.Get("@boxes")) is IList boxesList)
        {
            for (int b = 0; b < boxesList.Count; b++)
            {
                if (Unwrap(boxesList[b]) is RubyObject boxRo)
                {
                    PCBox box = new PCBox { BoxIndex = b + 1, Name = Unwrap(boxRo.Get("@name"))?.ToString() ?? $"Caja {b + 1}" };
                    var pokesInBox = Unwrap(boxRo.Get("@pokemon") ?? boxRo.Get("@pokemons"));
                    if (pokesInBox is IList slotList)
                    {
                        for (int s = 0; s < slotList.Count && s < 30; s++)
                        {
                            if (Unwrap(slotList[s]) is RubyObject pokeRo) box.Slots[s] = MapRubyToPokemon(pokeRo);
                        }
                    }
                    model.Boxes.Add(box);
                }
            }
        }

        // --- EXTRACCIÓN DE MOCHILA ---
        RubyObject bagRo = FindObjectByClass(rootData, "PokemonBag");
        if (bagRo != null && Unwrap(bagRo.Get("@pockets")) is IList pockets)
        {
            for (int i = 1; i < pockets.Count; i++) // El bolsillo 0 siempre está vacío
            {
                if (Unwrap(pockets[i]) is IList pocket)
                {
                    foreach (var itemObj in pocket)
                    {
                        if (Unwrap(itemObj) is IList itemPair && itemPair.Count == 2)
                        {
                            string intName = Unwrap(itemPair[0]).ToString().Replace(":", "").Replace("@", "").Trim();
                            int qty = SafeGetInt(Unwrap(itemPair[1]));
                            model.Bag.Add(new ItemSlot { 
                                InternalName = intName, 
                                Name = _pbs != null ? _pbs.GetName(_pbs.Items, intName, intName) : intName, 
                                Quantity = qty, 
                                Pocket = i 
                            });
                        }
                    }
                }
            }
        }

        return model;
    }

    private Pokemon MapRubyToPokemon(RubyObject ro)
    {
        Pokemon p = new Pokemon();

        string speciesRaw = Unwrap(ro.Get("@species"))?.ToString() ?? "Desconocido";
        p.InternalSpecies = speciesRaw;
        p.Species = _pbs != null ? _pbs.GetName(_pbs.Species, speciesRaw, speciesRaw) : speciesRaw;
        
        p.Nickname = Unwrap(ro.Get("@name"))?.ToString() ?? p.Species;
        p.Level = SafeGetInt(Unwrap(ro.Get("@level")), 1);

        int genderByte = SafeGetInt(Unwrap(ro.Get("@gender")), 0);
        p.Gender = genderByte == 0 ? "Macho ♂" : (genderByte == 1 ? "Hembra ♀" : "Sin Género ⚲");

        p.IsShiny = Convert.ToBoolean(Unwrap(ro.Get("@shiny")) ?? false);
        p.IsSuperShiny = Convert.ToBoolean(Unwrap(ro.Get("@super_shiny") ?? ro.Get("@superShiny")) ?? false);
        p.Form = SafeGetInt(Unwrap(ro.Get("@form")), 0);
        
        p.PersonalID = SafeGetLong(Unwrap(ro.Get("@personalID") ?? ro.Get("@personal_id") ?? ro.Get("@pid")));
        p.Exp = SafeGetInt(Unwrap(ro.Get("@exp")));
        p.IsEgg = SafeGetInt(Unwrap(ro.Get("@steps_to_hatch"))) > 0;
        p.ObtainText = Unwrap(ro.Get("@obtain_text"))?.ToString();

        string abilityRaw = Unwrap(ro.Get("@ability"))?.ToString() ?? "Desconocida";
        p.InternalAbility = abilityRaw;
        p.Ability = _pbs != null ? _pbs.GetName(_pbs.Abilities, abilityRaw, abilityRaw) : abilityRaw;
        
        p.Nature = GetPokemonNature(ro);
        p.InternalNature = Unwrap(ro.Get("@nature") ?? ro.Get("@initial_nature"))?.ToString()?.Replace(":", "")?.Replace("@", "")?.Trim();

        string itemRaw = Unwrap(ro.Get("@item"))?.ToString() ?? "Ninguno";
        p.InternalHeldItem = itemRaw;
        p.HeldItem = _pbs != null ? _pbs.GetName(_pbs.Items, itemRaw, itemRaw) : itemRaw;
        
        p.Happiness = SafeGetInt(Unwrap(ro.Get("@happiness")));
        
        string ballRaw = Unwrap(ro.Get("@poke_ball"))?.ToString() ?? "POKEBALL";
        p.InternalPokeBall = ballRaw;
        p.PokeBall = _pbs != null ? _pbs.GetName(_pbs.Items, ballRaw, "Pokéball") : ballRaw;

        ExtractStatsData(Unwrap(ro.Get("@iv") ?? ro.Get("@ivs")), p.IVs);
        ExtractStatsData(Unwrap(ro.Get("@ev") ?? ro.Get("@evs")), p.EVs);

        if (Unwrap(ro.Get("@moves")) is IList moveList)
        {
            foreach (var mObj in moveList)
            {
                if (Unwrap(mObj) is RubyObject mRo)
                {
                    string moveRaw = Unwrap(mRo.Get("@id"))?.ToString() ?? "Vacío";
                    p.Moves.Add(new PokemonMove
                    {
                        InternalName = moveRaw.Replace(":", "").Replace("@", "").Trim(),
                        Name = _pbs != null ? _pbs.GetName(_pbs.Moves, moveRaw, moveRaw) : moveRaw,
                        PP = SafeGetInt(Unwrap(mRo.Get("@pp"))),
                        PPUp = SafeGetInt(Unwrap(mRo.Get("@ppup")))
                    });
                }
            }
        }

        p.ObtainLevel = SafeGetInt(Unwrap(ro.Get("@obtain_level")));
        p.ObtainMap = Unwrap(ro.Get("@obtain_map"))?.ToString() ?? "Desconocido";

        return p;
    }

    private string GetPokemonNature(RubyObject ro)
    {
        object rawNature = Unwrap(ro.Get("@nature") ?? ro.Get("@initial_nature"));
        if (rawNature != null)
        {
            string strNat = rawNature.ToString().Replace(":", "").Replace("@", "").Trim();
            if (_pbs != null && !int.TryParse(strNat, out _)) return _pbs.GetName(_pbs.Natures, strNat, strNat);
            if (int.TryParse(strNat, out int idx) && idx >= 0 && idx < StandardNatures.Length) return StandardNatures[idx];
            if (!string.IsNullOrWhiteSpace(strNat)) return strNat;
        }

        object rawPid = Unwrap(ro.Get("@personalID") ?? ro.Get("@personal_id") ?? ro.Get("@pid"));
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
        rawData = Unwrap(rawData);
        if (rawData == null) return;

        if (rawData is IList list && list.Count >= 6)
        {
            targetArray[0] = SafeGetInt(list[0]);
            targetArray[1] = SafeGetInt(list[1]);
            targetArray[2] = SafeGetInt(list[2]);
            targetArray[3] = SafeGetInt(list[4]);
            targetArray[4] = SafeGetInt(list[5]);
            targetArray[5] = SafeGetInt(list[3]);
        }
        else if (rawData is IDictionary dict)
        {
            foreach (DictionaryEntry kvp in dict)
            {
                string rawKey = Unwrap(kvp.Key)?.ToString()?.ToUpper()?.Replace(":", "")?.Replace("@", "")?.Trim() ?? "";
                int val = SafeGetInt(Unwrap(kvp.Value));

                if (rawKey == "HP" || rawKey == "0") targetArray[0] = val;
                else if (rawKey == "ATTACK" || rawKey == "ATK" || rawKey == "1") targetArray[1] = val;
                else if (rawKey == "DEFENSE" || rawKey == "DEF" || rawKey == "2") targetArray[2] = val;
                else if (rawKey == "SPEED" || rawKey == "SPD" || rawKey == "SPE" || rawKey == "3") targetArray[5] = val;
                else if (rawKey == "SPECIAL_ATTACK" || rawKey == "SPATK" || rawKey == "SPECIALATTACK" || rawKey == "SP_ATTACK" || rawKey == "4") targetArray[3] = val;
                else if (rawKey == "SPECIAL_DEFENSE" || rawKey == "SPDEF" || rawKey == "SPECIALDEFENSE" || rawKey == "SP_DEFENSE" || rawKey == "5") targetArray[4] = val;
            }
        }
    }

    private object Unwrap(object obj)
    {
        if (obj is RubyWrapper rw) return rw.WrappedObject;
        return obj;
    }

    private int SafeGetInt(object obj, int fallback = 0)
    {
        if (obj == null) return fallback;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is System.Numerics.BigInteger bi) return (int)bi;
        try { return Convert.ToInt32(obj); } catch { return fallback; }
    }

    private long SafeGetLong(object obj, long fallback = 0)
    {
        if (obj == null) return fallback;
        if (obj is long l) return l;
        if (obj is int i) return i;
        if (obj is System.Numerics.BigInteger bi) return (long)bi;
        try { return Convert.ToInt64(obj); } catch { return fallback; }
    }

    private RubyObject FindObjectByClass(object node, string className, HashSet<object> visited = null)
    {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        node = Unwrap(node);

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

        node = Unwrap(node);

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