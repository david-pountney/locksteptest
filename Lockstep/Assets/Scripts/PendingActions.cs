using System;
using System.Collections.Generic;
using UnityEngine;

public class PendingActions
{
	public Action[] CurrentActions;
	private Action[] NextActions;
	private Action[] NextNextActions; 
	//incase other players advance to the next step and send their action before we advance a step
	private Action[] NextNextNextActions;
	
	private int currentActionsCount;
	private int nextActionsCount;
	private int nextNextActionsCount;
	private int nextNextNextActionsCount;
	
	private LockstepController _lockstepController;
	
	public PendingActions (LockstepController lockstepController)
    {
		this._lockstepController = lockstepController;
		
		CurrentActions = new Action[_lockstepController.NumberOfPlayers];
		NextActions = new Action[_lockstepController.NumberOfPlayers];
		NextNextActions = new Action[_lockstepController.NumberOfPlayers];
		NextNextNextActions = new Action[_lockstepController.NumberOfPlayers];
		
		currentActionsCount = 0;
		nextActionsCount = 0;
		nextNextActionsCount = 0;
		nextNextNextActionsCount = 0;
	}
	
	public void NextTurn()
    {
		//Finished processing this turns actions - clear it
		for(int i=0; i<CurrentActions.Length; i++)
        {
			CurrentActions[i] = null;
		}
		Action[] swap = CurrentActions;
		
		//last turn's actions is now this turn's actions
		CurrentActions = NextActions;
		currentActionsCount = nextActionsCount;
		
		//last turn's next next actions is now this turn's next actions
		NextActions = NextNextActions;
		nextActionsCount = nextNextActionsCount;
		
		NextNextActions = NextNextNextActions;
		nextNextActionsCount = nextNextNextActionsCount;
		
		//set NextNextNextActions to the empty list
		NextNextNextActions = swap;
		nextNextNextActionsCount = 0;
	}
	
	public void AddAction(Action action, int playerID, int currentLockStepTurn, int actionsLockStepTurn)
    {
		//add action for processing later
		if(actionsLockStepTurn == currentLockStepTurn + 1)
        {
			//if action is for next turn, add for processing 3 turns away
			if(NextNextNextActions[playerID - 1] != null)
            {
                //TODO: Error Handling
                UnityEngine.Debug.LogError("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextNextNextActions[playerID - 1] = action;
			nextNextNextActionsCount++;
		}
        else if(actionsLockStepTurn == currentLockStepTurn)
        {
            //if recieved action during our current turn
            //add for processing 2 turns away
			if(NextNextActions[playerID - 1] != null)
            {
                //TODO: Error Handling
                UnityEngine.Debug.LogError("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextNextActions[playerID - 1] = action;
			nextNextActionsCount++;
		}
        else if(actionsLockStepTurn == currentLockStepTurn - 1)
        {
			//if recieved action for last turn
			//add for processing 1 turn away
			if(NextActions[playerID - 1] != null)
            {
                //TODO: Error Handling
                UnityEngine.Debug.LogError ("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextActions[playerID - 1] = action;
			nextActionsCount++;
		}
        else
        {
            //TODO: Error Handling
            UnityEngine.Debug.LogError("WARNING!!!! Unexpected lockstepID recieved : " + actionsLockStepTurn);
			return;
		}
	}
	
	public bool ReadyForNextTurn()
    {
		if(nextNextActionsCount == _lockstepController.NumberOfPlayers)
        {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID + 1)
            {
				return true;
			}
			
			//Check if all Actions that will be processed next turn have been recieved
			if(nextActionsCount == _lockstepController.NumberOfPlayers)
            {
				return true;
			}
		}
		
		//if this is the 1st turn, no actions had the chance to be recieved yet
		if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID)
        {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}
	
	public int[] WhosNotReady()
    {

		if(nextNextActionsCount == _lockstepController.NumberOfPlayers)
        {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID + 1)
            {
				return null;
			}
			
			//Check if all Actions that will be processed next turn have been recieved
			if(nextActionsCount == _lockstepController.NumberOfPlayers)
            {
				return null;
			}
            else
            {
				return WhosNotReady (NextActions, nextActionsCount);
			}
			
		}
        else if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID)
        {
            //UnityEngine.Debug.LogError("WhosNotReady returns null");

            //if this is the 1st turn, no actions had the chance to be recieved yet
            return null;
		}
        else
        {
            //UnityEngine.Debug.LogError("WhosNotReady going in");

            return WhosNotReady (NextNextActions, nextNextActionsCount);
		}
	}
	
	private int[] WhosNotReady(Action[] actions, int count)
    {
		if(count < _lockstepController.NumberOfPlayers)
        {
			int[] notReadyPlayers = new int[_lockstepController.NumberOfPlayers - count];
			
			int index = 0;
			for(int playerID = 0; playerID < _lockstepController.NumberOfPlayers; playerID++)
            {
				if(actions[playerID] == null)
                {
					notReadyPlayers[index] = playerID;
					index++;
				}
			}
			
			return notReadyPlayers;
		}
        else
        {
			return null;
		}
	}
}