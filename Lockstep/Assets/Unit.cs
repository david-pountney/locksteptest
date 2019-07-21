using Lockstep;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UnitData
{
    public string Uuid;

    public int Direction;
    public int Speed;

    public double Xpos;
    public double Zpos;
}

public class Unit : MonoBehaviour, IHasGameFrame
{
    public UnitData UnitData { get => _unitData; set => _unitData = value; }

    public bool Finished { get => _finished; set => _finished = value; }

    [SerializeField] private UnitData _unitData;
    private bool _finished = false;
    
    public void SetUp()
    {
        this.transform.rotation = Quaternion.Euler(0f, _unitData.Direction, 0f);
    }

    public void GameFrameTurn(int gameFramesPerSecond)
    {
        long positionX = FixedMath.Create(transform.position.x);
        long positionZ = FixedMath.Create(transform.position.z);

        long forwardX = FixedMath.Create(transform.forward.x);
        long forwardZ = FixedMath.Create(transform.forward.z);
        
        //var lDirection = new Vector3d(FixedMath.Trig.Sin(FixedMath.Trig.Deg2Rad(fp_direction)), 0, FixedMath.Trig.Cos(FixedMath.Trig.Deg2Rad(fp_direction)));
        var dispositionX = positionX.Add(forwardX);
        var dispositionZ = positionZ.Add(forwardZ);
        var realX = dispositionX.ToFloat();
        var realZ = dispositionZ.ToFloat();

        var vector = new Vector3(realX, 0, realZ);

        this.transform.position = vector;

        if (this.transform.position.x >= 500 || this.transform.position.z >= 500 || this.transform.position.z <= -500 || this.transform.position.x <= -500)
        {
            _unitData.Direction += 180;
            this.transform.rotation = Quaternion.Euler(0f, _unitData.Direction, 0f);
            //this.transform.eulerAngles = new Vector3(0, _unitData.Direction, 0);

        }
        //Debug.LogError("SETTING UNIT POS X: " + this.transform.position.x);
        //Debug.LogError("SETTING UNIT POS Z: " + this.transform.position.z);
    }

    public void GameFrameTurn(object gameFramesPerSecond)
    {
        throw new NotImplementedException();
    }
}
