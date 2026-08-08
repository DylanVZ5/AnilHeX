using System.Collections.Generic;

public class Pokemon
{
    public string InternalSpecies { get; set; } 
    public string Species { get; set; }         
    public string Nickname { get; set; }
    public int Level { get; set; }
    public string Gender { get; set; }
    public bool IsShiny { get; set; }
    public bool IsSuperShiny { get; set; }      // NUEVA PROPIEDAD RADIANTE
    public int Form { get; set; }
    public long PersonalID { get; set; }        

    public string InternalAbility { get; set; }
    public string Ability { get; set; }
    
    public string InternalNature { get; set; }
    public string Nature { get; set; }
    
    public string InternalHeldItem { get; set; }
    public string HeldItem { get; set; }
    
    public int Happiness { get; set; }
    
    public string InternalPokeBall { get; set; }
    public string PokeBall { get; set; }
    
    public int Exp { get; set; }                

    public int[] IVs { get; set; } = new int[6]; 
    public int[] EVs { get; set; } = new int[6];

    public List<PokemonMove> Moves { get; set; } = new List<PokemonMove>();

    public int ObtainLevel { get; set; }
    public string ObtainMap { get; set; }
    public string ObtainText { get; set; }
    public bool IsEgg { get; set; }             
}

public class PokemonMove
{
    public string InternalName { get; set; }
    public string Name { get; set; }
    public int PP { get; set; }
    public int PPUp { get; set; }
}