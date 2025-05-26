using Godot;
using Godot.Collections;
using System;

public partial class PlayerMovement : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float CrouchSpeed = 2.5f;
	public const float JumpVelocity = 4.5f;
	public const float Sensitivity = 3.0f;
	public bool isCrouched = false;

    public override void _Ready()
    {
        base._Ready();
		Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		if(Input.IsActionJustPressed("Flashlight"))
		{
			SpotLight3D light = GetNode<Camera3D>("Camera3D").GetNode<SpotLight3D>("FlashLight");
            light.Visible = !light.Visible;
        }

		HandleCrouch();

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBackward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		float currentSpeed = isCrouched? CrouchSpeed : Speed;
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, currentSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

		if(@event is InputEventMouseMotion)
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			Rotation = new Vector3(Rotation.X, Rotation.Y - motion.Relative.X / 1000 * Sensitivity, Rotation.Z);
			/*Recommend getting the camera and storing as a variable when game launches but following the tutorial for now*/
			Camera3D camera = GetNode<Camera3D>("Camera3D");
            camera.Rotation = new Vector3(Mathf.Clamp(camera.Rotation.X - motion.Relative.Y / 1000 * Sensitivity,-2,2), camera.Rotation.Y, camera.Rotation.Z);
		}
    }

	private void HandleCrouch()
	{
		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D rayQuery = new PhysicsRayQueryParameters3D()
		{
			From = Position,
			To = Position + (Vector3.Up * 2),
			Exclude = [GetRid()]
		};
		Dictionary results = spaceState.IntersectRay(rayQuery);

        //Handle Crouch(Toggle)
        if (Input.IsActionJustPressed("Crouch"))
        {
            if (isCrouched && results.Count > 0)
            {
				return;
            }
            GetNode<AnimationPlayer>("AnimationPlayer").Play(isCrouched ? "UnCrouch" : "Crouch");
            isCrouched = !isCrouched;
        }
    }
}
