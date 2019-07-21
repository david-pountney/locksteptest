//The MIT License (MIT)

//Copyright (c) 2013 Clinton Brennan

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ConfirmedActions
{
	private bool[] confirmedCurrent;
	private bool[] confirmedPrior;
	
	private int confirmedCurrentCount;
	private int confirmedPriorCount;
	
	//Stop watches used to adjust lockstep turn length
	private Stopwatch currentSW;
	private Stopwatch priorSW;
	
	private LockstepController _lockstepController;
	
	public ConfirmedActions (LockstepController lockstepController)
	{
		this._lockstepController = lockstepController;
		confirmedCurrent = new bool[lockstepController.NumberOfPlayers];
		confirmedPrior = new bool[lockstepController.NumberOfPlayers];
		
		ResetArray(confirmedCurrent);
		ResetArray (confirmedPrior);
		
		confirmedCurrentCount = 0;
		confirmedPriorCount = 0;
		
		currentSW = new Stopwatch();
		priorSW = new Stopwatch();
	}
	
	public int GetPriorTime()
    {
		return ((int)priorSW.ElapsedMilliseconds);
	}
	
	public void StartTimer()
    {
		currentSW.Start ();
	}
	
	public void NextTurn()
    {
		//clear prior actions
		ResetArray(confirmedPrior);
		
		bool[] swap = confirmedPrior;
		Stopwatch swapSW = priorSW;
		
		//last turns actions is now this turns prior actions
		confirmedPrior = confirmedCurrent;
		confirmedPriorCount = confirmedCurrentCount;
		priorSW = currentSW;
		
		//set this turns confirmation actions to the empty array
		confirmedCurrent = swap;
		confirmedCurrentCount = 0;
		currentSW = swapSW;
		currentSW.Reset ();
	}
	
	public void ConfirmAction(int confirmingPlayerID, int currentLockStepTurn, int confirmedActionLockStepTurn) {
		if(confirmedActionLockStepTurn == currentLockStepTurn)
        {
			//if current turn, add to the current Turn Confirmation
			confirmedCurrent[confirmingPlayerID - 1] = true;
			confirmedCurrentCount++;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if(confirmedCurrentCount == _lockstepController.NumberOfPlayers)
            {
				currentSW.Stop ();
			}
		}
        else if(confirmedActionLockStepTurn == currentLockStepTurn -1)
        {
			//if confirmation for prior turn, add to the prior turn confirmation
			confirmedPrior[confirmingPlayerID - 1] = true;
			confirmedPriorCount++;
			//if we recieved the last confirmation, stop timer
			//this gives us the length of the longest roundtrip message
			if(confirmedPriorCount == _lockstepController.NumberOfPlayers)
            {
				priorSW.Stop ();
			}
		}
        else
        {
			//TODO: Error Handling
			Debug.Log("WARNING!!!! Unexpected lockstepID Confirmed : " + confirmedActionLockStepTurn + " from player: " + confirmingPlayerID);
		}
	}
	
	public bool ReadyForNextTurn()
    {
		//check that the action that is going to be processed has been confirmed
		if(confirmedPriorCount == _lockstepController.NumberOfPlayers)
        {
			return true;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID + 1)
        {
			return confirmedCurrentCount == _lockstepController.NumberOfPlayers;
		}
		//no action has been sent out prior to the first turn
		if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID)
        {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}
	
	public int[] WhosNotConfirmed()
    {
		//check that the action that is going to be processed has been confirmed
		if(confirmedPriorCount == _lockstepController.NumberOfPlayers)
        {
			return null;
		}
		//if 2nd turn, check that the 1st turns action has been confirmed
		if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID + 1)
        {
			if(confirmedCurrentCount == _lockstepController.NumberOfPlayers)
            {
				return null;
			}
            else
            {
				return WhosNotConfirmed (confirmedCurrent, confirmedCurrentCount);
			}
		}
		//no action has been sent out prior to the first turn
		if(_lockstepController.LockStepTurnID == LockstepController.FirstLockStepTurnID)
        {
			return null;
		}
		
		return WhosNotConfirmed (confirmedPrior, confirmedPriorCount);
	}
	
	/// <summary>
	/// Returns an array of player IDs of those players who have not
	/// confirmed are prior action.
	/// </summary>
	/// <returns>An array of not confirmed player IDs, or null if all players have confirmed</returns>
	private int[] WhosNotConfirmed(bool[] confirmed, int confirmedCount)
    {
		if(confirmedCount < _lockstepController.NumberOfPlayers)
        {
			//the number of "not confirmed" is the number of players minus the number of "confirmed"
			int[] notConfirmed = new int[_lockstepController.NumberOfPlayers - confirmedCount];
			int count = 0;
			//loop through each player and see who has not confirmed
			for(int playerID = 0; playerID < _lockstepController.NumberOfPlayers; playerID++)
            {
				if(!confirmed[playerID])
                {
					//add "not confirmed" player ID to the array
					notConfirmed[count] = playerID;
					count++;
				}
			}
			
			return notConfirmed;
		}
        else
        {
			return null;
		}
	}
	
	/// <summary>
	/// Sets every element of the boolean array to false
	/// </summary>
	/// <param name="a">The array to reset</param>
	private void ResetArray(bool[] a)
    {
		for(int i=0; i<a.Length; i++)
        {
			a[i] = false;
		}
	}
}