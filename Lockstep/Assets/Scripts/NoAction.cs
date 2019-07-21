using System;

[Serializable]
public class NoAction : Action
{
    public NoAction(int owningPlayer, int lockstepTurnID, string actionName) : base(owningPlayer, lockstepTurnID, actionName)
    {
        
    }

    public override void ProcessAction() {}
}