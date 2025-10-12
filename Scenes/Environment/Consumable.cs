using Godot;

public partial class Consumable : RigidBody2D
{
	private PlantEffect Effect;
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

	public void SetEffect(PlantEffect effect)
	{
		Effect = effect;
	}

	public virtual PlantEffect OnConsume()
	{
		this.QueueFree();
		return Effect;
	}
}

