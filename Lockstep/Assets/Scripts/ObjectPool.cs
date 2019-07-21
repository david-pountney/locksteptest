using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ObjectPool : MonoBehaviour
{ 
	public static ObjectPool Instance;

    [SerializeField] private GameObject _building;
    [SerializeField] private GameObject _units;
    
    public string GameStateHash = ""; 

	public List<IHasGameFrame> GameFrameObjects;

    private int xpos = 0;
    private int zpos = 0;

    void Awake()
    {
		Instance = this;
		GameFrameObjects = new List<IHasGameFrame>();
	}

    public UnitData[] GetGameState()
    {
        var collection = new List<UnitData>();
        var units = this.GetComponentsInChildren<Unit>();

        if(units.Length > 0)
        {
            xpos++;
            zpos++;
        }

        for (int i = 0; i < units.Length; i++)
        {
            var unitData = units[i].UnitData;

            //Debug.LogWarning("");

            var newUnitData = new UnitData();
            newUnitData.Xpos = units[i].transform.position.x;
            newUnitData.Zpos = units[i].transform.position.z;
            newUnitData.Direction = units[i].UnitData.Direction;
            newUnitData.Speed = units[i].UnitData.Speed;
            
            //unitData.Xpos = units[i].transform.position.x;
            //unitData.Zpos = units[i].transform.position.z;

            //unitData.Direction = units[i].transform.eulerAngles.y;

            collection.Add(newUnitData);
        }
        
        return collection.ToArray();
    }

    public string BuildHash(UnitData[] unitData)
    {
        if (unitData == null) return string.Empty;
        if (unitData.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < unitData.Length; ++i)
        {
            //Debug.LogError("unitData[i].Xpos: " + unitData[i].Xpos);
            //Debug.LogError("unitData[i].Zpos: " + unitData[i].Zpos);
            //Debug.LogError("unitData[i].Direction: " + unitData[i].Direction);
            //Debug.LogError("unitData[i].Speed: " + unitData[i].Speed);
            
            sb.Append(unitData[i].Xpos);
            sb.Append(unitData[i].Zpos);
            sb.Append(unitData[i].Direction);
            sb.Append(unitData[i].Speed);
        }

        Debug.Log(sb.ToString());

        return StaticFunctions.MD5Hash(sb.ToString());
    }

    public GameObject GetBuilding(int owningPlayer)
    {
        var instance = Instantiate<GameObject>(_building, Vector3.zero, Quaternion.identity, _units.transform);

        var gameFrame = instance.GetComponent<IHasGameFrame>();
        GameFrameObjects.Add(gameFrame);

        return instance;
    }
}
