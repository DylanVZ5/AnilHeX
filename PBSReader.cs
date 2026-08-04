using System;
using System.Collections.Generic;
using System.IO;

public class PBSReader
{
    public Dictionary<string, string> Abilities { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Items { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Moves { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Species { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Natures { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded => Abilities.Count > 0 || Moves.Count > 0 || Items.Count > 0 || Species.Count > 0 || Natures.Count > 0;

    public bool LoadPBSDirectory(string pbsFolderPath)
    {
        if (string.IsNullOrWhiteSpace(pbsFolderPath) || !Directory.Exists(pbsFolderPath)) 
            return false;

        string abilitiesPath = Path.Combine(pbsFolderPath, "abilities.txt");
        string itemsPath = Path.Combine(pbsFolderPath, "items.txt");
        string movesPath = Path.Combine(pbsFolderPath, "moves.txt");
        string pokemonPath = Path.Combine(pbsFolderPath, "pokemon.txt");
        string naturesPath = Path.Combine(pbsFolderPath, "natures.txt");

        if (File.Exists(abilitiesPath)) Abilities = ParsePBSFile(abilitiesPath);
        if (File.Exists(itemsPath)) Items = ParsePBSFile(itemsPath);
        if (File.Exists(movesPath)) Moves = ParsePBSFile(movesPath);
        if (File.Exists(pokemonPath)) Species = ParsePBSFile(pokemonPath);
        if (File.Exists(naturesPath)) Natures = ParsePBSFile(naturesPath);

        return IsLoaded;
    }

    private Dictionary<string, string> ParsePBSFile(string filePath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] lines = File.ReadAllLines(filePath);
        string currentId = null;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            // Formato INI moderno: [INTERNAL_NAME]
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentId = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            // Formato INI: Name = Nombre Real
            if (currentId != null && line.Contains("="))
            {
                var parts = line.Split('=', 2);
                if (parts[0].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    result[currentId] = parts[1].Trim();
                    currentId = null;
                }
            }
            // Formato CSV (3+ columnas: ID,INTERNAL_NAME,Name)
            else if (line.Contains(","))
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    string internalName = parts[1].Trim();
                    string realName = parts[2].Trim();
                    if (!string.IsNullOrEmpty(internalName))
                        result[internalName] = realName;
                }
                // Formato CSV simple (2 columnas: INTERNAL_NAME,Name)
                else if (parts.Length == 2)
                {
                    string internalName = parts[0].Trim();
                    string realName = parts[1].Trim();
                    if (!string.IsNullOrEmpty(internalName))
                        result[internalName] = realName;
                }
            }
        }
        return result;
    }

    public string GetName(Dictionary<string, string> dict, string rawId, string fallback = "Ninguno")
    {
        if (string.IsNullOrWhiteSpace(rawId)) return fallback;

        string cleanId = rawId.Replace(":", "").Replace("@", "").Trim();

        if (dict.TryGetValue(cleanId, out string realName))
            return realName;

        return cleanId;
    }
}