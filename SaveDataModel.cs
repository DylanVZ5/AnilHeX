using System.Collections.Generic;

public class ItemSlot 
{
    public string InternalName { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public int Pocket { get; set; }
}

public class SaveDataModel
{
    public object RootData { get; set; } 
    public List<Pokemon> Party { get; set; } = new List<Pokemon>();
    public List<PCBox> Boxes { get; set; } = new List<PCBox>();
    public List<ItemSlot> Bag { get; set; } = new List<ItemSlot>();
}

public class PCBox
{
    public int BoxIndex { get; set; }
    public string Name { get; set; }
    public Pokemon[] Slots { get; set; } = new Pokemon[30]; 
}