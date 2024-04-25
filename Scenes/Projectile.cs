using Godot;


namespace BlobFrontlines.Scenes;

public partial class Projectile : RigidBody2D
{
    [Export] public int Speed = 450;

    [Export] public int Damage = 35;
    
    public override void _Ready() {
        GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("move_2");
        TopLevel = true;
    }

    public override void _Process(double delta) {
        
    }
    
    private void _on_visible_on_screen_notifier_2d_screen_exited() 
    {
        QueueFree();
    }

    private void _on_body_entered(Node body) {
        if (body.IsInGroup("players")) {
            var player = (Player)body;
            player.Health -= Damage;
        }
        QueueFree();
    }
}