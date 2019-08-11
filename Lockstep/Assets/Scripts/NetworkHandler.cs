using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkHandler : MonoBehaviourPunCallbacks
{
    public System.Action JoinedRoom;

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");

        RoomOptions roomOptions = new RoomOptions
        {
            IsVisible = false,
            MaxPlayers = 4
        };

        PhotonNetwork.JoinOrCreateRoom("The room", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        print("Joined room");
        JoinedRoom?.Invoke();
    }
}
