namespace PaintPP.Core.Constraints;
using PaintPP.Core.Model;
using System.Numerics;

public class ContinuityConstraint : IConstraint
{
    private readonly ScenePoint _inPoint;
    private readonly float _inFactor;
    private readonly ScenePoint _outPoint;
    private readonly float _outFactor;

    public Continuity Kind { get; }
    public IReadOnlyList<Edge> Edges { get; }
    private readonly Vertex _vertex;

    public ContinuityConstraint(Vertex vertex, Edge incoming, Edge outgoing, Continuity kind)
    {
        _vertex = vertex;
        Kind = kind;
        Edges = [incoming, outgoing];

        if (incoming.Shape is BezierShape sh)
        {
            _inPoint = sh.C2;
            _inFactor = 3f;
        }
        else
        {
            _inPoint = incoming.Start;
            _inFactor = 1f;
        }

        if (outgoing.Shape is BezierShape bs)
        {
            _outPoint = bs.C1;
            _outFactor = 3f;
        }
        else
        {
            _outPoint = outgoing.End;
            _outFactor = 1f;
        }
    }

    private Vector2 VelocityIn() => _inFactor * (_vertex.Position - _inPoint.Position);
    private Vector2 VelocityOut() => _outFactor * (_outPoint.Position - _vertex.Position);
    public bool IsSatisfied(float epsilon)
    {
        var vIn = VelocityIn();
        var vOut = VelocityOut();
        if (vIn.Length() < 1e-5 || vOut.Length() < 1e-5)
        {
            return true;
        }
        if (Kind == Continuity.C1)
        {
            return Vector2.Distance(vIn, vOut) < epsilon;
        }
        else if (Kind == Continuity.G1)
        {
            return Vector2.Distance(Vector2.Normalize(vIn), Vector2.Normalize(vOut)) < epsilon;
        }
        return true;
    }
    public void Project(IReadOnlySet<ScenePoint> pinned)
    {
        bool outFree = !pinned.Contains(_outPoint);
        bool inFree = !pinned.Contains(_inPoint);

        if (outFree && _outFactor == 3)
            MoveOut();
        else if (inFree && _inFactor == 3)
            MoveIn();
        else if (outFree)
            MoveOut();
        else if (inFree)
            MoveIn();
    }
    private void MoveOut()
    {
        var vIn = VelocityIn();
        if (vIn.Length() < 1e-5)
            return;

        var dist = Vector2.Distance(_vertex.Position, _outPoint.Position);
        if (Kind == Continuity.C1)
        {
            _outPoint.Position = _vertex.Position + vIn / _outFactor;
        }
        else if (Kind == Continuity.G1)
        {
            _outPoint.Position = _vertex.Position + Vector2.Normalize(vIn) * dist;
        }
    }
    private void MoveIn()
    {
        var vOut = VelocityOut();
        if (vOut.Length() < 1e-5)
            return;

        var dist = Vector2.Distance(_vertex.Position, _inPoint.Position);
        if (Kind == Continuity.C1)
        {
            _inPoint.Position = _vertex.Position - vOut / _inFactor;
        }
        else if (Kind == Continuity.G1)
        {
            _inPoint.Position = _vertex.Position - Vector2.Normalize(vOut) * dist;
        }
    }
}
