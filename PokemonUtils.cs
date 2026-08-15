using System;
using System.Collections.Generic;
using System.Linq;

public static class PokemonUtils
{
    public static string GetNatureEffect(string internalNature)
    {
        switch (internalNature?.ToUpper())
        {
            case "LONELY": return "(+Ataque, -Defensa)";
            case "BRAVE": return "(+Ataque, -Velocid.)";
            case "ADAMANT": return "(+Ataque, -At. Esp.)";
            case "NAUGHTY": return "(+Ataque, -Def. Esp.)";
            case "BOLD": return "(+Defensa, -Ataque)";
            case "RELAXED": return "(+Defensa, -Velocid.)";
            case "IMPISH": return "(+Defensa, -At. Esp.)";
            case "LAX": return "(+Defensa, -Def. Esp.)";
            case "TIMID": return "(+Velocid., -Ataque)";
            case "HASTY": return "(+Velocid., -Defensa)";
            case "JOLLY": return "(+Velocid., -At. Esp.)";
            case "NAIVE": return "(+Velocid., -Def. Esp.)";
            case "MODEST": return "(+At. Esp., -Ataque)";
            case "MILD": return "(+At. Esp., -Defensa)";
            case "QUIET": return "(+At. Esp., -Velocid.)";
            case "RASH": return "(+At. Esp., -Def. Esp.)";
            case "CALM": return "(+Def. Esp., -Ataque)";
            case "GENTLE": return "(+Def. Esp., -Defensa)";
            case "SASSY": return "(+Def. Esp., -Velocid.)";
            case "CAREFUL": return "(+Def. Esp., -At. Esp.)";
            case "HARDY": case "DOCILE": case "SERIOUS": case "BASHFUL": case "QUIRKY": return "(Neutra)";
            default: return "";
        }
    }

    public static string GetNatureDisplayName(string rawName, string internalNature)
    {
        string effect = GetNatureEffect(internalNature);
        return string.IsNullOrEmpty(effect) ? rawName : $"{rawName} {effect}";
    }

    public static string GetInternalIdFromNatureText(string comboText, PBSReader pbs)
    {
        if (string.IsNullOrEmpty(comboText)) return "";
        string cleanName = comboText.Split('(')[0].Trim();
        return GetInternalId(pbs.Natures, cleanName, cleanName, pbs);
    }

    public static string GetInternalId(Dictionary<string, string> dict, string localizedName, string originalInternal, PBSReader pbs)
    {
        if (string.IsNullOrEmpty(localizedName)) return originalInternal;
        var pair = dict.FirstOrDefault(k => pbs.GetName(dict, k.Key, k.Value).Equals(localizedName, StringComparison.OrdinalIgnoreCase));
        if (pair.Key != null) return pair.Key;
        pair = dict.FirstOrDefault(k => k.Value.Equals(localizedName, StringComparison.OrdinalIgnoreCase));
        return pair.Key != null ? pair.Key : originalInternal;
    }

    public static int CalculateExp(int level, string growthRate)
    {
        if (level <= 1) return 0;
        string gr = growthRate?.ToUpper() ?? "MEDIUMFAST";
        
        if (gr == "0") gr = "FAST"; else if (gr == "1") gr = "MEDIUMFAST"; else if (gr == "2") gr = "SLOW";
        else if (gr == "3" || gr == "PARABOLIC") gr = "MEDIUMSLOW"; else if (gr == "4") gr = "ERRATIC"; else if (gr == "5") gr = "FLUCTUATING";

        double L = level; double exp = 0;

        if (gr == "FAST") exp = 0.8 * Math.Pow(L, 3);
        else if (gr == "MEDIUMFAST" || gr == "MEDIUM") exp = Math.Pow(L, 3);
        else if (gr == "SLOW") exp = 1.25 * Math.Pow(L, 3);
        else if (gr == "MEDIUMSLOW") exp = 1.2 * Math.Pow(L, 3) - 15 * Math.Pow(L, 2) + 100 * L - 140;
        else if (gr == "ERRATIC") {
            if (L <= 50) exp = Math.Pow(L, 3) * (100 - L) / 50.0;
            else if (L <= 68) exp = Math.Pow(L, 3) * (150 - L) / 100.0;
            else if (L <= 98) exp = Math.Pow(L, 3) * Math.Floor((1911 - 10 * L) / 3.0) / 500.0;
            else exp = Math.Pow(L, 3) * (160 - L) / 100.0;
        }
        else if (gr == "FLUCTUATING") {
            if (L <= 15) exp = Math.Pow(L, 3) * (Math.Floor((L + 1) / 3.0) + 24) / 50.0;
            else if (L <= 36) exp = Math.Pow(L, 3) * (L + 14) / 50.0;
            else exp = Math.Pow(L, 3) * (Math.Floor(L / 2.0) + 32) / 50.0;
        }
        else exp = Math.Pow(L, 3); 

        return (int)Math.Max(0, exp);
    }
}