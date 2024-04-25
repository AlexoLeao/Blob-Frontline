using System.Threading;
using Godot;

namespace BlobFrontlines.Scenes;

public partial class Player : CharacterBody2D {
    
    private const int WALK = 50;
    private const int RUN = 75;
    private const double RUN_TIMEOUT = 4.0;
    
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public PackedScene MachinegunScene { get; set; }
    [Export] public int Health = 100;
    [Export] public int Stamina = 100;
    [Export] public double StaminaLossRate = 0.5;
    [Export] public int PlayerSpeed = 50;
    [Export] public double FireRate = 0.2;
    [Export] public int MagazineSize = 32;
    

    private Vector2 _screenSize;
    private int _rotationDirection;
    private bool _canFire = true;
    private bool _canRun = true;
    private bool _auxLock = true;
    private ProgressBar _ammoProgressBar;
    private ProgressBar _staminaProgressBar;
    private ProgressBar _healthProgressBar;
    private Machinegun _machinegun;
    
    public override void _Ready() 
    {
        _screenSize = GetViewportRect().Size;
        _ammoProgressBar = GetNode<ProgressBar>("Camera2D/Control/AmmoCount");
        _staminaProgressBar = GetNode<ProgressBar>("Camera2D/Control/StaminaCount");
        _healthProgressBar = GetNode<ProgressBar>("Camera2D/Control/HealthCount");
        _machinegun = GetNode<Machinegun>("Machinegun");
        //Hide();
    }

    public override void _Process(double delta) 
    {
        UpdateHealthBar();

        if (Health <= 0) QueueFree();
        if (Stamina <= 0) StaminaTimeout();
        
        var velocity = Vector2.Zero;
        if (Input.IsActionPressed("movement_right")) velocity.X++;
        if (Input.IsActionPressed("movement_left")) velocity.X--;
        if (Input.IsActionPressed("movement_down")) velocity.Y++;
        if (Input.IsActionPressed("movement_up")) velocity.Y--;
        
        if (Input.IsActionPressed("Run") && _canRun && velocity != Vector2.Zero) {
            if (_auxLock) Run();
            UpdateStaminaBar();
        } else PlayerSpeed = WALK;
        
        if (Input.IsActionJustReleased("Reload")) {
            _machinegun.Reload(UpdateAmmoProgressBar);
            //_currentMagazineCount = MagazineSize;
            //_ammoProgressBar.Value = MagazineSize;
        }
        
        if (velocity.Length() > 0) velocity = velocity.Normalized() * PlayerSpeed;
        Velocity = velocity;
        
        var animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        
        if (velocity.Length() > 0) {
            animatedSprite2D.Play("walk");
        } else {
            animatedSprite2D.Play("stand");
        }

        if (Input.IsActionPressed("shoot_primary")) {
            _machinegun.Fire(DecrementAmmoProgressBar);
            //animatedSprite2D.Animation = "shoot";
           // if (_canFire && _currentMagazineCount > 0) _Shoot();
        }
        if (Input.IsActionJustReleased("aim_selection")) {
           _machinegun.ToogleHolster();
        }
        MoveAndSlide();
        LookAt(GetGlobalMousePosition());
    }

    private int DecrementAmmoProgressBar() {
        _ammoProgressBar.Value--;
        return (int)_ammoProgressBar.Value;
    }

    private int UpdateAmmoProgressBar() {
        UpdateProgressBarValue(_ammoProgressBar,_machinegun.MagazineSize);
        return 1;
    }

    private void UpdateHealthBar() {
        _healthProgressBar.Value = Health;
    }

    private void UpdateStaminaBar() {
        _staminaProgressBar.Value = Stamina;
    }

    private void UpdateProgressBarValue(ProgressBar bar, int value) {
        bar.Value = value;
    }

    private async void Run() {
        PlayerSpeed = RUN;
        Interlocked.Add(ref Stamina, -10);
        _auxLock = false;
        await ToSignal(GetTree().CreateTimer(StaminaLossRate), SceneTreeTimer.SignalName.Timeout);
        _auxLock = true;
    }

    private async void StaminaTimeout() {
        _canRun = false;
        await ToSignal(GetTree().CreateTimer(RUN_TIMEOUT), SceneTreeTimer.SignalName.Timeout);
        Stamina = 100;
        UpdateStaminaBar();
        _canRun = true;
    }
    
}   