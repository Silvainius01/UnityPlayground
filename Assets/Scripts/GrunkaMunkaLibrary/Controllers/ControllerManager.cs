//using Rewired;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;

//public enum CONTROL_TYPE { NONE, CONTROLLER, KBM, BRAIN }
//public enum CONTROL_SCHEME { DEFAULT }
//public enum INPUT_CATEGORY { NOT_JOINED, GAME }
//public enum DISCONNECT_STRATEGY { WAIT_FOR_RECONNECT_MAIN_ONLY, WAIT_FOR_RECONNECT_ALL, ALERT_AND_CONTINUE }

//public class ControllerManager : MonoBehaviour
//{
//    /* Singleton class that supports the following:
//        - Will create an instance if requested an not in the scene
//        - Will remove any duplicate instances
//        - Has global access
//        - Is Kept across different scenes
//    */

//    private static ControllerManager _instance;
//    private static bool applicationExiting = false;
//    public bool directlyMapControllersToPlayers { get; private set; }

//    // player num is index, player controller id is value (value of -1 is unassigned)
//    private PlayerInput[] playerInputMap = new PlayerInput[4];
//    public PlayerInput mainPlayer { get; private set; }
//    public int numPlayersAssigned { get; private set; }

//    // fake player that provides no active inputs
//    public PlayerInput nullPlayer { get; private set; }
//    public static Rewired.Player rwNullPlayer { get; private set; }

//    // controller callbacks
//    public delegate void MainPlayerSetCallback(PlayerInput playerInput);
//    private List<MainPlayerSetCallback> mainPlayerSetCallbacks = new List<MainPlayerSetCallback>();

//    public delegate void PlayerConnectionCallback(PlayerInput playerInput);
//    private List<PlayerConnectionCallback> playerReconnectedCallbacks = new List<PlayerConnectionCallback>();
//    private List<PlayerConnectionCallback> playerDisconnectedCallbacks = new List<PlayerConnectionCallback>();
//    public delegate void ControllerConnectedCallback(Joystick joystick, Rewired.Player rwPlayer);
//    private List<ControllerConnectedCallback> controllerConnectedAndAssignedCallbacks = new List<ControllerConnectedCallback>();

//    public delegate void HandheldModeChangedCallback(bool enabled);
//    private List<HandheldModeChangedCallback> handheldModeChangedCallbacks = new List<HandheldModeChangedCallback>();


//    // to splash callback
//    public delegate void ToSplashCallback();
//    private List<ToSplashCallback> toSplashCallbacks = new List<ToSplashCallback>();

//    Dictionary<int, Dictionary<int, bool>> rwPlayerSavedControllerMapStates = new Dictionary<int, Dictionary<int, bool>>();
//	LogicLocker vibrationLocker = new LogicLocker();

//	public DISCONNECT_STRATEGY disconnectStrategy { get; private set; }
//    private int layoutID = 0;
//    private const float timedAlertDuraction = 5.0f;

//    // controller disbling
//    private LogicLocker disableControllersLocker = new LogicLocker();
//    public bool disableAllRewiredControllers { get; private set; }
//    public bool disableAllNonPlayingPlayers { get; private set; }

//	///////////////// FNCTS /////////////////

//	public static ControllerManager instance
//    {
//        get
//        {
//            if (_instance == null && !applicationExiting)
//            {
//                // if no instance of object, create it
//                GameObject obj = new GameObject("Controller Manager");
//                _instance = obj.AddComponent<ControllerManager>();
//            }
//            return _instance;
//        }
//    }

//    public void Awake()
//    {
//        if (_instance != null && _instance != this)
//        {
//            // if obj is a duplicate, destroy it
//            Destroy(gameObject);
//            return;
//        }
//        else
//        {
//            _instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        disableAllRewiredControllers = false;

//        UpdateControllerLayouts();

//        // set no players assigned
//        numPlayersAssigned = 0;

//        // get the rewired null player (doesn't have inputs)
//        rwNullPlayer = ReInput.players.GetPlayer(RewiredConsts.Player.NullPlayer);
//        nullPlayer = CreatePlayerInputObj("Null Player", -1);

//        for (int i = 0; i < playerInputMap.Length; ++i)
//            playerInputMap[i] = CreatePlayerInputObj("Player " + i, i);

//        ClearMainPlayer();

//        // set controller mapping based on platform
//        directlyMapControllersToPlayers = false;
//        // register for controller connect / disconnect events
//        ReInput.ControllerConnectedEvent += OnControllerConnectedDefault;
//        ReInput.ControllerDisconnectedEvent += OnControllerDisconnectedDefault;
//        ReInput.ControllerPreDisconnectEvent += OnControllerPreDisconnectDefault;

//        // set disconnect strategy to defualt to wait for main player only
//        SetDisconnectStrategy(DISCONNECT_STRATEGY.WAIT_FOR_RECONNECT_MAIN_ONLY);
//    }

//    public void UpdateControllerLayouts()
//    {
//        // iterate through all initial players and disable incorrect platform layouts
//        for (int i = 0; i < ReInput.players.allPlayerCount; i++)
//        {
//            Player player = ReInput.players.AllPlayers[i];
//            player.controllers.maps.ClearMapsInLayout(ControllerType.Joystick, 1, false);
//        }
//    }

//    #region ControllerConnectEvents
//    void OnControllerConnectedDefault(ControllerStatusChangedEventArgs args)
//    {
//        Debug.Log("controller connect");
//        var controller = ReInput.controllers.GetController(args.controllerType, args.controllerId);
//        var rw = GetRwPlayerWithJoystick(ReInput.controllers.GetJoystick(controller.id));
//        string rwPlayerString = " rw: " + (rw != null ? rw.id.ToString() : "none");
//        if (args.controllerType != ControllerType.Joystick)
//        {
//            controller.enabled = false;
//            return;
//        }

//        // if controller connects while all controllers disabled, disable it
//        controller.enabled = !disableAllNonPlayingPlayers;
//        var joystick = ReInput.controllers.GetJoystick(controller.id);

//        // check for controller reconnect for player
//        bool newController = true;
//        foreach (var playerInput in playerInputMap)
//        {
//            if (playerInput.IsSet() && playerInput.rwPlayer.controllers.ContainsController(args.controllerType, args.controllerId))
//            {
//                // hide the current disconnect alert
//                SendReconnectionMessage(playerInput);
//                newController = false;

//                // show reconnect alert

//                break;
//            }
//        }

//        if (newController)
//        {
            
//        }
//        UpdateControllerLayouts();
//    }

//    void OnControllerDisconnectedDefault(ControllerStatusChangedEventArgs args)
//    {
//		Debug.Log("controller disconnect");
//	}

//    void OnControllerPreDisconnectDefault(ControllerStatusChangedEventArgs args)
//    {
//        Debug.Log("controller predisconnect");
//        var controller = ReInput.controllers.GetController(args.controllerType, args.controllerId);
//        var rw = GetRwPlayerWithJoystick(ReInput.controllers.GetJoystick(controller.id));
//        string rwPlayerString = " rw: " + (rw != null ? rw.id.ToString() : "none");
//        if (args.controllerType != ControllerType.Joystick) return;

//        var joystick = ReInput.controllers.GetJoystick(controller.id);

//        // check for disconnect on assigned players
//        foreach (var playerInput in playerInputMap)
//        {
//            // check if playing player player is assigned the controller
//            if (playerInput.IsSet() && args.controllerType == ControllerType.Joystick && playerInput.rwPlayer.controllers.ContainsController(args.controllerType, args.controllerId))
//            {
//                Rewired.Player rwPlayer = playerInput.rwPlayer;

//                // send disconnect message to listeners
//                SendDisconnectedMessage(playerInput);

//                switch (disconnectStrategy)
//                {
//                    case DISCONNECT_STRATEGY.WAIT_FOR_RECONNECT_MAIN_ONLY:
//                        break;
//                    case DISCONNECT_STRATEGY.WAIT_FOR_RECONNECT_ALL:
//                        break;
//                    case DISCONNECT_STRATEGY.ALERT_AND_CONTINUE:
//                        break;
//                }
//                break;
//            }
//        }
//    }

//    public void SetDisconnectStrategy(DISCONNECT_STRATEGY strategy)
//    {
//        // if disconnect strategy changed
//        if(disconnectStrategy != strategy)
//        {
//            // set to new strategy
//            disconnectStrategy = strategy;
//            RefreshDisconnectAlerts();
//        }
//    }

//    public void ClearControllerAlerts()
//    {
        
//    }

//    public void RefreshDisconnectAlerts()
//    {
        
//    }

//    public void RegisterReconnectedListener(PlayerConnectionCallback listener)
//    {
//        if (!playerReconnectedCallbacks.Contains(listener))
//            playerReconnectedCallbacks.Add(listener);
//    }

//    public void UnRegisterReconnectedListener(PlayerConnectionCallback listener)
//    {
//        playerReconnectedCallbacks.Remove(listener);
//    }

//    public void SendReconnectionMessage(PlayerInput playerInput)
//    {
//        foreach (var cb in playerReconnectedCallbacks)
//        {
//            cb(playerInput);
//        }
//    }

//    public void RegisterDisconnectedListener(PlayerConnectionCallback listener)
//    {
//        if (!playerDisconnectedCallbacks.Contains(listener))
//            playerDisconnectedCallbacks.Add(listener);
//    }

//    public void UnRegisterDisconnectedListener(PlayerConnectionCallback listener)
//    {
//        playerDisconnectedCallbacks.Remove(listener);
//    }

//    public void SendDisconnectedMessage(PlayerInput playerInput)
//    {
//        foreach (var cb in playerDisconnectedCallbacks)
//        {
//            cb(playerInput);
//        }
//    }

//    public void RegisterControllerConnectedAndAssignedListener(ControllerConnectedCallback listener)
//    {
//        if (!controllerConnectedAndAssignedCallbacks.Contains(listener))
//            controllerConnectedAndAssignedCallbacks.Add(listener);
//    }

//    public void UnRegisterControllerConnectedAndAssignedListener(ControllerConnectedCallback listener)
//    {
//        controllerConnectedAndAssignedCallbacks.Remove(listener);
//    }

//    public void SendControllerConnectedAndAssignedMessage(Joystick joystick, Rewired.Player rwPlayer)
//    {
//        foreach (var cb in controllerConnectedAndAssignedCallbacks)
//        {
//            cb(joystick, rwPlayer);
//        }
//    }
//#endregion

//#region CONTROLLER ALERT FCNS

//    public void AlertTimedOut()
//    {
//    }

//#endregion

//    public PlayerInput CreatePlayerInputObj(string name, int playerNum)
//    {
//        GameObject obj = new GameObject(name);
//        obj.transform.SetParent(transform);
//        var playerInput = obj.AddComponent<PlayerInput>();
//        playerInput.Init(playerNum);
//        return playerInput;
//    }

//    public void RegisterMainPlayerSetListener(MainPlayerSetCallback listener)
//    {
//        if(!mainPlayerSetCallbacks.Contains(listener))
//            mainPlayerSetCallbacks.Add(listener);
//    }

//    public void UnRegisterMainPlayerSetListener(MainPlayerSetCallback listener)
//    {
//        mainPlayerSetCallbacks.Remove(listener);
//    }

//    public void SendMainPlayerSetMessage()
//    {
//        foreach (var cb in mainPlayerSetCallbacks)
//        {
//            cb(mainPlayer);
//        }
//    }

//    public void RegisterToSplashListener(ToSplashCallback listener)
//    {
//        if (!toSplashCallbacks.Contains(listener))
//            toSplashCallbacks.Add(listener);
//    }

//    public void UnRegisterToSplashListener(ToSplashCallback listener)
//    {
//        toSplashCallbacks.Remove(listener);
//    }

//    public void SendToSplashMessage()
//    {
//        foreach (var cb in toSplashCallbacks)
//        {
//            cb();
//        }
//    }

//    // assign the new available in game player the rewired player
//    public PlayerInput AssignPlayer(Rewired.Player rwPlayer)
//    {
//        if (!directlyMapControllersToPlayers)
//        {
//            // if rw players not directly mapped to player, assign rwPlayer to next available player
//            var pos = GetNextFreePosition();
//            if(pos != -1)
//                return AssignPlayer(pos, rwPlayer);
//        }
//        else
//        {
//            // if direct player to rw player mapping, assign to same id
//            return AssignPlayer(rwPlayer.id, rwPlayer);
//        }
//        return null;
//    }

//    // assigns an rewired player to an in game player
//    public PlayerInput AssignPlayer(int playerNum, Rewired.Player rwPlayer)
//    {
//        if (!IsValidPlayerNum(playerNum) || IsControllerInUse(rwPlayer)) return null;
//        else if(directlyMapControllersToPlayers && playerNum != rwPlayer.id)
//        {
//            // if in direct mapping mode and player id isn't same as rw player id return null
//            return null;
//        }

//        if (!playerInputMap[playerNum].IsSet())
//        {
//            // increment number of players assigned
//            ++numPlayersAssigned;

//            playerInputMap[playerNum].SetAsPlayer(rwPlayer);

//            // disable assignment map category in rwPlayer 
//            SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Default, false); 

//            return playerInputMap[playerNum];
//        }
//        return null;
//    }

//    // assign AI to a player slot
//    public PlayerInput AssignAI(int playerNum)
//    {
//        if (!IsValidPlayerNum(playerNum) || playerInputMap[playerNum].IsSet()) return null;

//        // increment number of players assigned
//        ++numPlayersAssigned;

//        playerInputMap[playerNum].SetAsAI();

//        return playerInputMap[playerNum];
//    }

//    public void ResetAssignedPlayers(bool ignoreMainPlayer = false)
//    {
//        // reset all assigned controllers, with option to not remove the main player
//        for(int i = 0; i < playerInputMap.Length; ++i)
//        {
//            var player = GetPlayerInput(i);
//            if(player.IsSet() && (!ignoreMainPlayer || !player.isMainPlayer))
//            {
//                RemovePlayerAssignment(i);
//            }
//        }
//    }

//    // remove either a set player or set ai
//    public bool RemovePlayerAssignment(int playerNum)
//    {
//        if (!IsValidPlayerNum(playerNum)) return false;
//        else if (playerInputMap[playerNum].IsSet())
//        {
//            // if is main player, clear main player
//            if (playerInputMap[playerNum].isMainPlayer)
//            {
//                ClearMainPlayer();
//            }

//            // decrement number of players assigned
//            --numPlayersAssigned;

//            // clear the player
//            playerInputMap[playerNum].Clear();
//            return true;
//        }

//        return false;
//    }

//    public List<Rewired.Player> GetPlayersAttemptingToJoin()
//    {
//        List<Rewired.Player> rwPlayers = new List<Rewired.Player>(ReInput.players.playerCount);
//        for (int i = 0; i < ReInput.players.playerCount; ++i)
//        {
//            var rwPlayer = ReInput.players.GetPlayer(i);

//            // if a rw player isn't joined, make sure their assignement is active
//            if (GetControllerIndex(rwPlayer) == -1)
//                SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Default, true);
//            if (rwPlayer.GetButtonDown(RewiredConsts.Action.Assignment))
//            {
//                rwPlayers.Add(rwPlayer);
//            }
//        }
//        return rwPlayers;
//    }

//    public List<Rewired.Player> GetAllUnjoinedRwPlayers()
//    {
//        List<Rewired.Player> rwPlayers = new List<Rewired.Player>(ReInput.players.playerCount);
//        for (int i = 0; i < ReInput.players.playerCount; ++i)
//        {
//            var player = ReInput.players.GetPlayer(i);
//            if (GetControllerIndex(player) == -1)
//            {
//                // if a rw player isn't joined, make sure their assignement is active
//                SetPlayerMapEnabled(player, RewiredConsts.Category.Default, true);
//                rwPlayers.Add(player);
//            }
//        }
//        return rwPlayers;
//    }

//    public bool SetMainPlayer(int playerNum)
//    {
//        if (!IsValidPlayerNum(playerNum)) return false;

//        foreach(var player in playerInputMap)
//        {
//            player.isMainPlayer = player.gamePlayerId == playerNum;
//        }
//        mainPlayer = playerInputMap[playerNum];

//        SendMainPlayerSetMessage();
//        return true;
//    }

//    public void ClearMainPlayer()
//    {
//        if(mainPlayer != null)
//            mainPlayer.isMainPlayer = false;
//        mainPlayer = nullPlayer;

//        SendMainPlayerSetMessage();
//    }

//    public bool IsValidPlayerNum(int playerNum)
//    {
//        return playerNum >= 0 && playerNum < playerInputMap.Length;
//    }

//    public bool IsControllerInUse(Rewired.Player rwPlayer)
//    {
//        return GetControllerIndex(rwPlayer) != -1;
//    }

//    public int GetControllerIndex(Rewired.Player rwPlayer)
//    {
//        if (rwPlayer == rwNullPlayer) return -1;
//        for(int i = 0; i < playerInputMap.Length; ++i)
//        {
//            if (playerInputMap[i].rwPlayer == rwPlayer) return i;
//        }
//        return -1;
//    }

//    public bool IsMainPlayerSet()
//    {
//        return mainPlayer != nullPlayer;
//    }

//    public PlayerInput GetPlayerInput(int playerNum)
//    {
//        if (!IsValidPlayerNum(playerNum)) return null;
//        else return playerInputMap[playerNum];
//    }

//    public PlayerInput GetPlayerInput(Rewired.Player rwPlayer)
//    {
//        for(int i = 0; i < 4; ++i)
//        {
//            if (playerInputMap[i].rwPlayer == rwPlayer) return playerInputMap[i];
//        }
//        return null;
//    }

//    public void SetPlayerMapEnabled(Rewired.Player rwPlayer, int map, bool enabled, bool ignoreAlert = false)
//    {
//        Dictionary<int, bool> mapStates = null;
//        if (!rwPlayerSavedControllerMapStates.TryGetValue(rwPlayer.id, out mapStates))
//        {
//            mapStates = new Dictionary<int, bool>();
//            rwPlayerSavedControllerMapStates.Add(rwPlayer.id, mapStates);
//        }
//        mapStates[map] = enabled;
//    }

//    public Dictionary<int, bool> GetRwPlayerMapStates(Rewired.Player rwPlayer)
//    {
//        Dictionary<int, bool> mapStates = null;
//        if (!rwPlayerSavedControllerMapStates.TryGetValue(rwPlayer.id, out mapStates))
//        {
//            mapStates = new Dictionary<int, bool>();
//        }
//        return mapStates;
//    }

//    public Dictionary<int, bool> GetJoystickMapStates(Rewired.Player rwPlayer, Joystick joystick)
//    {
//        Dictionary<int, bool> states = new Dictionary<int, bool>();
//        foreach (var controllerMap in rwPlayer.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id))
//            states[controllerMap.categoryId] = controllerMap.enabled;
//        return states;
//    }

//    public void PrintPlayerMapStates(int playerNum)
//    {
//        var playerInput = playerInputMap[playerNum];
//        var joystick = GetMainJoystickFromPlayerInput(playerInput);
//        var mapStates = GetJoystickMapStates(playerInput.rwPlayer, joystick);
//        foreach (var state in mapStates)
//        { 
//            //Debug.Log("map state:" + state.Key + " - " + state.Value);
//        }
//        //Debug.Log("joystick enabled: " + joystick.enabled + " all disabled " + disableAllRewiredControllers);
//    }

//    public void ApplyMapStatesToControllers(Rewired.Player rwPlayer)
//    {
//        // restore each map state for the player
//        var mapStates = GetRwPlayerMapStates(rwPlayer);
//        foreach (var state in mapStates)
//            rwPlayer.controllers.maps.SetMapsEnabled(state.Value, state.Key);
//    }

//    public Rewired.Player GetRwPlayerWithJoystick(Joystick joystick)
//    {
//        for (int i = 0; i < ReInput.players.playerCount; ++i)
//        {
//            var rwPlayer = ReInput.players.GetPlayer(i);
//            if (rwPlayer.controllers.ContainsController(ControllerType.Joystick, joystick.id))
//                return rwPlayer;
//        }
//        return null;
//    }

//    public int GetNumPlayers()
//    {
//        return numPlayersAssigned;
//    }

//    public int GetNumNonAIPlayers()
//    {
//        int num = 0;
//        foreach (var playerInput in playerInputMap)
//        {
//            if (playerInput.IsSet() && playerInput.controlType != CONTROL_TYPE.BRAIN)
//                ++num;
//        }
//        return num;
//    }

//    public bool IsPlayingSinglePlayer()
//    {
//        int numPlayers = GetNumNonAIPlayers();
//        return (IsMainPlayerSet() && numPlayers <= 1);
//    }

//    public int GetNumAI()
//    {
//        int num = 0;
//        foreach(var playerInput in playerInputMap)
//        {
//            if (playerInput.controlType == CONTROL_TYPE.BRAIN)
//                ++num;
//        }
//        return num;
//    }

//    public int GetOverwriteAIIndex(Rewired.Player rwPlayer)
//    {
//        if (directlyMapControllersToPlayers)
//        {
//            // if on switch, check to see if position is an ai
//            if (playerInputMap[rwPlayer.id].controlType == CONTROL_TYPE.BRAIN) return rwPlayer.id;
//        }
//        else
//        {
//            // find first ai
//            for(int i = 0; i < playerInputMap.Length; ++i)
//            {
//                if (playerInputMap[i].controlType == CONTROL_TYPE.BRAIN) return i;
//            }
//        }
//        return -1;
//    }

//    public int GetNextFreePosition()
//    {
//        foreach (var playerInput in playerInputMap)
//        {
//            if (!playerInput.IsSet())
//            {
//                return playerInput.gamePlayerId;
//            }
//        }
//        return -1;
//    }

//	public void LockAllControllerVibration(string key)
//	{
//		vibrationLocker.SetLocker(key);
//		if(vibrationLocker.IsLocked())
//		    SetAllControllerVibrationPaused(true);

//	}

//	public void UnlockAllControllerVibration(string key)
//	{
//		vibrationLocker.RemoveLocker(key);
//		if (!vibrationLocker.IsLocked())
//			SetAllControllerVibrationPaused(false);
//	}

//    public void ClearVibrationsLocked()
//    {
//        vibrationLocker.Clear();
//        SetAllControllerVibrationPaused(false);
//    }

//	void SetAllControllerVibrationPaused(bool isPaused)
//    {
//        foreach(var playerInput in playerInputMap)
//            if (playerInput.IsSet()) playerInput.vibrationCont.SetVibrationPaused(isPaused);
//    }

//    public void SetAllControllersDisabled(string key)
//    {
//        disableControllersLocker.SetLocker(key);

//        disableAllRewiredControllers = true;
//        for (int i = 0; i < ReInput.players.playerCount; ++i)
//        {
//            var player = ReInput.players.GetPlayer(i);
//            SetPlayerControllersEnabled(player, false);
//        }
//    }

//    public void RemoveAllControllersDisabled(string key)
//    {
//        disableControllersLocker.RemoveLocker(key);
//        if (!disableControllersLocker.IsLocked())
//        {
//            disableAllRewiredControllers = !enabled;
//            for (int i = 0; i < ReInput.players.playerCount; ++i)
//            {
//                var player = ReInput.players.GetPlayer(i);
//                SetPlayerControllersEnabled(player, true);
//            }
//        }
//    }

//    public void SetPlayerControllersEnabled(Rewired.Player player, bool enabled)
//    {
//        var joysticks = player.controllers.Joysticks;
//        foreach(var joystick in joysticks)
//        {
//            joystick.enabled = enabled;
//        }
//    }

//    public Joystick GetMainJoystickFromPlayerInput(PlayerInput playerInput)
//    {
//        if (playerInput.rwPlayer != null)
//            return GetMainJoystickFromRwPlayer(playerInput.rwPlayer);
//        return null;
//    }

//    public Joystick GetMainJoystickFromRwPlayer(Rewired.Player rwPlayer)
//    {
//        if (rwPlayer.controllers.joystickCount > 0)
//            return rwPlayer.controllers.Joysticks[0];
//        return null;
//    }

//    public PlayerInput GetPlayerInputFromRwPlayer(Rewired.Player rwPlayer)
//    {
//        foreach(var player in playerInputMap)
//        {
//            if (player.rwPlayer == rwPlayer) return player;
//        }
//        return null;
//    }

//    public Rewired.Player GetRWPlayer(int playerNum)
//    {
//        return ReInput.players.GetPlayer(playerNum);
//    }

//    public void AssignJoystickToPlayer(int rwID, Joystick joystick, bool removeOtherControllers = false)
//    {
//        var rwPlayer = ReInput.players.GetPlayer(rwID);
//        if (rwPlayer == null) return;

//        if (removeOtherControllers)
//            ClearAllRwPlayerControllers(rwPlayer);

//        rwPlayer.controllers.AddController<Joystick>(joystick.id, true);
//        var playerInput = GetPlayerInputFromRwPlayer(rwPlayer);

//        // apply map states to newly assigned controller
//        ApplyMapStatesToControllers(rwPlayer);
//    }

//    public void UnAssignJoystick(Joystick joystick)
//    {
//        var rwPlayer = GetRwPlayerWithJoystick(joystick);
//        if(rwPlayer != null)
//        {
//            rwPlayer.controllers.RemoveController<Joystick>(joystick.id);
//        }
//    }

//    public void ClearAllRwPlayerControllers(Rewired.Player rwPlayer)
//    {
//        rwPlayer.controllers.ClearAllControllers();
//    }

//    public void PrintAllControllerInfo()
//    {
//        string msg = "";
//        for (int i = 0; i < ReInput.players.playerCount; ++i)
//        {
//            var player = ReInput.players.GetPlayer(i);
//            msg += "---- Printing info for rwPlayer: " + i + "\n";
//            msg += "Player assigned to: " + GetControllerIndex(player) + "\n";
//            for(int j = 0; j < player.controllers.joystickCount; ++j)
//            {
//                var joystick = player.controllers.Joysticks[j];
//                msg += "joystick: " + joystick.id + " " + joystick.isConnected + " " + joystick.name + " " + joystick.enabled + "\n";
//                var maps = player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id);
//                if(maps != null)
//                {
//                    foreach (var controllerMap in player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id))
//                    {
//                        msg += "map state: category: " + controllerMap.categoryId + " enabled: " + controllerMap.enabled + " layout: " + layoutID + "\n";
//                    }
//                }
//            }
//        }
//    }

//    public void OnDestroy()
//    {
//        // unregister for controller connect / disconnect events
//        ReInput.ControllerConnectedEvent -= OnControllerConnectedDefault;
//        ReInput.ControllerDisconnectedEvent -= OnControllerDisconnectedDefault;
//        ReInput.ControllerPreDisconnectEvent -= OnControllerPreDisconnectDefault;
//    }

//    public void SetInputEventSystemEnabled(bool enabled, Rewired.Player rwPlayer = null)
//    {
//        EventSystem eventSystem = EventSystem.current;
//        Rewired.Integration.UnityUI.RewiredStandaloneInputModule rwInputModule = null;
//        if (eventSystem != null)
//            rwInputModule = eventSystem.GetComponent<Rewired.Integration.UnityUI.RewiredStandaloneInputModule>();

//        if (rwInputModule != null)
//        {
//            if (enabled)
//            {
//                rwInputModule.enabled = true;
//                // use main player if no player specified
//                if(rwPlayer == null)
//                    rwInputModule.RewiredPlayerIds = new int[] { mainPlayer.rwPlayer.id };
//                else
//                    rwInputModule.RewiredPlayerIds = new int[] { rwPlayer.id };
//            }
//            else
//            {
//                rwInputModule.enabled = false;
//                rwInputModule.RewiredPlayerIds = new int[] { };
//            }
//        }
//    }

//    public bool GetInputEventSystemEnabled()
//    {
//        EventSystem eventSystem = EventSystem.current;
//        Rewired.Integration.UnityUI.RewiredStandaloneInputModule rwInputModule = null;
//        if (eventSystem != null)
//            rwInputModule = eventSystem.GetComponent<Rewired.Integration.UnityUI.RewiredStandaloneInputModule>();

//        if (rwInputModule != null)
//        {
//            return rwInputModule.enabled && rwInputModule.RewiredPlayerIds != null;
//        }
//        return false;
//    }

//    public void OnApplicationQuit()
//    {
//        applicationExiting = true;
//    }
//}
