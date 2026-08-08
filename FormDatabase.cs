using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class FormDatabase
{
    public static readonly List<string[]> ParadoxGroups = new List<string[]> {
        new[] { "DONPHAN", "GREATTUSK", "IRONTREADS" },
        new[] { "VOLCARONA", "SLITHERWING", "IRONMOTH" },
        new[] { "JIGGLYPUFF", "SCREAMTAIL" },
        new[] { "AMOONGUSS", "BRUTEBONNET" },
        new[] { "MISDREAVUS", "FLUTTERMANE" },
        new[] { "MAGNETON", "SANDYSHOCKS" },
        new[] { "SALAMENCE", "ROARINGMOON" },
        new[] { "DELIBIRD", "IRONBUNDLE" },
        new[] { "HARIYAMA", "IRONHANDS" },
        new[] { "HYDREIGON", "IRONJUGULIS" },
        new[] { "TYRANITAR", "IRONTHORNS" },
        new[] { "GARDEVOIR", "GALLADE", "IRONVALIANT" },
        new[] { "SUICUNE", "WALKINGWAKE" },
        new[] { "VIRIZION", "IRONLEAVES" },
        new[] { "RAIKOU", "RAGINGBOLT" },
        new[] { "ENTEI", "GOUGINGFIRE" },
        new[] { "COBALION", "IRONCROWN" },
        new[] { "TERRAKION", "IRONBOULDER" },
        new[] { "CYCLIZAR", "KORAIDON", "MIRAIDON" }
    };

    public static bool HasFormsOrParadox(string internalSpecies, string appRoot)
    {
        if (string.IsNullOrEmpty(internalSpecies)) return false;
        string spc = internalSpecies.ToUpper();
        
        if (ParadoxGroups.Any(g => g.Contains(spc))) return true;
        
        string basePath = Path.Combine(appRoot, "Graphics", "Pokemon", "Icons");
        if (File.Exists(Path.Combine(basePath, $"{spc}_1.png")) || 
            File.Exists(Path.Combine(basePath, $"{spc}_1_s.png")) || 
            File.Exists(Path.Combine(basePath, $"{spc}_1s.png")))
        {
            return true;
        }

        return false;
    }

    // Basado en la base de datos nativa (pokemon_forms.txt)
    public static string GetFormName(string spc, int form)
    {
        if (form == 0) return "Forma Base";

        // Mapeo exacto de los FormName de la base de datos de Añil
        switch (spc)
        {
            // MEGAS X / Y y Z exclusivas
            case "VENUSAUR": return form == 1 ? "Mega Venusaur X" : form == 2 ? "Mega Venusaur Y" : "Forma Alternativa";
            case "CHARIZARD": return form == 1 ? "Mega Charizard X" : form == 2 ? "Mega Charizard Y" : "Forma Alternativa";
            case "BLASTOISE": return form == 1 ? "Mega Blastoise X" : form == 2 ? "Mega Blastoise Y" : "Forma Alternativa";
            case "MEWTWO": return form == 1 ? "Mega Mewtwo X" : form == 2 ? "Mega Mewtwo Y" : form == 5 ? "Mega Armadura" : "Forma Alternativa";
            case "RAICHU": return form == 1 ? "Forma Alola" : form == 2 ? "Mega Raichu x" : form == 3 ? "Mega Raichu Y" : "Forma Alternativa";
            case "GARCHOMP": return form == 1 ? "Mega Garchomp" : form == 2 ? "Mega Garchomp Z" : "Forma Alternativa";
            case "LUCARIO": return form == 1 ? "Mega Lucario" : form == 2 ? "Mega Lucario Z" : "Forma Alternativa";
            case "ABSOL": return form == 1 ? "Mega Absol" : form == 2 ? "Mega Absol Z" : "Forma Alternativa";
            case "MAGEARNA": return form == 1 ? "Forma Color Vetusta" : form == 2 ? "Mega Magearna" : form == 3 ? "Mega Magearna Forma Color Vetusta" : "Forma Alternativa";

            // VARIANTES DE ALOLA
            case "RATTATA": case "RATICATE": case "SANDSHREW": case "SANDSLASH": case "VULPIX": case "NINETALES": 
            case "DIGLETT": case "DUGTRIO": case "PERSIAN": case "GEODUDE": case "GRAVELER": case "GOLEM": 
            case "GRIMER": case "MUK": case "EXEGGUTOR": case "MAROWAK": 
                if (form == 1) return "Forma Alola"; break;

            // VARIANTES DE GALAR
            case "PONYTA": case "RAPIDASH": case "SLOWPOKE": case "FARFETCHD": case "WEEZING": case "MRMIME":
            case "ARTICUNO": case "ZAPDOS": case "MOLTRES": case "SLOWKING": case "CORSOLA": case "ZIGZAGOON":
            case "LINOONE": case "YAMASK": case "STUNFISK":
                if (form == 1) return "Forma Galar"; break;
            
            // VARIANTES DE HISUI
            case "GROWLITHE": case "ARCANINE": case "VOLTORB": case "ELECTRODE": case "TYPHLOSION": case "QWILFISH":
            case "SNEASEL": case "SAMUROTT": case "LILLIGANT": case "ZORUA": case "ZOROARK": case "BRAVIARY":
            case "SLIGGOO": case "GOODRA": case "AVALUGG": case "DECIDUEYE":
                if (form == 1) return "Forma Hisui"; break;

            // CASOS ESPECIALES / MÚLTIPLES FORMAS
            case "PIKACHU":
                string[] pikaForms = { "", "", "Forma Coqueta", "Forma Aristócrata", "Forma Enmascarada", "Forma Erudita", "Forma Superstar", "Forma Roquera", "Forma Gorra Original", "Forma Gorra Hoenn", "Forma Gorra Sinnoh", "Forma Gorra Teselia", "Forma Gorra Kalos", "Forma Gorra Alola", "Forma Gorra Compañero", "Forma Gorra Trotamundos", "Forma Amarillo", "Mega Pikachu" };
                if (form >= 2 && form < pikaForms.Length) return pikaForms[form]; break;
            case "PICHU": if (form == 2) return "Forma Picoreja"; break;
            case "MEOWTH": return form == 1 ? "Forma Alola" : form == 2 ? "Forma Galar" : "Forma Alternativa";
            case "SLOWBRO": return form == 1 ? "Forma Galar" : form == 2 ? "Mega Slowbro" : "Forma Alternativa";
            case "TAUROS": return form == 1 ? "Forma Paldea (Combatiente)" : form == 2 ? "Forma Paldea (Ardiente)" : form == 3 ? "Forma Paldea (Acuática)" : "Forma Alternativa";
            case "WOOPER": if (form == 1) return "Forma Paldea"; break;
            case "CASTFORM": return form == 1 ? "Forma Sol" : form == 2 ? "Forma Lluvia" : form == 3 ? "Forma Nieve" : "Forma Alternativa";
            case "KYOGRE": case "GROUDON": if (form == 1) return "Forma Primigenia"; break;
            case "DEOXYS": return form == 1 ? "Forma Ataque" : form == 2 ? "Forma Defensa" : form == 3 ? "Forma Velocidad" : "Forma Alternativa";
            case "BURMY": case "WORMADAM": return form == 1 ? "Forma Tronco Arena" : form == 2 ? "Forma Tronco Basura" : "Forma Alternativa";
            case "CHERRIM": if (form == 1) return "Forma Soleada"; break;
            case "SHELLOS": case "GASTRODON": if (form == 1) return "Forma Mar Este"; break;
            case "ROTOM":
                string[] rotomForms = { "", "Forma Calor", "Forma Lavado", "Forma Frío", "Forma Ventilador", "Forma Corte" };
                if (form >= 1 && form <= 5) return rotomForms[form]; break;
            case "GIRATINA": if (form == 1) return "Forma Origen"; break;
            case "SHAYMIN": if (form == 1) return "Forma Cielo"; break;
            case "ARCEUS": case "SILVALLY":
                string[] types = { "", "Tipo Lucha", "Tipo Volador", "Tipo Veneno", "Tipo Tierra", "Tipo Roca", "Tipo Bicho", "Tipo Fantasma", "Tipo Acero", "Tipo Desconocido", "Tipo Fuego", "Tipo Agua", "Tipo Planta", "Tipo Eléctrico", "Tipo Psíquico", "Tipo Hielo", "Tipo Dragón", "Tipo Siniestro", "Tipo Hada" };
                if (form >= 1 && form <= 18) return types[form]; break;
            case "DARMANITAN": return form == 1 ? "Forma Daruma" : form == 2 ? "Forma Galar" : form == 3 ? "Forma Daruma de Galar" : "Forma Alternativa";
            case "DEERLING": case "SAWSBUCK": return form == 1 ? "Forma Verano" : form == 2 ? "Forma Otoño" : form == 3 ? "Forma Invierno" : "Forma Alternativa";
            case "TORNADUS": case "THUNDURUS": case "LANDORUS": case "ENAMORUS": if (form == 1) return "Forma Tótem"; break;
            case "KYUREM": return form == 1 || form == 3 ? "Forma Blanca" : form == 2 || form == 4 ? "Forma Negra" : "Forma Alternativa";
            case "KELDEO": if (form == 1) return "Forma Brío"; break;
            case "MELOETTA": if (form == 1) return "Forma Danza"; break;
            case "GENESECT": return form == 1 ? "Forma FulgoROM" : form == 2 ? "Forma PiroROM" : form == 3 ? "Forma CrioROM" : form == 4 ? "Forma HidroROM" : "Forma Alternativa";
            case "GRENINJA": return form == 1 ? "Greninja Ash" : form == 2 ? "Mega Greninja" : "Forma Alternativa";
            case "VIVILLON":
                string[] vivillon = { "", "Motivo Isleño", "Motivo Continental", "Motivo Oriental", "Motivo Vergel", "Motivo Estepa", "Motivo Polar", "Motivo Jungla", "Motivo Marino", "Motivo Moderno", "Motivo Monzón", "Motivo Océano", "Motivo Taiga", "Motivo Oasis", "Motivo Desierto", "Motivo Pantano", "Motivo Solar", "Motivo Tundra", "Motivo Fantasía", "Motivo Poké Ball" };
                if (form >= 1 && form <= 19) return vivillon[form]; break;
            case "FLABEBE": case "FLOETTE": case "FLORGES":
                if (form == 1) return "Forma Flor Amarilla"; if (form == 2) return "Forma Flor Naranja";
                if (form == 3) return "Forma Flor Azul"; if (form == 4) return "Forma Flor Blanca";
                if (spc == "FLOETTE" && form == 5) return "Forma Flor Eterna";
                if (spc == "FLOETTE" && form == 6) return "Mega Floette"; break;
            case "FURFROU":
                string[] furfrou = { "", "Forma Corte Corazón", "Forma Corte Estrella", "Forma Corte Rombo", "Forma Corte Señorita", "Forma Corte Dama", "Forma Corte Caballero", "Forma Corte Aristocrático", "Forma Corte Kabuki", "Forma Corte Faraónico" };
                if (form >= 1 && form <= 9) return furfrou[form]; break;
            case "MEOWSTIC": return form == 1 ? "Forma Hembra" : form == 2 ? "Mega Meowstic" : "Forma Alternativa";
            case "AEGISLASH": if (form == 1) return "Forma Filo"; break;
            case "PUMPKABOO": case "GOURGEIST": return form == 1 ? "Tamaño Normal" : form == 2 ? "Tamaño Grande" : form == 3 ? "Tamaño Extragrande" : "Forma Alternativa";
            case "XERNEAS": if (form == 1) return "Modo Activo"; break;
            case "ZYGARDE": return form == 1 ? "Forma 10%" : form == 2 || form == 3 ? "Forma Completa" : form == 4 ? "Mega Zygarde" : "Forma Alternativa";
            case "HOOPA": if (form == 1) return "Forma Desatada"; break;
            case "ORICORIO": return form == 1 ? "Estilo Animado" : form == 2 ? "Estilo Plácido" : form == 3 ? "Estilo Refinado" : "Forma Alternativa";
            case "LYCANROC": return form == 1 ? "Forma Nocturna" : form == 2 ? "Forma Crepuscular" : "Forma Alternativa";
            case "WISHIWASHI": if (form == 1) return "Forma Banco"; break;
            case "MINIOR":
                string[] minior = { "", "", "", "", "", "", "", "Forma Núcleo Rojo", "Forma Núcleo Naranja", "Forma Núcleo Amarillo", "Forma Núcleo Verde", "Forma Núcleo Azul", "Forma Núcleo Añil", "Forma Núcleo Violeta" };
                if (form >= 7 && form <= 13) return minior[form]; break;
            case "MIMIKYU": if (form == 1) return "Forma Descubierta"; break;
            case "NECROZMA": return form == 1 ? "Forma Melena Crepuscular" : form == 2 ? "Forma Alas del Alba" : form == 3 ? "Forma Ultra-Necrozma" : "Forma Alternativa";
            case "CRAMORANT": return form == 1 ? "Forma Tragatodo" : form == 2 ? "Forma Engulletodo" : "Forma Alternativa";
            case "TOXTRICITY": return form == 1 ? "Forma Grave" : form == 2 || form == 3 ? "Mega Toxtricity" : "Forma Alternativa";
            case "SINISTEA": case "POLTEAGEIST": if (form == 1) return "Forma Genuina"; break;
            case "ALCREMIE": return form == 63 ? "Mega Alcremie" : $"Forma Confite {form}";
            case "EISCUE": if (form == 1) return "Forma Cara Deshielo"; break;
            case "INDEEDEE": case "BASCULEGION": case "OINKOLOGNE": if (form == 1) return "Forma Hembra"; break;
            case "MORPEKO": if (form == 1) return "Forma Voraz"; break;
            case "ZACIAN": if (form == 1) return "Forma Espada Suprema"; break;
            case "ZAMAZENTA": if (form == 1) return "Forma Escudo Supremo"; break;
            case "URSHIFU": if (form == 1) return "Estilo Fluido"; break;
            case "CALYREX": return form == 1 ? "Forma Jinete Glacial" : form == 2 ? "Forma Jinete Espectral" : "Forma Alternativa";
            case "DIALGA": case "PALKIA": if (form == 1) return "Forma Origen"; break;
            case "BASCULIN": return form == 2 ? "Forma Raya Roja" : form == 3 ? "Forma Raya Azul" : "Forma Alternativa";
            case "URSALUNA": if (form == 1) return "Forma Luna Carmesí"; break;
            case "DUDUNSPARCE": if (form == 1) return "Forma Trinodular"; break;
            case "PALAFIN": if (form == 1) return "Forma Heroica"; break;
            case "MAUSHOLD": if (form == 1) return "Familia de Tres"; break;
            case "TATSUGIRI": return form == 1 || form == 4 ? "Forma Lánguida" : form == 2 || form == 5 ? "Forma Recta" : form == 3 ? "Forma Curvada" : "Forma Alternativa";
            case "SQUAWKABILLY": return form == 1 ? "Plumaje Azul" : form == 2 ? "Plumaje Amarillo" : form == 3 ? "Plumaje Blanco" : "Forma Alternativa";
            case "GIMMIGHOUL": if (form == 1) return "Forma Andante"; break;
            case "POLTCHAGEIST": case "SINISTCHA": if (form == 1) return "Forma Opulenta/Exquisita"; break;
            case "OGERPON": return form == 1 ? "Máscara Fuente" : form == 2 ? "Máscara Horno" : form == 3 ? "Máscara Cimiento" : "Forma Alternativa";
            case "TERAPAGOS": return form == 1 ? "Forma Teracristal" : form == 2 ? "Forma Astral" : "Forma Alternativa";
            case "ZARUDE": if (form == 1) return "Forma papá"; break;
            case "EEVEE": return form == 1 ? "Eevee Lets Go" : form == 2 ? "Mega Eevee" : "Forma Alternativa";

            // MEGAS DE LA BASE DE DATOS
            case "BEEDRILL": case "PIDGEOT": case "ALAKAZAM": case "KANGASKHAN": case "PINSIR": case "GYARADOS": 
            case "AERODACTYL": case "AMPHAROS": case "STEELIX": case "SCIZOR": case "HERACROSS": case "HOUNDOOM": 
            case "TYRANITAR": case "SCEPTILE": case "BLAZIKEN": case "SWAMPERT": case "GARDEVOIR": case "SABLEYE": 
            case "MAWILE": case "AGGRON": case "MEDICHAM": case "MANECTRIC": case "SHARPEDO": case "CAMERUPT": 
            case "ALTARIA": case "BANETTE": case "GLALIE": case "SALAMENCE": case "METAGROSS": case "LATIAS": 
            case "LATIOS": case "RAYQUAZA": case "LOPUNNY": case "ABOMASNOW": case "GALLADE": case "DIANCIE": 
            case "AUDINO": case "KINGLER": case "LAPRAS": case "MACHAMP": case "GARBODOR": case "CORVIKNIGHT": 
            case "ORBEETLE": case "BUTTERFREE": case "DREDNAW": case "COALOSSAL": case "FLAPPLE": case "APPLETUN": 
            case "SANDACONDA": case "CENTISKORCH": case "HATTERENE": case "GRIMMSNARL": case "COPPERAJAH": 
            case "DURALUDON": case "JUMPLUFF": case "RILLABOOM": case "CINDERACE": case "INTELEON": case "SNORLAX": 
            case "CLEFABLE": case "STARMIE": case "VICTREEBEL": case "DRAGONITE": case "MEGANIUM": case "FERALIGATR": 
            case "SKARMORY": case "FROSLASS": case "EMBOAR": case "EXCADRILL": case "SCOLIPEDE": case "SCRAFTY": 
            case "EELEKTROSS": case "CHANDELURE": case "CHESNAUGHT": case "DELPHOX": case "PYROAR": case "MALAMAR": 
            case "BARBARACLE": case "DRAGALGE": case "HAWLUCHA": case "DRAMPA": case "FALINKS": case "STARAPTOR": 
            case "CRABOMINABLE": case "CHIMECHO": case "GOLURK": case "BAXCALIBUR": case "SCOVILLAIN": 
            case "GLIMMORA": case "GOLISOPOD": case "ZERAORA": case "HEATRAN": case "DARKRAI":
                if (form == 1) return $"Mega {spc.Substring(0, 1).ToUpper() + spc.Substring(1).ToLower()}"; break;
        }
        
        return $"Forma Alternativa {form}";
    }
}