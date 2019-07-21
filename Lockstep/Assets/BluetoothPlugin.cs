using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BluetoothPlugin : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            //StartPackage();
        }
    }

    void StartPackage()
    {
        AndroidJavaClass activityClass;
        AndroidJavaObject activity;

        activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");

        var bluetoothClass = new AndroidJavaClass("com.company.bluetoothplugin.BluetoothOptions");

        bluetoothClass.CallStatic("ShowBluetoothSettings", activity);
    }
}
