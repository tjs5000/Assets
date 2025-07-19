using System;
using System.Collections.Generic;

public enum NodeStatus { Success, Failure, Running }

public abstract class BTNode
{
    public abstract NodeStatus Tick();
}

public class Selector : BTNode
{
    private readonly List<BTNode> _children;
    public Selector(params BTNode[] children) => _children = new List<BTNode>(children);
    public override NodeStatus Tick()
    {
        foreach (var c in _children)
        {
            var s = c.Tick();
            if (s != NodeStatus.Failure) return s;
        }
        return NodeStatus.Failure;
    }
}

public class Sequence : BTNode
{
    private readonly List<BTNode> _children;
    public Sequence(params BTNode[] children) => _children = new List<BTNode>(children);
    public override NodeStatus Tick()
    {
        foreach (var c in _children)
        {
            var s = c.Tick();
            if (s != NodeStatus.Success) return s;
        }
        return NodeStatus.Success;
    }
}

public class ConditionNode : BTNode
{
    private readonly Func<bool> _cond;
    public ConditionNode(Func<bool> cond) => _cond = cond;
    public override NodeStatus Tick() => _cond() ? NodeStatus.Success : NodeStatus.Failure;
}

public class ActionNode : BTNode
{
    private readonly Func<NodeStatus> _act;
    public ActionNode(Func<NodeStatus> act) => _act = act;
    public override NodeStatus Tick() => _act();
}
