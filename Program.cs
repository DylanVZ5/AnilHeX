using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("        AñilHeX - Test de Escritura      ");
        Console.WriteLine("========================================");

        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        // Variaciones posibles del nombre de la carpeta en AppData\Roaming
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

        Console.WriteLine($"[+] Carpeta encontrada: {saveDir}");

        // Buscar automáticamente los archivos de guardado tipo "Partida X.rxdata"
        string[] saveFiles = Directory.GetFiles(saveDir, "Partida *.rxdata");
        if (saveFiles.Length == 0)
        {
            Console.WriteLine("[-] No se encontraron archivos de partida ('Partida X.rxdata') en esa carpeta.");
            return;
        }

        // Seleccionamos el primer archivo de partida encontrado
        string savePath = saveFiles[0];
        Console.WriteLine($"[*] Archivo detectado: {Path.GetFileName(savePath)}");

        try
        {
            SaveParser parser = new SaveParser(savePath);
            SaveDataModel data = parser.ParseSave();

            Console.WriteLine("[+] ¡Partida leída y descomprimida con éxito!");
            Console.WriteLine($"[+] Pokémon en el equipo: {data.Party.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error al procesar el guardado: {ex.Message}");
        }
    }
}