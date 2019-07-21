using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDebug : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _gameFramesPerSecondText, _syncedText;

    public void SetGameFramesPerSecondText(int gameFramesPerSecondText)
    {
        _gameFramesPerSecondText.text = "GameFramesPerSecond: " + gameFramesPerSecondText;
    }

    public void SetSyncedText(bool synced)
    {
        _syncedText.text = "Synced: " + synced;
    }
}
