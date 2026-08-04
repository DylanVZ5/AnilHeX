using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("        AñilHeX - Test de Escritura      ");
        Console.WriteLine("========================================");

        // 1. Cargar los archivos PBS desde el directorio raíz
        PBSReader pbs = new PBSReader();
        string pbsPath = Path.Combine(Directory.GetCurrentDirectory(), "PBS");
        
        if (Directory.Exists(pbsPath))
        {
            pbs.LoadPBSDirectory(pbsPath);
            Console.WriteLine("[+] Archivos PBS cargados correctamente.");
        }
        else
        {
            Console.WriteLine($"[-] No se encontró la carpeta PBS en: {pbsPath}");
            Console.WriteLine("    Los nombres saldrán como 'Desconocido'.");
        }

        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] possibleFolders = {
            Path.Combine(appDataRoaming, "Pokemon Anil"),
            Path.Combine(appDataRoaming, "Pokémon Añil"),
            Path.Combine(appDataRoaming, "PokemonAnil")
        };

        string saveDir = null;
        foreach (string folder in possibleFolders)
        {
            if (Directory.Exists(folder))
            {
                saveDir = folder;
                break;
            }
        }

        if (string.IsNullOrEmpty(saveDir))
        {
            Console.WriteLine("[-] No se encontró la carpeta en AppData\\Roaming\\Pokemon Anil.");
            return;
        }

        string[] saveFiles = Directory.GetFiles(saveDir, "Partida *.rxdata");
        if (saveFiles.Length == 0)
        {
            Console.WriteLine("[-] No se encontraron archivos de partida ('Partida X.rxdata').");
            return;
        }

        string savePath = saveFiles[0];
        Console.WriteLine($"[*] Archivo detectado: {Path.GetFileName(savePath)}");

        try
        {
            SaveParser parser = new SaveParser(savePath, pbs);
            SaveDataModel data = parser.ParseSave();

            Console.WriteLine("[+] ¡Partida leída y descomprimida con éxito!");
            Console.WriteLine($"[+] Pokémon en el equipo: {data.Party.Count}");

            if (data.Party.Count > 0)
            {
                Pokemon p1 = data.Party[0];
                Console.WriteLine("\n--- DATOS DEL PRIMER POKÉMON ---");
                Console.WriteLine($"Especie: {p1.Species}");
                Console.WriteLine($"Mote: {p1.Nickname}");
                Console.WriteLine($"Nivel: {p1.Level}");
                Console.WriteLine($"Shiny: {p1.IsShiny}");
                Console.WriteLine($"Habilidad: {p1.Ability}");
                Console.WriteLine($"Naturaleza: {p1.Nature}");
                Console.WriteLine($"IVs (PS/ATQ/DEF/SPA/SPD/VEL): {string.Join("/", p1.IVs)}");

                Console.WriteLine("\n[*] Probando edición y guardado...");
                
                SaveWriter writer = new SaveWriter(savePath, data.RootData);

                // Modificamos a Emboar a nivel 100, shiny y IVs perfectos
                int[] ivsPerfectos = { 31, 31, 31, 31, 31, 31 };
                bool modificado = writer.ModifyPartyPokemon(0, level: 100, isShiny: true, ivs: ivsPerfectos);
                
                if (modificado)
                {
                    bool guardado = writer.Save();
                    if (guardado)
                    {
                        Console.WriteLine("[+] ¡Archivo modificado y guardado con éxito!");
                        Console.WriteLine("[!] Se creó un archivo '.bak'. Abre el juego y verifica a tu Pokémon.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error Crítico: {ex.Message}\n{ex.StackTrace}");
        }
    }
}