using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Enemy : CharacterBody3D
{
	public enum States
	{
		Patrol,
		Chasing,
		Hunting,
		Waiting
	}

	public States CurrentState;
	public NavigationAgent3D NavigationAgent;

	private List<Marker3D> waypoints = new();
	private int waypointIndex;
	private float patrolSpeed = 2;
	private float chaseSpeed = 3;
	private Timer patrolTimer;
	private Vector3 lastLookingDirection;
	/*I absolutely hate this*/
	private bool playerInEarshotFar;
	private bool playerInEarshotClose;
	private bool playerInVisionFar;
	private bool playerInVisionClose;
	private Camera3D playerHeadPosition;

    private PhysicsDirectSpaceState3D spaceState;
	private Marker3D headRaycastLocation;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		NavigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		patrolTimer = GetNode<Timer>("PatrolTimer");
		CurrentState = States.Patrol;
        spaceState = GetWorld3D().DirectSpaceState;
		headRaycastLocation = GetNode<Marker3D>("HeadRaycastLocation");
        playerHeadPosition = GetTree().GetNodesInGroup("Player")[0].GetNode<Camera3D>("Camera3D");

		var waypointList = GetTree().GetNodesInGroup("EnemyWaypoint");
		waypointList.Shuffle();
        waypoints = waypointList.Select(x => x as Marker3D).ToList();

		NavigationAgent.TargetPosition = waypoints[0].GlobalPosition;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        switch (CurrentState)
		{
            case States.Patrol:
                if (NavigationAgent.IsNavigationFinished())
                {
                    patrolTimer.Start();
                    CurrentState = States.Waiting;
                    return;
                }

                MoveTowardsPoint(delta, patrolSpeed);

                break;
            case States.Chasing:
                if (NavigationAgent.IsNavigationFinished())
                {
                    if(playerInVisionClose)
                        GD.Print("rawwr XD");

                    CurrentState = States.Waiting;
                    patrolTimer.Start();
                    return;
                }
                MoveTowardsPoint(delta, chaseSpeed);
                break;
			case States.Hunting:
				if(NavigationAgent.IsNavigationFinished())
				{
					CurrentState = States.Waiting;
					patrolTimer.Start();
					return;
				}
                MoveTowardsPoint(delta, patrolSpeed);

                break;
			case States.Waiting:
				break;
			default:
				break;
		}
	}

    private void MoveTowardsPoint(double delta, float speed)
    {
        Vector3 targetPos = NavigationAgent.GetNextPathPosition();
        Vector3 direction = GlobalPosition.DirectionTo(targetPos);
        Vector3 lookAtDirection = lastLookingDirection.Lerp(targetPos, .05f);
        LookAt(new Vector3(lookAtDirection.X, GlobalPosition.Y, lookAtDirection.Z), Vector3.Up);
        lastLookingDirection = lookAtDirection;
        Velocity = direction * speed;
        MoveAndSlide();
        if(playerInEarshotFar)
        {
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
	{
        PhysicsRayQueryParameters3D rayQuery = new PhysicsRayQueryParameters3D()
        {
            From = headRaycastLocation.GlobalPosition,
            To = playerHeadPosition.GlobalPosition + (Vector3.Down * .25f),
            Exclude = [GetRid()]
        };
        Dictionary results = spaceState.IntersectRay(rayQuery);

		if(results.Keys.Count > 0 )
		{
			Node3D node = (Node3D)results["collider"];
            if (node is PlayerMovement player)
            {
                if (playerInEarshotFar && !player.isCrouched)
                {
                    CurrentState = States.Hunting;
					NavigationAgent.TargetPosition = player.Position;
                    GD.Print("HearFar");
                }
                if (playerInEarshotClose)
				{
					CurrentState = States.Chasing;
                    GD.Print("HearClose");
                }

                if (playerInVisionFar && !player.isCrouched)
                {
                    CurrentState = States.Hunting;
                    NavigationAgent.TargetPosition = player.Position;
                    GD.Print("SeeFar");
                }
                if (playerInVisionClose)
				{
					CurrentState = States.Chasing;
                    GD.Print("SeeClose");
                }
            }
        }
    }

    #region Signals

    private void _on_patrol_timer_timeout()
	{
		waypointIndex = Mathf.PosMod(++waypointIndex,waypoints.Count);
		NavigationAgent.TargetPosition = waypoints[waypointIndex].GlobalPosition;
		CurrentState = States.Patrol;
	}

	private void _on_close_hearing_area_body_entered(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInEarshotClose = true;
	}
	private void _on_close_hearing_area_body_exited(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInEarshotClose =false;
    }

	private void _on_far_hearing_area_body_entered(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInEarshotFar = true;
    }
	private void _on_far_hearing_area_body_exited(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInEarshotFar =false;
    }

	private void _on_close_sight_body_entered(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInVisionClose = true;
    }
	private void _on_close_sight_body_exited(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInVisionClose =false;
    }

	private void _on_far_sight_body_entered(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;

		playerInVisionFar=true;
        
    }
	private void _on_far_sight_body_exited(Node3D obj)
	{
        if (obj is not PlayerMovement)
            return;
        playerInVisionFar =false;
    }
    #endregion
}
