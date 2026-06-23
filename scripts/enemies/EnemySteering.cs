using Godot;
using System.Collections.Generic;

public enum WatchdogState
{
    Fresh,
    Recovering,
    Stuck,
}

public struct ProgressWatchdog
{
    public float Tolerance { get; }
    public float ResetTime { get; }
    public WatchdogState State { get; private set; } = WatchdogState.Fresh;
    public float StuckDuration => _stuckTime;

    private float _stuckTime;
    private float _freshTime;

    public ProgressWatchdog(float tolerance, float resetTime)
    {
        Tolerance = tolerance;
        ResetTime = resetTime;
    }

    public WatchdogState Sample(float desiredSpeed, float actualSpeed, float deltaSeconds)
    {
        var isStuck = Mathf.Abs(desiredSpeed) > 0.5f
                      && Mathf.Abs(actualSpeed) < Mathf.Abs(desiredSpeed) * 0.3f;

        if (isStuck)
        {
            _stuckTime += deltaSeconds;
            _freshTime = 0f;
        }
        else
        {
            _stuckTime = 0f;
            _freshTime += deltaSeconds;
        }

        if (_stuckTime >= Tolerance)
            State = WatchdogState.Stuck;
        else if (_freshTime >= ResetTime)
            State = WatchdogState.Fresh;
        else
            State = WatchdogState.Recovering;

        return State;
    }

    public void Reset()
    {
        _stuckTime = 0f;
        _freshTime = ResetTime;
        State = WatchdogState.Fresh;
    }
}

public static class EnemySteering
{
    private const uint TerrainMask = 1u;
    private const float StepUpCheckStepHeight = 24f;

    public static Vector2 ComputeSeparationPush(
        Vector2 selfPosition,
        IEnumerable<Node2D> peers,
        float radius,
        float strength)
    {
        if (radius <= 0f || strength <= 0f || peers == null)
            return Vector2.Zero;

        var push = Vector2.Zero;
        var radiusSq = radius * radius;
        foreach (var peer in peers)
        {
            if (peer == null || !GodotObject.IsInstanceValid(peer))
                continue;
            var diff = selfPosition - peer.GlobalPosition;
            var distSq = diff.LengthSquared();
            if (distSq <= 0.0001f || distSq > radiusSq)
                continue;
            var dist = Mathf.Sqrt(distSq);
            var falloff = 1f - (dist / radius);
            push += (diff / dist) * (falloff * strength);
        }
        return push;
    }

    public static bool IsBlockedAhead(Node2D self, Vector2 forwardDir, float distance)
    {
        if (distance <= 0f)
            return false;
        var space = self.GetWorld2D().DirectSpaceState;
        var dir = forwardDir.Normalized();
        var rect = new RectangleShape2D { Size = new Vector2(distance, 16f) };
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = rect,
            Transform = new Transform2D(dir.Angle(), self.GlobalPosition + dir * (distance * 0.5f)),
            CollisionMask = TerrainMask,
            CollideWithBodies = true,
            CollideWithAreas = false,
        };
        return space.IntersectShape(query, 1).Count > 0;
    }

    public static bool HasFloorAhead(Node2D self, Vector2 forwardDir, float forwardDistance, float downDistance)
    {
        if (forwardDistance <= 0f || downDistance <= 0f)
            return false;
        var space = self.GetWorld2D().DirectSpaceState;
        var origin = self.GlobalPosition + forwardDir.Normalized() * forwardDistance;
        var query = PhysicsRayQueryParameters2D.Create(
            origin,
            origin + new Vector2(0, downDistance),
            TerrainMask);
        return space.IntersectRay(query).Count > 0;
    }

    public static bool CanStepUp(Node2D self, Vector2 forwardDir, float probeDistance)
    {
        if (probeDistance <= 0f)
            return false;
        var stepHeight = StepUpCheckStepHeight;
        var space = self.GetWorld2D().DirectSpaceState;
        var dir = forwardDir.Normalized();
        var pos = self.GlobalPosition;

        var wallOrigin = pos;
        var wallTarget = pos + dir * probeDistance;
        var wallQuery = PhysicsRayQueryParameters2D.Create(wallOrigin, wallTarget, TerrainMask);
        if (space.IntersectRay(wallQuery).Count == 0)
            return false;

        var upOrigin = pos + new Vector2(0, -stepHeight);
        var upTarget = upOrigin + dir * probeDistance;
        var upQuery = PhysicsRayQueryParameters2D.Create(upOrigin, upTarget, TerrainMask);
        if (space.IntersectRay(upQuery).Count > 0)
            return false;

        var landOrigin = pos + dir * probeDistance + new Vector2(0, -stepHeight);
        var landTarget = landOrigin + new Vector2(0, 200f);
        var landQuery = PhysicsRayQueryParameters2D.Create(landOrigin, landTarget, TerrainMask);
        return space.IntersectRay(landQuery).Count > 0;
    }
}
