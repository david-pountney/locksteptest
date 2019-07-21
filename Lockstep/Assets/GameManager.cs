using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private LockstepController _lockstepController; 

    private void Update()
    {
        //if (Input.GetKeyUp(KeyCode.A))
        if (Input.GetMouseButtonUp(0))
        {
            //var xpos = Random.Range(-100, 100);
            var xpos = 50;
            //var zpos = Random.Range(-100, 100);
            var zpos = 50;

            //var speed = Random.Range(4, 6);
            var speed = 5;
            var direction = Random.Range(0, 359);
            //var direction = 45;

            var action = new CreateUnit(PhotonNetwork.LocalPlayer.ActorNumber,
                                        _lockstepController.LockStepTurnID,
                                        "CreateUnit",
                                        System.Guid.NewGuid().ToString(),
                                        xpos,
                                        zpos,
                                        speed,
                                        direction
                                        );

            _lockstepController.CreateAction(action);
        }
    }
}
