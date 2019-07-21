using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class LockstepController : MonoBehaviourPun
{
    public int LockStepTurnID { get => _lockStepTurnID; set => _lockStepTurnID = value; }
    public int NumberOfPlayers { get => _numberOfPlayers; set => _numberOfPlayers = value; }

    public static readonly int FirstLockStepTurnID = 0;

    [SerializeField] private int _numberOfPlayers;
    [SerializeField] private NetworkHandler _networkHandler;

    [SerializeField] private int _initialLockStepTurnLength = 200; //in Milliseconds
    [SerializeField] private int _initialGameFrameTurnLength = 50; //in Milliseconds

    [SerializeField] private UIDebug _uiDebug;
    
    private float _accumilatedTime = 0f;

    private float _frameLength = 0.05f; //50 miliseconds

    private int _gameFrame = 0; //Current Game Frame number in the currect lockstep turn
    
    private int _lockStepTurnID = FirstLockStepTurnID;
    
    //Variables for adjusting Lockstep and GameFrame length
    private RollingAverage _networkAverage;
    private RollingAverage _runtimeAverage;

    private long _currentGameFrameRuntime; //used to find the maximum gameframe runtime in the current lockstep turn
    private Stopwatch _gameTurnStopwatch;

    private int _lockstepTurnLength;
    private int _gameFrameTurnLength;
    private int _gameFramesPerLockstepTurn;
    
    private int _lockstepsPerSecond;
    private int _gameFramesPerSecond;

    private int playerIDToProcessFirst = 0; //used to rotate what player's action gets processed first

    private List<string> _readyPlayers;
    private List<string> _playersConfirmedImReady;

    private bool _initialized = false; //indicates if we are initialized and ready for game start
    
    private PendingActions _pendingActions;
    private ConfirmedActions _confirmedActions;

    private Queue<Action> _actionsToSend;

    private bool _gameStarted = false;

    private Dictionary<int, Action> _actionDictionary = new Dictionary<int, Action>();

    private bool _imSync = true;

    private void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Random.InitState(7);
        //Screen.SetResolution(1080, 540, true);
    }

    private void OnEnable()
    {
        _networkHandler.JoinedRoom += PrepGameStart;
    }

    private void OnDisable()
    {
        _networkHandler.JoinedRoom -= PrepGameStart;
    }

    public void CreateAction(Action action)
    {
        _actionsToSend.Enqueue(action);
    }
    
    private void PrepGameStart()
    {
        _lockStepTurnID = FirstLockStepTurnID;

        _numberOfPlayers = 2;

        _pendingActions = new PendingActions(this);
        _confirmedActions = new ConfirmedActions(this);
        _actionsToSend = new Queue<Action>();

        _gameTurnStopwatch = new Stopwatch();
        _currentGameFrameRuntime = 0;

        _networkAverage = new RollingAverage(_numberOfPlayers, _initialLockStepTurnLength);
        _runtimeAverage = new RollingAverage(_numberOfPlayers, _initialGameFrameTurnLength);

        InitGameStartLists();
        
        this.photonView.RPC("PlayerReadyToStart", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber.ToString());
    }

    public void InitGameStartLists()
    {
        if (_initialized) { return; }

        _readyPlayers = new List<string>(_numberOfPlayers);
        _playersConfirmedImReady = new List<string>(_numberOfPlayers);

        _initialized = true;
    }
    
    [PunRPC]
    public void PlayerReadyToStart(string playerID, PhotonMessageInfo info)
    {
        print("Player " + playerID + " is ready to start the game.");

        //make sure initialization has already happened -incase another player sends game start before we are ready to handle it
        InitGameStartLists();

        _readyPlayers.Add(playerID);

        if (PhotonNetwork.IsMasterClient)
        {
            //don't need an rpc call if we are the server
            ConfirmReadyToStartServer(/*confirmingPlayerID*/ PhotonNetwork.LocalPlayer.ActorNumber.ToString() /*confirmedPlayerID*/, playerID);
        }
        else
        {
            print("ConfirmReadyToStartServer");

            //nv.RPC("ConfirmReadyToStartServer", RPCMode.Server, Network.player.ToString() /*confirmingPlayerID*/, playerID /*confirmedPlayerID*/);
            this.photonView.RPC("ConfirmReadyToStartServer", RpcTarget.MasterClient/*confirmingPlayerID*/, PhotonNetwork.LocalPlayer.ActorNumber.ToString() /*confirmedPlayerID*/, playerID);
        }

        //Check if we can start the game
        CheckGameStart();
    }

    //Masterclient gets this message and broadcasts the confirmation to all players (only the one who has been confirmed will do anything)
    [PunRPC]
    public void ConfirmReadyToStartServer(string confirmingPlayerID, string confirmedPlayerID)
    {
        if (!PhotonNetwork.IsMasterClient) { return; } //workaround when multiple players running on same machine

        print("Server Message: Player " + confirmingPlayerID + " is confirming Player " + confirmedPlayerID + " is ready to start the game.");

        //validate ID
        //if (!gameSetup.players.ContainsKey(confirmingPlayerID))
        //if (!PhotonNetwork.)
        //{
        //    //TODO: error handling
        //    log.Debug("Server Message: WARNING!!! Unrecognized confirming playerID: " + confirmingPlayerID);
        //    return;
        //}
        //if (!gameSetup.players.ContainsKey(confirmedPlayerID))
        //{
        //    //TODO: error handling
        //    log.Debug("Server Message: WARNING!!! Unrecognized confirmed playerID: " + confirmingPlayerID);
        //}

        //relay message to confirmed client
        if (PhotonNetwork.LocalPlayer.ActorNumber.ToString().Equals(confirmedPlayerID))
        {
            //don't need an rpc call if we are the server
            ConfirmReadyToStart(confirmedPlayerID, confirmingPlayerID);
        }
        else
        {
            this.photonView.RPC("ConfirmReadyToStart", RpcTarget.OthersBuffered, confirmedPlayerID, confirmingPlayerID);
        }

    }

    //Confirmation by player who sent 'Ready' message. We check to see if it's the player who originally sent the message (The confirmedPlayerID)
    //The confirmedPlayer then storesthe confirming Player.
    [PunRPC]
    public void ConfirmReadyToStart(string confirmedPlayerID, string confirmingPlayerID)
    {
        if (!PhotonNetwork.LocalPlayer.ActorNumber.ToString().Equals(confirmedPlayerID)) { return; }

        print("Player " + confirmingPlayerID + " confirmed I am ready to start the game.");
        _playersConfirmedImReady.Add(confirmingPlayerID);

        //Check if we can start the game
        CheckGameStart();
    }

    private void CheckGameStart()
    {
        if (_playersConfirmedImReady == null)
        {
            print("WARNING!!! Unexpected null reference during game start. IsInit? " + _initialized);
            return;
        }
        //check if all expected players confirmed our gamestart message
        if (_playersConfirmedImReady.Count == _numberOfPlayers)
        {
            //check if all expected players sent their gamestart message
            if (_readyPlayers.Count == _numberOfPlayers)
            {
                //we are ready to start
                print("All players are ready to start. Starting Game.");

                //we no longer need these lists
                _playersConfirmedImReady = null;
                _readyPlayers = null;

                GameStart();
            }
        }
    }

    private void GameStart()
    {
        //start the LockStep Turn loop
        _gameStarted = true;
    }

    //called once per unity frame
    public void Update()
    {
        if (!_gameStarted)
            return;


        //Basically same logic as FixedUpdate, but we can scale it by adjusting FrameLength
        _accumilatedTime = _accumilatedTime + Time.deltaTime;

        //in case the FPS is too slow, we may need to update the game multiple times a frame
        while (_accumilatedTime > _frameLength)
        {
            GameFrameTurn();
            _accumilatedTime = _accumilatedTime - _frameLength;
        }
    }

    private void GameFrameTurn()
    {
        //first frame is used to process actions
        if (_gameFrame == 0)
        {
            if (LockStepTurn())
            {
                _gameFrame++;
            }
            else
                Debug.LogError("Failed lockstep turn");
        }
        else
        {
            //update game
            //SceneManager.Manager.TwoDPhysics.Update(GameFramesPerSecond);

            List<IHasGameFrame> finished = new List<IHasGameFrame>();
            foreach (IHasGameFrame obj in ObjectPool.Instance.GameFrameObjects)
            {
                obj.GameFrameTurn(_gameFramesPerSecond);
                if (obj.Finished)
                {
                    finished.Add(obj);
                }
            }

            foreach (IHasGameFrame obj in finished)
            {
                ObjectPool.Instance.GameFrameObjects.Remove(obj);
            }

            _gameFrame++;
            if (_gameFrame == _gameFramesPerLockstepTurn)
            {
                print("_gameFramesPerLockstepTurn: " + _gameFramesPerLockstepTurn);
                _gameFrame = 0;
            }

            //stop the stop watch, the gameframe turn is over
            _gameTurnStopwatch.Stop();
            //update only if it's larger - we will use the game frame that took the longest in this lockstep turn
            long runtime = Convert.ToInt32((Time.deltaTime * 1000))/*deltaTime is in secounds, convert to milliseconds*/ + _gameTurnStopwatch.ElapsedMilliseconds;
            if (runtime > _currentGameFrameRuntime)
            {
                _currentGameFrameRuntime = runtime;
            }
            //clear for the next frame
            _gameTurnStopwatch.Reset();
        }
    }

    private bool LockStepTurn()
    {
        //Check if we can proceed with the next turn
        bool nextTurn = NextTurn();
        
        if (nextTurn)
        {
            Debug.Log("LockStepTurnID: " + _lockStepTurnID + " Frame: " + _gameFrame);
            SendPendingAction();
            //the first and second lockstep turn will not be ready to process yet
            if (_lockStepTurnID >= FirstLockStepTurnID + 3)
            {
                ProcessActions();
            }
        }
        //otherwise wait another turn to recieve all input from all players

        UpdateGameFrameRate();
        return nextTurn;
    }

    private void SendPendingAction()
    {
        Action action = null;
        if (_actionsToSend.Count > 0)
        {
            Debug.LogError("_actionsToSend.Count: " + _actionsToSend.Count);
            action = _actionsToSend.Dequeue();
            Debug.LogError("_actionsToSend.Count: " + _actionsToSend.Count);

        }

        //if no action for this turn, send the NoAction action
        if (action == null)
        {
            action = new NoAction(PhotonNetwork.LocalPlayer.ActorNumber, LockStepTurnID, "NoAction");
            
        }

        action.LockstepTurnID = LockStepTurnID;

        _actionDictionary.Add(LockStepTurnID, action);

        if (_actionDictionary.Count >= 5)
        {
            //_actionDictionary.RemoveAt(0);
        }

        action.GameState = ObjectPool.Instance.GetGameState();

        Debug.LogError("--Local CreateUnit--");

        foreach (UnitData data in action.GameState)
        {
            Debug.LogError("UUID: " + data.Uuid);

            Debug.LogError("- lockstep turn: " + action.LockstepTurnID);

            Debug.LogError("Local ation x position: " + data.Xpos);
            Debug.LogError("Local ation z position: " + data.Zpos);
        }

        action.Hash = ObjectPool.Instance.BuildHash(action.GameState);

        //action.NetworkAverage = Network.GetLastPing (Network.connections[0/*host player*/]);
        if (_lockStepTurnID > FirstLockStepTurnID + 1)
        {
            action.NetworkAverage = _confirmedActions.GetPriorTime();
        }
        else
        {
            action.NetworkAverage = _initialLockStepTurnLength;
        }
        action.RuntimeAverage = Convert.ToInt32(_currentGameFrameRuntime);

        //clear the current runtime average
        _currentGameFrameRuntime = 0;

        //add action to our own list of actions to process
        _pendingActions.AddAction(action, PhotonNetwork.LocalPlayer.ActorNumber, _lockStepTurnID, _lockStepTurnID);

        //start the confirmed action timer for network average
        _confirmedActions.StartTimer();

        //confirm our own action
        _confirmedActions.ConfirmAction(PhotonNetwork.LocalPlayer.ActorNumber, _lockStepTurnID, _lockStepTurnID);

        //send action to all other players
        this.photonView.RPC("RecieveAction", RpcTarget.Others, LockStepTurnID, PhotonNetwork.LocalPlayer.ActorNumber.ToString(), BinarySerialization.SerializeObjectToByteArray(action));

        Debug.Log("Sent " + (action.GetType().Name) + " action for turn " + _lockStepTurnID);
    }

    [PunRPC]
    public void RecieveAction(int lockStepTurn, string playerID, byte[] actionAsBytes, PhotonMessageInfo info)
    {
        Debug.Log("Recieved Player " + playerID + "'s action for turn " + lockStepTurn + " on turn " + LockStepTurnID);
        Action action = BinarySerialization.DeserializeObject<Action>(actionAsBytes);
        if (action == null)
        {
            //TODO: Error handle invalid actions recieve
            Debug.LogError("Recieve action failed");
        }
        else
        {   
            if(action is CreateUnit)
            {
                Debug.LogError("--Remote CreateUnit--");
                //var localAction = _actionDictionary[action.LockstepTurnID];
                //int i = 0;
                foreach (UnitData data in action.GameState)
                {
                    //if(localAction.GameState[0].Xpos != data.Xpos)
                    //{
                    //    Debug.LogError("FAILED ERROR DUN FOR");
                    //}
                    Debug.LogError("- lockstep turn: " + action.LockstepTurnID);

                    Debug.LogError("remote UUID: " + data.Uuid);
                    Debug.LogError("Local ation x position: " + data.Xpos);
                    Debug.LogError("Local ation z position: " + data.Zpos);
                }
            }
            _pendingActions.AddAction(action, Convert.ToInt32(playerID), LockStepTurnID, lockStepTurn);

            //send confirmation
            if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.MasterClient.ActorNumber)
            {
                Debug.Log("Player " + PhotonNetwork.LocalPlayer.ActorNumber + " IS the master client, so confirm action ourselves");
                //we don't need an rpc call if we are the server
                ConfirmActionServer(lockStepTurn, PhotonNetwork.LocalPlayer.ActorNumber.ToString(), playerID);
            }
            else
            {
                //this.photonView.RPC("ConfirmActionServer", RpcTarget.MasterClient, lockStepTurn, Network.player.ToString(), playerID);
                Debug.Log("Player " + PhotonNetwork.LocalPlayer.ActorNumber + " IS NOT the master client, so send message to master client to confirm action");

                //ConfirmActionServer(lockStepTurn, PhotonNetwork.LocalPlayer.ActorNumber.ToString(), playerID);

                this.photonView.RPC("ConfirmActionServer", RpcTarget.MasterClient, lockStepTurn, PhotonNetwork.LocalPlayer.ActorNumber.ToString(), playerID);
            }
        }
    }

    [PunRPC]
    public void ConfirmActionServer(int lockStepTurn, string confirmingPlayerID, string confirmedPlayerID)
    {
        //if (!PhotonNetwork.IsMasterClient) { UnityEngine.Debug.LogError("is master - getting out"); return; } //Workaround - if server and client on same machine

        Debug.Log("ConfirmActionServer called turn:" + lockStepTurn + " playerID:" + confirmingPlayerID);
        Debug.Log("Sending Confirmation to player " + confirmedPlayerID);

        if (PhotonNetwork.LocalPlayer.ActorNumber.ToString().Equals(confirmedPlayerID))
        {
            //we don't need an RPC call if this is the server
            ConfirmAction(lockStepTurn, confirmingPlayerID);
        }
        else
        {
            var player = PhotonNetwork.PlayerList[Convert.ToInt32(confirmedPlayerID) - 1];

            this.photonView.RPC("ConfirmAction", player, lockStepTurn, confirmingPlayerID);
        }
    }

    [PunRPC]
    public void ConfirmAction(int lockStepTurn, string confirmingPlayerID)
    {
        Debug.Log("ConfirmAction: turn " + lockStepTurn + " confirmingPlayerID: " + confirmingPlayerID);

        _confirmedActions.ConfirmAction(Convert.ToInt32(confirmingPlayerID), LockStepTurnID, lockStepTurn);
    }

    private void ProcessActions()
    {
        Debug.Log("ProcessActions for lockstep turn: " + _lockStepTurnID + " for player " + PhotonNetwork.LocalPlayer.ActorNumber.ToString());

        //process action should be considered in runtime performance
        _gameTurnStopwatch.Start();

        //the first and second lockstep turn will not be ready to process yet
        if (_lockStepTurnID >= FirstLockStepTurnID + 3)
        {
            for (int i = 0; i < _pendingActions.CurrentActions.Length; i++)
            {
                _imSync = _pendingActions.CurrentActions[i].CheckIfInSync(_actionDictionary, _imSync);
                _uiDebug.SetSyncedText(_imSync);

            }
        }

        //Rotate the order the player actions are processed so there is no advantage given to
        //any one player
        for (int i = playerIDToProcessFirst; i < _pendingActions.CurrentActions.Length; i++)
        {
            _pendingActions.CurrentActions[i].ProcessAction();
            _runtimeAverage.Add(_pendingActions.CurrentActions[i].RuntimeAverage, i);
            _networkAverage.Add(_pendingActions.CurrentActions[i].NetworkAverage, i);
        }

        for (int i = 0; i < playerIDToProcessFirst; i++)
        {
            _pendingActions.CurrentActions[i].ProcessAction();
            _runtimeAverage.Add(_pendingActions.CurrentActions[i].RuntimeAverage, i);
            _networkAverage.Add(_pendingActions.CurrentActions[i].NetworkAverage, i);
        }

        playerIDToProcessFirst++;
        if (playerIDToProcessFirst >= _pendingActions.CurrentActions.Length)
        {
            playerIDToProcessFirst = 0;
        }
        
        //finished processing actions for this turn, stop the stopwatch
        _gameTurnStopwatch.Stop();
    }



    /// <summary>
    /// Check if the conditions are met to proceed to the next turn.
    /// If they are it will make the appropriate updates. Otherwise 
    /// it will return false.
    /// </summary>
    private bool NextTurn()
    {
        //		log.Debug ("Next Turn Check: Current Turn - " + LockStepTurnID);
        //		log.Debug ("    priorConfirmedCount - " + confirmedActions.playersConfirmedPriorAction.Count);
        //		log.Debug ("    currentConfirmedCount - " + confirmedActions.playersConfirmedCurrentAction.Count);
        //		log.Debug ("    allPlayerCurrentActionsCount - " + pendingActions.CurrentActions.Count);
        //		log.Debug ("    allPlayerNextActionsCount - " + pendingActions.NextActions.Count);
        //		log.Debug ("    allPlayerNextNextActionsCount - " + pendingActions.NextNextActions.Count);
        //		log.Debug ("    allPlayerNextNextNextActionsCount - " + pendingActions.NextNextNextActions.Count);

        //This players confirmed actions
        if (_confirmedActions.ReadyForNextTurn())
        {
            //Other players actions we have to confirm
            if (_pendingActions.ReadyForNextTurn())
            {
                //increment the turn ID
                _lockStepTurnID++;
                //move the confirmed actions to next turn
                _confirmedActions.NextTurn();
                //move the pending actions to this turn
                _pendingActions.NextTurn();

                return true;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Have not recieved player(s) actions: ");
                foreach (int i in _pendingActions.WhosNotReady())
                {
                    sb.Append(i + ", ");
                }
                print(sb.ToString());
            }
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Have not recieved confirmation from player(s): ");

            var whosNotReady = _pendingActions.WhosNotReady();
            if (whosNotReady != null)
            {
                foreach (int i in _pendingActions.WhosNotReady())
                {
                    sb.Append(i + ", ");
                }

                print(sb.ToString());
            }
        }

        if(_confirmedActions.ReadyForNextTurn() && _pendingActions.ReadyForNextTurn())
        {
        	//increment the turn ID
        	_lockStepTurnID++;
        	//move the confirmed actions to next turn
        	_confirmedActions.NextTurn();
        	//move the pending actions to this turn
        	_pendingActions.NextTurn();
        			
        	return true;
        }

        Debug.LogError("Can't precede with next turn");
        return false;
    }

    private void UpdateGameFrameRate()
    {
        //log.Debug ("Runtime Average is " + runtimeAverage.GetMax ());
        //log.Debug ("Network Average is " + networkAverage.GetMax ());
        _lockstepTurnLength = (_networkAverage.GetMax() * 2/*two round trips*/) + 1/*minimum of 1 ms*/;
        //_lockstepTurnLength = 200;
        _gameFrameTurnLength = _runtimeAverage.GetMax();
        //_gameFrameTurnLength = 50;

        if (_gameFrameTurnLength <= 10)
            _gameFrameTurnLength = 10;

        //lockstep turn has to be at least as long as one game frame
        if (_gameFrameTurnLength > _lockstepTurnLength)
        {
            _lockstepTurnLength = _gameFrameTurnLength;
        }

        //print("_lockstepTurnLength: " + _lockstepTurnLength);
        //print("_gameFrameTurnLength: " + _gameFrameTurnLength);

        _gameFramesPerLockstepTurn = _lockstepTurnLength / _gameFrameTurnLength;


        //print("_gameFramesPerLockstepTurn: " + _gameFramesPerLockstepTurn);
        
        //if gameframe turn length does not evenly divide the lockstep turn, there is extra time left after the last
        //game frame. Add one to the game frame turn length so it will consume it and recalculate the Lockstep turn length
        if (_lockstepTurnLength % _gameFrameTurnLength > 0)
        {
            _gameFrameTurnLength++;
            _lockstepTurnLength = _gameFramesPerLockstepTurn * _gameFrameTurnLength;
        }

        _lockstepsPerSecond = (1000 / _lockstepTurnLength);

        if (_lockstepsPerSecond == 0) { _lockstepsPerSecond = 1; } //minimum per second

        _gameFramesPerSecond = _lockstepsPerSecond * _gameFramesPerLockstepTurn;
        _uiDebug.SetGameFramesPerSecondText(_gameFramesPerSecond);
    }
}
