using Godot;
using System.ComponentModel.DataAnnotations;

public partial class StatsDisplay : CanvasLayer
{
	[Required]
	[Export] private Player Player;
	private TextureProgressBar HealthProgress = null;
	private TextureProgressBar EnergyProgress = null;

	public override void _Ready()
	{
		base._Ready();
		EnergyProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/EnergyProgress");
		HealthProgress = GetNode<TextureProgressBar>("VBoxContainer/HBoxContainer/HealthProgress");

		HealthProgress.MaxValue = Player.MaxHealth;
		EnergyProgress.MaxValue = Player.MaxEnergy;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		HealthProgress.Value = Player.Health;
		EnergyProgress.Value = Player.Energy;
	}

}
