using Godot;
using System;


public partial class TestPlant : Plant
{
	private PlantEffect seedEffect = new() { Name = "Test Plant Effect", Duration = 10, EnergyMod = 3, HealthMod = 0 };
	[Export] PackedScene SeedConsumable;
	public override void OnSpawnTick()
	{
		AddChild(SeedConsumable.Instantiate());
		var Children = GetChildren();
		foreach (var child in Children)
		{
			if (child is Consumable consumable)
			{
				consumable.SetEffect(seedEffect);
			}
		}

	}
}
