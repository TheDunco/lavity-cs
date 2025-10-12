using Godot;
using System.Collections.Generic;

public partial class Plant : Node2D
{

	internal List<Consumable> Consumables = [];
	internal LavityLight LavityLight = null;
	internal RandomNumberGenerator rng = null;

	[ExportCategory("Consumable")]
	[Export] private PackedScene Consumable;
	[Export] private string ConsumableName = "Base Consumable";
	[Export] private int ConsumableDuration = 10;
	[Export] private double ConsumableEnergyMod = 3;
	[Export] private double ConsumableHealthMod = 1;
	[Export] private double ConsumableStomachSpace = 30;
	[Export] private double ConsumableSpawnChance = 2.0;
	[Export] private int MaxConsumables = 3;
	[Export(PropertyHint.ColorNoAlpha)] private Color ConsumableModulate = Colors.White;
	private PlantEffect ConsumableEffect = null;

	public override void _Ready()
	{
		base._Ready();
		LavityLight = GetNode<LavityLight>("LavityLight");
		ConsumableEffect = new()
		{
			Name = ConsumableName,
			Duration = ConsumableDuration,
			EnergyMod = ConsumableEnergyMod,
			HealthMod = ConsumableHealthMod,
			StomachSpace = ConsumableStomachSpace
		};

		rng = GetNode<RngManager>("/root/RngManager").Rng;

		SpawnManager spawnManager = GetNode<SpawnManager>("/root/SpawnManager");
		spawnManager.SpawnTick += OnSpawnTick;

	}

	public void OnSpawnTick()
	{
		if (Consumables.Count > MaxConsumables)
		{
			return;
		}

		if (!IsInstanceValid(LavityLight))
		{
			LavityLight = null;
			return;
		}

		if (rng.RandfRange(0, 100) < ConsumableSpawnChance)
		{
			Consumable NewConsumable = (Consumable)Consumable.Instantiate();
			NewConsumable.SetEffect(ConsumableEffect);
			NewConsumable.Position = LavityLight.Position + new Vector2(rng.RandiRange(-2, 2), rng.RandiRange(-2, 2));
			NewConsumable.Modulate = ConsumableModulate;
			Consumables.Add(NewConsumable);
			AddChild(NewConsumable);
		}
	}
}
