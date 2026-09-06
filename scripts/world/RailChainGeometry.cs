using Godot;
using System.Collections.Generic;

/// <summary>
/// Builds rail chains that keep a constant PERPENDICULAR clearance from the floor
/// polyline, so steep segments do not visually hug the ground the way a fixed
/// vertical offset does.
///
/// Each rail rides one floor segment between two joints. A joint sits on the
/// bisector of the two segments' up-normals at the shared vertex, at distance
/// clearance/cos(dTheta/2) from it - the exact point whose perpendicular distance
/// to both offset lines is the clearance.
/// </summary>
public static class RailChainGeometry
{
    public struct RailLayout
    {
        public Vector2 Center;
        public float Rotation;
        public float Length;
    }

    public static List<RailLayout> Build(FloorSegment[] segments, int startSegment, int endSegment, float clearance)
    {
        var layouts = new List<RailLayout>();
        if (segments == null || startSegment < 0 || endSegment >= segments.Length || startSegment > endSegment)
            return layouts;

        var jointCount = endSegment - startSegment + 2;
        var joints = new Vector2[jointCount];
        for (var i = 0; i < jointCount; i++)
        {
            var rightIndex = startSegment + i;
            if (rightIndex == 0)
            {
                joints[i] = new Vector2(segments[0].StartX, segments[0].StartY) + UpNormal(segments[0]) * clearance;
                continue;
            }

            var vertex = new Vector2(segments[rightIndex].StartX, segments[rightIndex].StartY);
            joints[i] = Joint(vertex, UpNormal(segments[rightIndex - 1]), UpNormal(segments[rightIndex]), clearance);
        }

        for (var k = startSegment; k <= endSegment; k++)
        {
            var a = joints[k - startSegment];
            var b = joints[k - startSegment + 1];
            var delta = b - a;
            layouts.Add(new RailLayout
            {
                Center = (a + b) * 0.5f,
                Rotation = delta.Angle(),
                Length = delta.Length(),
            });
        }

        return layouts;
    }

    private static Vector2 Joint(Vector2 vertex, Vector2 normalA, Vector2 normalB, float clearance)
    {
        var nA = normalA.Normalized();
        var nB = normalB.Normalized();
        var cosHalf = Mathf.Sqrt(Mathf.Clamp((1f + Mathf.Clamp(nA.Dot(nB), -1f, 1f)) * 0.5f, 0f, 1f));
        var bisector = nA + nB;
        if (cosHalf < 0.0001f || bisector.LengthSquared() < 0.0001f)
            return vertex + nA * clearance;

        return vertex + bisector.Normalized() * (clearance / cosHalf);
    }

    private static Vector2 UpNormal(FloorSegment segment)
    {
        var dir = new Vector2(segment.EndX - segment.StartX, segment.EndY - segment.StartY).Normalized();
        return new Vector2(dir.Y, -dir.X);
    }
}
