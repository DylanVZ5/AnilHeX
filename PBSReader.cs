using System;
using System.Collections.Generic;
using System.IO;

public class PBSReader
{
    public Dictionary<string, string> Abilities { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> AbilityDescriptions { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<string, string> Items { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ItemDescriptions { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ItemPockets { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ItemMoves { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> RandomizedItemMoves { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Moves { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MoveDescriptions { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> MovePPs { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<string, string> Species { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Natures { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
    public Dictionary<string, string> SpeciesGrowthRates { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
    // NUEVO: Diccionario para saber la habilidad por defecto del Pokémon
    public Dictionary<string, string> SpeciesAbilities { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 

    public Dictionary<string, List<string>> Evolutions { get; private set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> PreEvolutions { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded => Abilities.Count > 0 || Moves.Count > 0 || Items.Count > 0 || Species.Count > 0;

    public bool LoadPBSDirectory(string pbsFolderPath)
    {
        if (string.IsNullOrWhiteSpace(pbsFolderPath) || !Directory.Exists(pbsFolderPath)) return false;

        if (File.Exists(Path.Combine(pbsFolderPath, "abilities.txt"))) ParsePBSFile(Path.Combine(pbsFolderPath, "abilities.txt"), Abilities, AbilityDescriptions);
        if (File.Exists(Path.Combine(pbsFolderPath, "items.txt"))) ParsePBSItems(Path.Combine(pbsFolderPath, "items.txt")); 
        if (File.Exists(Path.Combine(pbsFolderPath, "moves.txt"))) ParsePBSFile(Path.Combine(pbsFolderPath, "moves.txt"), Moves, MoveDescriptions, MovePPs);
        
        // Usamos el parseador exclusivo para sacar los datos de los Pokémon
        if (File.Exists(Path.Combine(pbsFolderPath, "pokemon.txt"))) {
            ParsePBSPokemon(Path.Combine(pbsFolderPath, "pokemon.txt")); 
            
            // CONSTRUIR EL ÁRBOL DE PRE-EVOLUCIONES
            foreach (var kvp in Evolutions) {
                foreach (var evo in kvp.Value) {
                    PreEvolutions[evo] = kvp.Key;
                }
            }
        } 
        
        if (File.Exists(Path.Combine(pbsFolderPath, "natures.txt"))) ParsePBSFile(Path.Combine(pbsFolderPath, "natures.txt"), Natures);

        return IsLoaded;
    }

    private void ParsePBSItems(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        string currentId = null;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentId = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            if (currentId != null && line.Contains("="))
            {
                var parts = line.Split(new char[] { '=' }, 2);
                string key = parts[0].Trim();
                string val = parts[1].Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) Items[currentId] = val;
                else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase)) ItemDescriptions[currentId] = val;
                else if (key.Equals("Pocket", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(val, out int pkt)) ItemPockets[currentId] = pkt;
                }
                else if (key.Equals("Move", StringComparison.OrdinalIgnoreCase))
                {
                    ItemMoves[currentId] = val; 
                }
            }
        }
    }

    // NUEVO: Analizador exclusivo del archivo pokemon.txt
    private void ParsePBSPokemon(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        string currentId = null;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentId = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            if (currentId != null && line.Contains("="))
            {
                var parts = line.Split(new char[] { '=' }, 2);
                string key = parts[0].Trim();
                string val = parts[1].Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) Species[currentId] = val;
                else if (key.Equals("GrowthRate", StringComparison.OrdinalIgnoreCase)) SpeciesGrowthRates[currentId] = val;
                else if (key.Equals("Abilities", StringComparison.OrdinalIgnoreCase)) 
                {
                    // Si tiene varias habilidades separadas por coma, nos quedamos solo con la primera
                    SpeciesAbilities[currentId] = val.Split(',')[0].Trim(); 
                }
                else if (key.Equals("Evolutions", StringComparison.OrdinalIgnoreCase)) 
                {
                    string[] evData = val.Split(',');
                    List<string> evos = new List<string>();
                    for (int i = 0; i < evData.Length; i += 3) {
                        if (!string.IsNullOrWhiteSpace(evData[i])) evos.Add(evData[i].Trim().ToUpper());
                    }
                    Evolutions[currentId] = evos;
                }
            }
        }
    }

    private void ParsePBSFile(string filePath, Dictionary<string, string> names, Dictionary<string, string> descs = null, Dictionary<string, int> nums = null)
    {
        string[] lines = File.ReadAllLines(filePath);
        string currentId = null;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentId = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            if (currentId != null && line.Contains("="))
            {
                var parts = line.Split(new char[] { '=' }, 2);
                string key = parts[0].Trim();
                string val = parts[1].Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) names[currentId] = val;
                else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase) && descs != null) descs[currentId] = val;
                else if ((key.Equals("TotalPP", StringComparison.OrdinalIgnoreCase) || key.Equals("Pocket", StringComparison.OrdinalIgnoreCase)) && nums != null)
                {
                    if (int.TryParse(val, out int num)) nums[currentId] = num;
                }
            }
            else if (currentId == null && line.Contains(","))
            {
                var parts = line.Split(',');
                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[1].Trim())) names[parts[1].Trim()] = parts[2].Trim();
                else if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0].Trim())) names[parts[0].Trim()] = parts[1].Trim();
            }
        }
    }

    public string GetName(Dictionary<string, string> dict, string rawId, string fallback = "Ninguno")
    {
        if (string.IsNullOrWhiteSpace(rawId)) return fallback;
        string cleanId = rawId.Replace(":", "").Replace("@", "").Trim();

        if (dict == Items)
        {
            // 1. Declaramos las variables primero para que C# no se queje de que están sin asignar
            string rMove = null;
            string nMove = null;
            
            // 2. Ahora sí hacemos la comprobación
            bool hasRandomMove = RandomizedItemMoves != null && RandomizedItemMoves.TryGetValue(cleanId, out rMove);
            bool hasNormalMove = ItemMoves != null && ItemMoves.TryGetValue(cleanId, out nMove);
            
            if (hasRandomMove || hasNormalMove)
            {
                string moveInternal = hasRandomMove ? rMove : nMove;
                string baseItemName = dict.TryGetValue(cleanId, out string realName) ? realName : cleanId;
                string moveRealName = Moves.TryGetValue(moveInternal, out string mName) ? mName : moveInternal;
                return $"{baseItemName}: {moveRealName}"; 
            }
        }

        return dict.TryGetValue(cleanId, out string realNameFallback) ? realNameFallback : cleanId;
    }
}