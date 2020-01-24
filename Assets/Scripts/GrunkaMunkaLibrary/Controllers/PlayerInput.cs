using Rewired;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInput : MonoBehaviour
{
	public int gamePlayerId { get; private set; }
	public Rewired.Player rwPlayer { get; private set; }
	public bool isMainPlayer;
	public CONTROL_TYPE controlType { get; private set; }
	public CONTROL_SCHEME shipControlScheme { get; private set; }
	public INPUT_CATEGORY inputState { get; private set; }
	public PlayerVibrationCont vibrationCont { get; private set; }
	public bool hasBeenSaved { get; private set; }
	public bool waitingForReconnect { get; private set; }
	public int dcFrame { get; private set; }

	public void Init(int gamePlayerId)
	{
		this.gamePlayerId = gamePlayerId;
		vibrationCont = gameObject.AddComponent<PlayerVibrationCont>();
		vibrationCont.Init(this);
		Clear();
	}

	public bool IsSet()
	{
		return controlType != CONTROL_TYPE.NONE;
	}

	public void Clear()
	{
		SetPlayerInputState(INPUT_CATEGORY.NOT_JOINED);
		rwPlayer = ControllerManager.rwNullPlayer;
		controlType = CONTROL_TYPE.NONE;
		vibrationCont.ClearVibrations();
		isMainPlayer = false;
		hasBeenSaved = false;
		waitingForReconnect = false;
		dcFrame = 0;
	}

	public void SetAsPlayer(Rewired.Player rwPlayer)
	{
		this.rwPlayer = rwPlayer;
		controlType = CONTROL_TYPE.CONTROLLER;
	}

	public void SetAsAI()
	{
		rwPlayer = ControllerManager.rwNullPlayer;
		controlType = CONTROL_TYPE.BRAIN;
		//SetControlScheme(CONTROL_SCHEME.ACTIVE_RB);
		isMainPlayer = false;
	}

	public void SetControlScheme(CONTROL_SCHEME controlScheme)
	{
		this.shipControlScheme = controlScheme;
	}

	public void SetPlayerInputState(INPUT_CATEGORY inputState)
	{
		// don't touch input state if no controller or player not assigned
		if (controlType == CONTROL_TYPE.NONE || rwPlayer == ControllerManager.rwNullPlayer) return;
		var controllerMgr = ControllerManager.instance;
		this.inputState = inputState;

		switch (inputState)
		{
			case INPUT_CATEGORY.NOT_JOINED:
				controllerMgr.SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Default, true);
				controllerMgr.SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Ship, false);
				break;
			case INPUT_CATEGORY.GAME:
				controllerMgr.SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Default, false);
				controllerMgr.SetPlayerMapEnabled(rwPlayer, RewiredConsts.Category.Ship, true);
				break;
		}
	}

	public bool IsControllerConnected()
	{
		if (!IsSet()) return false;
		else
		{
			if (controlType == CONTROL_TYPE.BRAIN) return false;
			else
			{
				bool hasController = rwPlayer.controllers.joystickCount != 0 && rwPlayer.controllers.Joysticks[0].isConnected;
				return hasController;
			}
		}
	}

	public void PlayerHasBeenSaved()
	{
		hasBeenSaved = true;
	}
}