using System;
using System.Collections.Generic;

public class PlayerData
{
    public string TrainerName { get; set; }    // Nombre del jugador
    public uint TrainerID { get; set; }         // Public ID
    public uint SecretID { get; set; }          // Secret ID (Para calcular Shinies)
    public int Money { get; set; }              // Dinero en billetera
    public int Coins { get; set; }              // Monedas del Casino / Game Corner
    public int BadgesCount { get; set; }        // Cantidad de medallas obtenidas
    
    // Lista del Equipo (Party - Máximo 6 Pokémon)
    public List<Pokemon> Party { get; set; } = new List<Pokemon>();

    // Cajas del PC (Generalmente 30 cajas x 30 posiciones)
    public List<Pokemon> StoragePC { get; set; } = new List<Pokemon>();
}