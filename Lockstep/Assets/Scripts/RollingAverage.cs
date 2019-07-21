using UnityEngine;

public class RollingAverage
{
	public int[] currentValues; //used only for logging
	
	private int[] playerAverages;
	public RollingAverage(int numofPlayers, int initValue)
    {
		playerAverages = new int[numofPlayers];
		currentValues = new int[numofPlayers];
		for(int i=0; i<numofPlayers; i++)
        {
			playerAverages[i] = initValue;
			currentValues[i] = initValue;
		}
	}
	
	public void Add(int newValue, int playerID)
    {
		if(newValue > playerAverages[playerID])
        {
			//rise quickly
			playerAverages[playerID] = newValue;
		}
        else
        {
			//slowly fall down
			playerAverages[playerID] = (playerAverages[playerID] * (9) + newValue * (1)) / 10;
		}
		
		currentValues[playerID] = newValue;
	}
	
	public int GetMax()
    {
		int max = int.MinValue;
		foreach(int average in playerAverages)
        {
			if(average > max)
            {
				max = average;
			}
		}
		
		return max;
	}
}
