using System.Collections.Generic;

public class SaveDataModel
{
    public List<Pokemon> Party { get; set; } = new List<Pokemon>();
    public List<PCBox> Boxes { get; set; } = new List<PCBox>();
}

public class PCBox
{
    public int BoxIndex { get; set; }
    public string Name { get; set; }
    public Pokemon[] Slots { get; set; } = new Pokemon[30]; // 30 posiciones por caja (pueden ser null)
}