using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("        AñilHeX - Test de Mote           ");
        Console.WriteLine("========================================");

        PBSReader pbs = new PBSReader();
        string pbsPath = Path.Combine(Directory.GetCurrentDirectory(), "PBS");
        
        if (Directory.Exists(pbsPath)) pbs.LoadPBSDirectory(pbsPath);

        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] possibleFolders = {
            Path.Combine(appDataRoaming, "Pokemon Anil"),
            Path.Combine(appDataRoaming, "Pokémon Añil"),
            Path.Combine(appDataRoaming, "PokemonAnil")
        };

        string saveDir = null;
        foreach (string folder in possibleFolders)
        {
            if (Directory.Exists(folder)) { saveDir = folder; break; }
        }

        if (string.IsNullOrEmpty(saveDir)) return;

        string[] saveFiles = Directory.GetFiles(saveDir, "Partida *.rxdata");
        if (saveFiles.Length == 0) return;

        string savePath = saveFiles[0];
        Console.WriteLine($"[*] Archivo detectado: {Path.GetFileName(savePath)}");

        try
        {
            SaveParser parser = new SaveParser(savePath, pbs);
            SaveDataModel data = parser.ParseSave();

            if (data.Party.Count > 0)
            {
                Pokemon p1 = data.Party[0];
                Console.WriteLine("\n--- DATOS ACTUALES ---");
                Console.WriteLine($"Especie: {p1.Species}");
                Console.WriteLine($"Mote Actual: {p1.Nickname}");

                Console.WriteLine("\n[*] Escribiendo 'ANIL-HEX' como nuevo mote...");
                
                SaveWriter writer = new SaveWriter(savePath, data.RootData);
                bool modificado = writer.ModifyPartyPokemon(0, newNickname: "ANIL-HEX");
                
                if (modificado && writer.Save())
                {
                    Console.WriteLine("[+] ¡Partida guardada! Abre el juego y verifica si el Emboar cambió de nombre.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error Crítico: {ex.Message}");
        }
    }
}