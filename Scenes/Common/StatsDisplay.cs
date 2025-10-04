using Godot;
using System.ComponentModel.DataAnnotations;

public partial class StatsDisplay : CanvasLayer
{
	[Required]
	[Export] private Player Player;
	private TextureProgressBar HealthProgress = null;
	private TextureProgressBar EnergyProgress = null;
	private TextureProgressBar StomachProgress = null;

	public override void _Ready()
	{
		base._Ready();
		StomachProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/StomachProgress");
		EnergyProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/EnergyProgress");
		HealthProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/HealthProgress");

		HealthProgress.MaxValue = Player.MaxHealth;
		EnergyProgress.MaxValue = Player.MaxEnergy;
		StomachProgress.MaxValue = Player.MaxStomachSpace;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		HealthProgress.Value = Player.Health;
		EnergyProgress.Value = Player.Energy;
		StomachProgress.Value = Player.Fullness;
	}

}
