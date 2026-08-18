using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SaveWriter
{
    private readonly string _savePath;
    private readonly object _rootRubyData;
    private readonly PBSReader _pbs; // NUEVO

    public SaveWriter(string savePath, object rootRubyData, PBSReader pbs = null) 
    { 
        _savePath = savePath; 
        _rootRubyData = rootRubyData;
        _pbs = pbs;
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

    public object CloneRubyObject(object source) 
    {
        if (source == null) return null;
        using(var ms = new MemoryStream()) {
            var writer = new RubyMarshalWriter(ms);
            writer.WriteHeader();
            writer.WriteValue(source);
            ms.Position = 0;
            var reader = new RubyMarshalReader(ms);
            return reader.ReadValue();
        }
    }

    public bool AddPokemon(bool isParty, int boxIndex, string internalSpecies, string displayName, int level, int exp, string defaultAbility)
    {
        IList list = GetRubyList(isParty, boxIndex);
        if (list == null) return false;

        RubyObject templatePoke = FindObjectByClass(_rootRubyData, "PokeBattle_Pokemon") ?? FindObjectWithAttribute(_rootRubyData, "@species");
        if (templatePoke == null) return false;

        RubyObject newPoke = (RubyObject)CloneRubyObject(templatePoke);
        
        newPoke.Set("@species", new RubySymbol(internalSpecies.ToUpper()));
        
        newPoke.Set("@name", null);
        
        newPoke.Set("@level", level);
        newPoke.Set("@exp", exp); 
        newPoke.Set("@shiny", false);
        if (newPoke.Attributes.ContainsKey("@superShiny")) newPoke.Set("@superShiny", false);
        else newPoke.Set("@super_shiny", false);
        
        // LIMPIEZA PROFUNDA DEL POKÉMON MOLDE
        newPoke.Attributes.Remove("@calc_level");
        newPoke.Attributes.Remove("@calc_stats");
        newPoke.Attributes.Remove("@nature_for_stats");
        newPoke.Attributes.Remove("@calc_nature");
        
        // FIX CRÍTICO: El juego necesita un HP base para no crashear la barra de vida en el PC
        newPoke.Set("@hp", 10);
        newPoke.Set("@totalhp", 10);
        
        UpdateStatsAttribute(newPoke, "@iv", "@ivs", new int[] { 0, 0, 0, 0, 0, 0 });
        UpdateStatsAttribute(newPoke, "@ev", "@evs", new int[] { 0, 0, 0, 0, 0, 0 });
        
        if (string.IsNullOrEmpty(defaultAbility) || defaultAbility.StartsWith("AUTO")) 
        {
            newPoke.Set("@ability", null);
            if (newPoke.Attributes.ContainsKey("@ability_id")) newPoke.Set("@ability_id", null);
            
            int index = 0; 
            if (defaultAbility == "AUTO_1") index = 1;
            else if (defaultAbility == "AUTO_2") index = 2; 

            newPoke.Set("@ability_index", index); 
            if (index == 2) newPoke.Set("@hiddenAbility", true);
            else newPoke.Attributes.Remove("@hiddenAbility");
        }
        else 
        {
            RubySymbol defaultAbilitySym = new RubySymbol(defaultAbility.ToUpper());
            newPoke.Set("@ability", defaultAbilitySym);
            if (newPoke.Attributes.ContainsKey("@ability_id")) newPoke.Set("@ability_id", defaultAbilitySym);
            newPoke.Attributes.Remove("@ability_index");
            newPoke.Attributes.Remove("@abilityNum");
            newPoke.Attributes.Remove("@hiddenAbility");
        }

        object templateNat = templatePoke.Get("@nature") ?? templatePoke.Get("@initial_nature");
        if (templateNat is RubySymbol) {
            newPoke.Set("@nature", new RubySymbol("HARDY"));
            if (newPoke.Attributes.ContainsKey("@initial_nature")) newPoke.Set("@initial_nature", new RubySymbol("HARDY"));
        } else {
            newPoke.Set("@nature", 0); 
            if (newPoke.Attributes.ContainsKey("@initial_nature")) newPoke.Set("@initial_nature", 0);
        }

        newPoke.Set("@happiness", 70);
        newPoke.Set("@form", 0);
        newPoke.Set("@item", null); 

        byte[] buf = new byte[4];
        new Random().NextBytes(buf);
        long newPid = BitConverter.ToUInt32(buf, 0);
        if (newPoke.Attributes.ContainsKey("@personalID")) newPoke.Set("@personalID", newPid);
        else if (newPoke.Attributes.ContainsKey("@personal_id")) newPoke.Set("@personal_id", newPid);
        else newPoke.Set("@pid", newPid);

        var movesList = Unwrap(newPoke.Get("@moves")) as IList;
        if (movesList != null && movesList.Count > 0)
        {
            object firstMoveTemplate = CloneRubyObject(movesList[0]); 
            movesList.Clear();
            if (Unwrap(firstMoveTemplate) is RubyObject mRo)
            {
                mRo.Set("@id", new RubySymbol("TACKLE")); 
                mRo.Set("@pp", 35);
                mRo.Set("@ppup", 0);
            }
            movesList.Add(firstMoveTemplate);
        }

        if (isParty)
        {
            if (list.Count < 6) list.Add(newPoke);
            else return false;
        }
        else
        {
            int emptySlot = -1;
            for (int i = 0; i < list.Count && i < 30; i++)
            {
                if (Unwrap(list[i]) == null) { emptySlot = i; break; }
            }
            if (emptySlot != -1) list[emptySlot] = newPoke;
            else if (list.Count < 30) list.Add(newPoke);
            else return false;
        }

        UnlockPokedex(internalSpecies);

        return true;
    }

    public void ClonePokemonInRuby(bool fromParty, int fromBox, int fromSlot, bool toParty, int toBox, int toSlot) 
    {
        IList fromList = GetRubyList(fromParty, fromBox);
        IList toList = GetRubyList(toParty, toBox);
        if (fromList != null && toList != null && fromSlot < fromList.Count) 
        {
            object clone = CloneRubyObject(fromList[fromSlot]);
            if (toParty) toList.Add(clone); 
            else toList[toSlot] = clone;
        }
    }

    public void DeletePokemonInRuby(bool isParty, int boxIndex, int slotIndex) 
    {
        IList list = GetRubyList(isParty, boxIndex);
        if (list != null && slotIndex >= 0 && slotIndex < list.Count) 
        {
            if (isParty) list.RemoveAt(slotIndex);
            else list[slotIndex] = null; 
        }
    }

    public long GetMoney()
    {
        RubyObject trainerObj = FindObjectByClass(_rootRubyData, "PokeBattle_Trainer") ?? FindObjectByClass(_rootRubyData, "Player");
        if (trainerObj != null && trainerObj.Attributes.ContainsKey("@money"))
        {
            object moneyObj = Unwrap(trainerObj.Get("@money"));
            if (moneyObj != null) return Convert.ToInt64(moneyObj); // Usamos ToInt64 para billones
        }
        return 0;
    }

    public void SyncMoney(long money)
    {
        RubyObject trainerObj = FindObjectByClass(_rootRubyData, "PokeBattle_Trainer") ?? FindObjectByClass(_rootRubyData, "Player");
        if (trainerObj != null)
        {
            trainerObj.Set("@money", money);
        }
    }

    public void SyncBag(List<ItemSlot> bag) 
    {
        RubyObject bagRo = FindObjectByClass(_rootRubyData, "PokemonBag");
        if (bagRo != null && Unwrap(bagRo.Get("@pockets")) is IList pockets)
        {
            for (int i = 1; i < pockets.Count; i++) (Unwrap(pockets[i]) as IList)?.Clear();
            
            foreach (var item in bag)
            {
                if (item.Pocket >= 1 && item.Pocket < pockets.Count)
                {
                    if (Unwrap(pockets[item.Pocket]) is IList pocket)
                        pocket.Add(new List<object> { new RubySymbol(item.InternalName), item.Quantity });
                }
            }
        }
    }

    private int GetNatureId(string nat) {
        switch(nat?.ToUpper()) {
            case "HARDY": return 0; case "LONELY": return 1; case "BRAVE": return 2; case "ADAMANT": return 3; case "NAUGHTY": return 4;
            case "BOLD": return 5; case "DOCILE": return 6; case "RELAXED": return 7; case "IMPISH": return 8; case "LAX": return 9;
            case "TIMID": return 10; case "HASTY": return 11; case "SERIOUS": return 12; case "JOLLY": return 13; case "NAIVE": return 14;
            case "MODEST": return 15; case "MILD": return 16; case "QUIET": return 17; case "BASHFUL": return 18; case "RASH": return 19;
            case "CALM": return 20; case "GENTLE": return 21; case "SASSY": return 22; case "CAREFUL": return 23; case "QUIRKY": return 24;
            default: return 0; 
        }
    }

    public void SyncPokemon(Pokemon p, bool isParty, int boxIndex, int slotIndex)
    {
        RubyObject pokeRo = GetPokemonRubyObject(isParty, boxIndex, slotIndex);
        if (pokeRo == null || p == null) return;

        if (string.IsNullOrWhiteSpace(p.Nickname) || 
            p.Nickname.Equals(p.Species, StringComparison.OrdinalIgnoreCase) || 
            p.Nickname.Equals(p.InternalSpecies, StringComparison.OrdinalIgnoreCase)) 
        {
            pokeRo.Set("@name", null);
        } 
        else 
        {
            var nameWrapper = new RubyWrapper(p.Nickname);
            nameWrapper.InstanceVariables[new RubySymbol("E")] = true; 
            pokeRo.Set("@name", nameWrapper);
        }
        
        pokeRo.Set("@form", p.Form);
        pokeRo.Set("@species", new RubySymbol(p.InternalSpecies.ToUpper())); 

        pokeRo.Set("@level", p.Level);
        pokeRo.Set("@exp", p.Exp);
        pokeRo.Set("@happiness", p.Happiness);
        
        // LIMPIEZA DE CORRUPCIÓN POR POKÉMON
        pokeRo.Attributes.Remove("@calc_level");
        pokeRo.Attributes.Remove("@nature_for_stats");
        pokeRo.Attributes.Remove("@calc_nature");
        pokeRo.Attributes.Remove("@calc_stats");
        
        int genderVal = 0; 
        if (!string.IsNullOrEmpty(p.Gender)) {
            if (p.Gender.Contains("Hembra")) genderVal = 1;
            else if (p.Gender.Contains("Sin Género")) genderVal = 2;
        }
        pokeRo.Set("@gender", genderVal);

        pokeRo.Set("@shiny", p.IsShiny);
        
        if (pokeRo.Attributes.ContainsKey("@superShiny")) pokeRo.Set("@superShiny", p.IsSuperShiny);
        else pokeRo.Set("@super_shiny", p.IsSuperShiny);

        if (string.IsNullOrWhiteSpace(p.InternalHeldItem) || p.InternalHeldItem.Equals("Ninguno", StringComparison.OrdinalIgnoreCase))
            pokeRo.Set("@item", null); 
        else 
            pokeRo.Set("@item", new RubySymbol(p.InternalHeldItem.ToUpper())); 

        if (!string.IsNullOrEmpty(p.InternalNature)) {
            object oldNat = pokeRo.Get("@nature") ?? pokeRo.Get("@initial_nature");
            if (oldNat is RubySymbol) {
                pokeRo.Set("@nature", new RubySymbol(p.InternalNature.ToUpper()));
            } else {
                pokeRo.Set("@nature", GetNatureId(p.InternalNature)); 
            }
        }

        if (string.IsNullOrEmpty(p.InternalAbility) || p.InternalAbility.StartsWith("AUTO")) 
        {
            pokeRo.Set("@ability", null);
            if (pokeRo.Attributes.ContainsKey("@ability_id")) pokeRo.Set("@ability_id", null);
            
            int index = 0; 
            if (p.InternalAbility == "AUTO_1") index = 1; 
            else if (p.InternalAbility == "AUTO_2") index = 2; 

            pokeRo.Set("@ability_index", index); 
            if (index == 2) pokeRo.Set("@hiddenAbility", true);
            else pokeRo.Attributes.Remove("@hiddenAbility");
        }
        else 
        {
            RubySymbol abSym = new RubySymbol(p.InternalAbility.ToUpper());
            pokeRo.Set("@ability", abSym);
            if (pokeRo.Attributes.ContainsKey("@ability_id")) pokeRo.Set("@ability_id", abSym);
            
            pokeRo.Attributes.Remove("@ability_index");
            pokeRo.Attributes.Remove("@abilityNum");
            pokeRo.Attributes.Remove("@hiddenAbility");
        }

        UpdateStatsAttribute(pokeRo, "@iv", "@ivs", p.IVs);
        UpdateStatsAttribute(pokeRo, "@ev", "@evs", p.EVs);

        var movesList = Unwrap(pokeRo.Get("@moves")) as IList;
        if (movesList != null && movesList.Count > 0)
        {
            object templateMove = CloneRubyObject(movesList[0]); 
            
            while (movesList.Count > p.Moves.Count) movesList.RemoveAt(movesList.Count - 1);
            while (movesList.Count < p.Moves.Count) movesList.Add(CloneRubyObject(templateMove));

            for (int i = 0; i < p.Moves.Count; i++)
            {
                if (Unwrap(movesList[i]) is RubyObject mRo)
                {
                    mRo.Set("@id", new RubySymbol(p.Moves[i].InternalName.ToUpper()));
                    mRo.Set("@pp", p.Moves[i].PP);
                    mRo.Set("@ppup", p.Moves[i].PPUp);
                }
            }
        }

        UnlockPokedex(p.InternalSpecies);
    }

    public void SwapPokemonInRuby(bool isParty1, int box1, int slot1, bool isParty2, int box2, int slot2)
    {
        IList list1 = GetRubyList(isParty1, box1);
        IList list2 = GetRubyList(isParty2, box2);
        if (list1 == null || list2 == null) return;
        if (slot1 < 0 || slot1 >= list1.Count || slot2 < 0 || slot2 >= list2.Count) return;

        object temp = list1[slot1];
        list1[slot1] = list2[slot2];
        list2[slot2] = temp;
    }

    private void UnlockPokedex(string internalSpecies)
    {
        if (string.IsNullOrWhiteSpace(internalSpecies)) return;
        
        RubyObject pokedex = FindObjectByClass(_rootRubyData, "PlayerPokedex") 
                          ?? FindObjectWithAttribute(_rootRubyData, "@owned");
        
        if (pokedex == null) return;

        if (_pbs != null) {
            string current = internalSpecies.ToUpper();
            // Buscar al ancestro más antiguo de la familia
            while (_pbs.PreEvolutions.ContainsKey(current)) {
                current = _pbs.PreEvolutions[current];
            }
            UnlockSpeciesRecursive(current, pokedex);
        } else {
            UnlockSingleSpecies(internalSpecies.ToUpper(), pokedex);
        }
    }

    private void UnlockSpeciesRecursive(string species, RubyObject pokedex, HashSet<string> visited = null) {
        visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (visited.Contains(species)) return;
        visited.Add(species);

        UnlockSingleSpecies(species, pokedex);
        if (_pbs.Evolutions.TryGetValue(species, out var evos)) {
            foreach(var evo in evos) UnlockSpeciesRecursive(evo, pokedex, visited);
        }
    }

    private void UnlockSingleSpecies(string species, RubyObject pokedex) {
        RubySymbol spcSym = new RubySymbol(species);

        var owned = Unwrap(pokedex.Get("@owned"));
        if (owned is IDictionary dictOwned) dictOwned[spcSym] = true;
        else if (owned is IList listOwned && !listOwned.Contains(spcSym)) listOwned.Add(spcSym);

        var seen = Unwrap(pokedex.Get("@seen"));
        if (seen is IDictionary dictSeen) dictSeen[spcSym] = true;
        else if (seen is IList listSeen && !listSeen.Contains(spcSym)) listSeen.Add(spcSym);
    }

    private IList GetRubyList(bool isParty, int boxIndex)
    {
        if (isParty)
        {
            RubyObject trainerObj = FindObjectByClass(_rootRubyData, "PokeBattle_Trainer") ?? FindObjectByClass(_rootRubyData, "Player") ?? FindObjectWithAttribute(_rootRubyData, "@party");
            return trainerObj != null ? Unwrap(trainerObj.Get("@party")) as IList : null;
        }
        else
        {
            RubyObject storageObj = FindObjectByClass(_rootRubyData, "PokemonStorage") ?? FindObjectWithAttribute(_rootRubyData, "@boxes");
            if (storageObj != null && Unwrap(storageObj.Get("@boxes")) is IList boxesList && boxIndex >= 0 && boxIndex < boxesList.Count)
            {
                if (Unwrap(boxesList[boxIndex]) is RubyObject boxObj)
                {
                    return Unwrap(boxObj.Get("@pokemon") ?? boxObj.Get("@pokemons")) as IList;
                }
            }
        }
        return null;
    }

    private RubyObject GetPokemonRubyObject(bool isParty, int boxIndex, int slotIndex)
    {
        IList list = GetRubyList(isParty, boxIndex);
        if (list != null && slotIndex >= 0 && slotIndex < list.Count)
        {
            return Unwrap(list[slotIndex]) as RubyObject;
        }
        return null;
    }

    private void UpdateStatsAttribute(RubyObject pokeRo, string attr1, string attr2, int[] newStats)
    {
        string targetAttr = pokeRo.Attributes.ContainsKey(attr1) ? attr1 : attr2;
        var currentData = Unwrap(pokeRo.Get(targetAttr));

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
                string kStr = Unwrap(key).ToString().Replace(":", "").Replace("@", "").Replace("_", "").ToUpper();
                
                if (kStr == "HP" || kStr == "0") 
                    dict[key] = newStats[0];
                else if (kStr == "SPEED" || kStr == "SPD" || kStr == "3") 
                    dict[key] = newStats[5];
                else if ((kStr.Contains("SPECIAL") && kStr.Contains("ATTACK")) || kStr.Contains("SPATK") || kStr == "4") 
                    dict[key] = newStats[3];
                else if ((kStr.Contains("SPECIAL") && kStr.Contains("DEFENSE")) || kStr.Contains("SPDEF") || kStr == "5") 
                    dict[key] = newStats[4];
                else if ((kStr == "ATTACK" || kStr == "ATK" || kStr == "1") && !kStr.Contains("SPECIAL") && !kStr.Contains("SP")) 
                    dict[key] = newStats[1];
                else if ((kStr == "DEFENSE" || kStr == "DEF" || kStr == "2") && !kStr.Contains("SPECIAL") && !kStr.Contains("SP")) 
                    dict[key] = newStats[2];
            }
        }
    }

    private object Unwrap(object obj)
    {
        if (obj is RubyWrapper rw) return rw.WrappedObject;
        return obj;
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