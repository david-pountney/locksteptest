using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Text;

[Serializable]
public class CreateUnit : Action
{
    public string Uuid;
    public int Xpos;
    public int Zpos;
    public int Speed;
    public int Direction;
    
    public CreateUnit(int owningPlayer, int lockstepTurnID, string actionName, string uuid, int xpos, int zpos, int speed, int direction) : base(owningPlayer, lockstepTurnID, actionName)
    {
        Uuid = uuid;
        Debug.LogError("UUID HAS BEEN SET TO: " + uuid);
        Xpos = xpos;
        Zpos = zpos;
        Speed = speed;
        Direction = direction;
        
        //Debug.LogError("CreateUnit frame: " + gameFrame);
        //Debug.LogError("unitData Count: " + unitData.Length);


        //Debug.LogError("CreatUnit Action with Hash " + Hash);
    }

    public override void ProcessAction()
    {
        Debug.LogError("PRocss Action for createunit lockstep turn: " + LockstepTurnID );
        //if (ObjectPool.Instance.GetGameState().Length > 25) return;

        GameObject b = ObjectPool.Instance.GetBuilding(OwningPlayer);

        var move = b.GetComponent<Unit>();
        var data = move.UnitData;

        Debug.LogError("UUID IS STILL SET TO: " + Uuid);

        data.Uuid = Uuid;

        data.Direction = Direction;
        data.Speed = Speed;

        move.UnitData = data;
        
        //move.UnitData.Direction = Direction;
        //move.UnitData.Speed = Speed;

        move.SetUp();

        b.transform.position = new Vector3(Xpos, 0f, Zpos);
    }
}