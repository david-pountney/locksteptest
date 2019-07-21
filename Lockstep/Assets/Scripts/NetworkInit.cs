using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInit : MonoBehaviour
{
    private void Awake()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
}
