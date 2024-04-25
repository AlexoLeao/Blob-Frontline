using System.Threading;
using Godot;

namespace BlobFrontlines.Scenes;

public partial class Machinegun : Area2D {

	[Export] public PackedScene ProjectileScene { get; set; }
	[Export] public bool isHolstered = true;
	[Export] public int MuzzleSpeed = 450;
	[Export] public double FireRate = 0.1;
    [Export] public int MagazineSize = 32;
	[Export] public double ReloadTime = 1.8;

    private int _currentMagazineCount = 0;
	private bool _canFireLock = true;
	private bool _hasAmmo = false;
	private bool _isReloading = false;
	private AnimatedSprite2D _muzzle;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Visible = false;
		_muzzle = GetNode<AnimatedSprite2D>("Muzzle_Marker/MuzzleFlash");
		_muzzle.Visible = false;
		_currentMagazineCount = MagazineSize;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		LookAt(GetGlobalMousePosition());
		// TODO: use Interlocked check here 
		if (_currentMagazineCount <= 0) _hasAmmo = false;
		else _hasAmmo = true;
	}	

	// Toogles if weapon is equiped or not
	public void ToogleHolster() {
		Visible = !Visible;
	}

	// Reloads weapon magazine
	// TODO: Cancel reloading if gun is tried to be fired
	public async void Reload(System.Func<int> runWhenReloaded) {
		if (_isReloading || Equals(_currentMagazineCount, MagazineSize)) return;
		_isReloading = true;
		await ToSignal(GetTree().CreateTimer(ReloadTime), SceneTreeTimer.SignalName.Timeout); 
		Interlocked.Exchange(ref _currentMagazineCount, MagazineSize);
		runWhenReloaded();
		_isReloading = false;
		//_currentMagazineCount = MagazineSize;
	}

	public async void Fire(System.Func<int> runWhenFired) {
		if (!Visible) return;
		if (_canFireLock && _hasAmmo && !_isReloading) {
			_canFireLock = false;
			FireRound();
			runWhenFired();
			Interlocked.Decrement(ref _currentMagazineCount);
			await ToSignal(GetTree().CreateTimer(FireRate), SceneTreeTimer.SignalName.Timeout);
			_canFireLock = true;
		}
	}

	private void FireRound() {
		var spawnPos = GetNode<Marker2D>("Muzzle_Marker").GlobalPosition;
		GetNode<AudioStreamPlayer>("AudioStreamPlayer").Play();
		FireMuzzleFlash();
		SpawnProjectile();
	}

	private void SpawnProjectile() {
		Projectile bullet = ProjectileScene.Instantiate<Projectile>();
		var velocity = new Vector2(MuzzleSpeed, 0);
		var spawnPos = GetNode<Marker2D>("Muzzle_Marker").GlobalPosition;
		bullet.Position = spawnPos;
        bullet.Rotation = GlobalRotation; 
        bullet.LinearVelocity = velocity.Rotated(GlobalRotation);
		AddChild(bullet);
	}

	private void FireMuzzleFlash() {
        _muzzle.Visible = true;
        _muzzle.Play();
	}
}
