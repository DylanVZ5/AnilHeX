using System;
using System.Drawing;
using System.IO;

public static class SpriteManager
{
    public static Image GetPokemonIcon(string appRoot, string species, int form, bool isShiny)
    {
        string cleanSpecies = species?.Replace(" ", "")?.ToUpper() ?? ""; 
        
        string normalBasePath = Path.Combine(appRoot, "Graphics", "Pokemon", "Icons");
        string shinyBasePath = Path.Combine(appRoot, "Graphics", "Pokemon", "icons shiny");

        string fileToLoad = null;

        // 1. Si es Shiny o Radiante, busca primero en la carpeta "icons shiny"
        if (isShiny)
        {
            string[] shinyPriorityFiles = {
                Path.Combine(shinyBasePath, $"{cleanSpecies}_{form}.png"),
                Path.Combine(shinyBasePath, $"{cleanSpecies}.png"),
                // Respaldo por si usan el formato clásico con 's' en la carpeta normal
                Path.Combine(normalBasePath, $"{cleanSpecies}_{form}s.png"),
                Path.Combine(normalBasePath, $"{cleanSpecies}_{form}_s.png"),
                Path.Combine(normalBasePath, $"{cleanSpecies}s.png"),
                Path.Combine(normalBasePath, $"{cleanSpecies}_s.png")
            };

            foreach (var file in shinyPriorityFiles) {
                if (File.Exists(file)) {
                    fileToLoad = file;
                    break;
                }
            }
        }

        // 2. Si no es Shiny, o si no encontró el archivo shiny, busca en la carpeta normal
        if (fileToLoad == null)
        {
            string[] regularFiles = {
                Path.Combine(normalBasePath, $"{cleanSpecies}_{form}.png"),
                Path.Combine(normalBasePath, $"{cleanSpecies}.png")
            };

            foreach (var file in regularFiles) {
                if (File.Exists(file)) {
                    fileToLoad = file;
                    break;
                }
            }
        }

        // 3. Procesa y recorta la imagen (para evitar iconos dobles)
        if (fileToLoad != null)
        {
            using (var fs = new FileStream(fileToLoad, FileMode.Open, FileAccess.Read))
            using (Image fullImg = Image.FromStream(fs))
            {
                int frameWidth = fullImg.Width / 2;
                int frameHeight = fullImg.Height;
                
                // Medida de seguridad por si el icono ya es de un solo frame
                if (fullImg.Width <= fullImg.Height * 1.5) { 
                    frameWidth = fullImg.Width; 
                }

                Bitmap singleFrame = new Bitmap(frameWidth, frameHeight);
                using (Graphics g = Graphics.FromImage(singleFrame)) {
                    g.DrawImage(fullImg, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(0, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                }
                return singleFrame;
            }
        }
        return null;
    }
}