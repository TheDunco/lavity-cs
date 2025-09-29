using Godot;
using System;


public partial class StatsManager : Node
{
	[Signal] public delegate void StatsTickEventHandler();

	private Timer StatsTimer;

	public override void _Ready()
	{
		StatsTimer = new Timer
		{
			WaitTime = 1.0,
			OneShot = false,
			Autostart = true
		};
		StatsTimer.Timeout += () => EmitSignal(SignalName.StatsTick);
		AddChild(StatsTimer);
	}
}

public class PlantEffect
{
	public string Name;
	public double Duration;    // seconds to digest
	public double EnergyMod; // per second
	public double HealthMod; // per second

	public PlantEffect()
	{
	}


	public PlantEffect(string name = "Default Plant Effect", double duration = 1.0, double eMod = 0, double hMod = 0)
	{
		Name = name;
		Duration = duration;
		EnergyMod = eMod;
		HealthMod = hMod;
	}
};

