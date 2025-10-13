using Godot;

public partial class Consumable : RigidBody2D
{
	public PlantEffect Effect { get; set; }
	public override void _Ready()
	{
		base._Ready();
	}

	public Sprite2D GetStomachTextureSprite()
	{

		return GetNode<Sprite2D>("StomachTextureSprite");
	}

	public Consumable(PlantEffect effect)
	{
		Effect = effect;
	}

	public Consumable()
	{
	}

	//TODO: Move the sound effect here
	public virtual Consumable OnConsume()
	{
		this.Visible = false;
		this.ProcessMode = ProcessModeEnum.Disabled;
		return this;
	}
}

