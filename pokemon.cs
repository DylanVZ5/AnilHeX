using System.Collections.Generic;

public class Pokemon
{
    // Identificación Básica
    public string Species { get; set; }
    public string Nickname { get; set; }
    public int Level { get; set; }
    public string Gender { get; set; }
    public bool IsShiny { get; set; }
    public int Form { get; set; }

    // Combate y Estado
    public string Ability { get; set; }
    public string Nature { get; set; }
    public string HeldItem { get; set; }
    public int Happiness { get; set; }
    public string PokeBall { get; set; }

    // Estadísticas Reales (IVs y EVs)
    public int[] IVs { get; set; } = new int[6]; // [PS, Atq, Def, AtqEsp, DefEsp, Vel]
    public int[] EVs { get; set; } = new int[6];

    // Movimientos (Ataques)
    public List<PokemonMove> Moves { get; set; } = new List<PokemonMove>();

    // Información del Encuentro
    public int ObtainLevel { get; set; }
    public string ObtainMap { get; set; }
    public string ObtainText { get; set; }
}

public class PokemonMove
{
    public string Name { get; set; }
    public int PP { get; set; }
    public int PPUp { get; set; }
}