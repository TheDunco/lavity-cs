using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class TestPlant : Plant
{

	[Export] internal PackedScene Consumable;
	private List<Consumable> Consumables = [];
	private readonly PlantEffect ConsumableEffect = new() { Name = "Test Plant Effect", Duration = 10, EnergyMod = 3, HealthMod = 0 };
	private LavityLight LavityLight = null;
	public override void _Ready()
	{
		base._Ready();
		LavityLight = GetNode<LavityLight>("LavityLight");
	}

	public override void OnSpawnTick()
	{
		if (Consumables.Count > MaxConsumables)
		{
			return;
		}

		if (rng.RandiRange(0, 100) < ConsumableSpawnChance)
		{
			Consumable NewConsumable = (Consumable)Consumable.Instantiate();
			NewConsumable.SetEffect(ConsumableEffect);
			NewConsumable.Position = LavityLight.Position;
			Consumables.Add(NewConsumable);
			AddChild(NewConsumable);
		}
	}
}
