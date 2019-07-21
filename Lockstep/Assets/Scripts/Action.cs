using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public abstract class Action
{
    public int OwningPlayer;
    public string Hash;
    public UnitData[] GameState;
    public int LockstepTurnID;
    public string ActionName;

	public int NetworkAverage { get; set; }
	public int RuntimeAverage { get; set; }

    public Action(int owningPlayer, int lockstepTurnID, string actionName)
    {
        OwningPlayer = owningPlayer;
        LockstepTurnID = lockstepTurnID;
        ActionName = actionName;
    }

    public abstract void ProcessAction();
    
    public virtual bool CheckIfInSync(Dictionary<int, Action> actionDictionary, bool currentlyInSync)
    {
        if (!currentlyInSync) return false;

        string actionHash = string.Empty;

        //for(int i = 0; i < actionDictionary.Count; i++)
        {
            //if (actionDictionary[i].LockstepTurnID == this.LockstepTurnID)
            {
                //if (actionDictionary[i].OwningPlayer != this.OwningPlayer)
                {
                    var localUnitData = actionDictionary[LockstepTurnID].GameState;

                    //if (localUnitData != null && localUnitData.Length == 0) continue;
                    //if (this.LockstepTurnID < 5) continue;

                    for(int remoteUnitData = 0; remoteUnitData < GameState.Length; remoteUnitData++)
                    {
                        if(remoteUnitData >= GameState.Length)
                        {
                            Debug.LogError("bad times");
                            Debug.LogError("remoteUnitData: " + remoteUnitData);
                            Debug.LogError("GameState.Length: " + GameState.Length);

                            Debug.Break();
                        }

                        if (remoteUnitData >= localUnitData.Length)
                        {
                            Debug.LogError("bad times 2");
                            Debug.LogError("remoteUnitData: " + remoteUnitData);
                            Debug.LogError("localUnitData.Length: " + localUnitData.Length);

                            Debug.Break();
                        }

                        if (GameState[remoteUnitData].Xpos != localUnitData[remoteUnitData].Xpos)
                        {

                            Debug.LogError("XPos Desynced");
                            Debug.LogError("local UUID: " + localUnitData[remoteUnitData].Uuid);
                            Debug.LogError("remote UUID: " + GameState[remoteUnitData].Uuid);

                            Debug.LogError("ActionName: " + ActionName);
                            Debug.LogError("Lockstep turn: " + actionDictionary[LockstepTurnID].LockstepTurnID);
                            Debug.LogError("Remote lockstep turn: " + LockstepTurnID);
                            Debug.LogError("Local Xpos: " + localUnitData[remoteUnitData].Xpos);
                            Debug.LogError("Remote Xpos: " + GameState[remoteUnitData].Xpos);
                            currentlyInSync = false;
                        }
   
                        if (GameState[remoteUnitData].Zpos != localUnitData[remoteUnitData].Zpos)
                        {
                            Debug.LogError("Zpos Desynced");
                            Debug.LogError("Local Zpos: " + localUnitData[remoteUnitData].Zpos);
                            Debug.LogError("Remote Zpos: " + GameState[remoteUnitData].Zpos);
                            currentlyInSync = false;
                        }

                        if (GameState[remoteUnitData].Speed != localUnitData[remoteUnitData].Speed)
                        {
                            Debug.LogError("Speed Desynced");
                            Debug.LogError("Local Speed: " + localUnitData[remoteUnitData].Speed);
                            Debug.LogError("Remote Speed: " + GameState[remoteUnitData].Speed);
                            currentlyInSync = false;
                        }

                        if (GameState[remoteUnitData].Direction != localUnitData[remoteUnitData].Direction)
                        {
                            Debug.LogError("Direction Desynced");
                            Debug.LogError("Local Direction: " + localUnitData[remoteUnitData].Direction);
                            Debug.LogError("Remote Direction: " + GameState[remoteUnitData].Direction);
                            currentlyInSync = false;
                        }
                    }
                    
                    actionHash = actionDictionary[LockstepTurnID].Hash;
                    if (actionHash != Hash)
                    {
                        Debug.LogError("DESYNC FAILURE");
                        Debug.LogError("LocalHash: " + actionDictionary[LockstepTurnID].Hash);
                        Debug.LogError("PlayerActionHash: " + Hash);
                        
                        return false;
                    }
                    else
                    {
                        //Debug.LogWarning("Success!: " + actionDictionary[i].Hash + " " + Hash);
                        //Debug.LogWarning(this.OwningPlayer);

                        return true;
                    }

                }
            }
        }

        return true;

    }
}