using Godot;
using System;

public partial class SpawnManager : Node
{
	[Signal] public delegate void SpawnTickEventHandler();

	private Timer SpawnTimer;

	public override void _Ready()
	{
		SpawnTimer = new Timer
		{
			WaitTime = 1.0,
			OneShot = false,
			Autostart = true
		};
		SpawnTimer.Timeout += () => EmitSignal(SignalName.SpawnTick);
		AddChild(SpawnTimer);
	}
}
