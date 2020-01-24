using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class VibrationLockManager
{
    private PlayerVibrationCont vibrationCont;
    private HashSet<string> activeKeys = new HashSet<string>();

    public VibrationLockManager(PlayerVibrationCont vibrationCont)
    {
        this.vibrationCont = vibrationCont;
    }

    public void SetVibration(string key, float strength)
    {
        if (!activeKeys.Contains(key))
        {
            activeKeys.Add(key);
            vibrationCont.SetVibration(key, strength);
        }
    }

    public void RemoveVibration(string key)
    {
        if (activeKeys.Remove(key)) vibrationCont.RemoveVibration(key);
    }

    public void ClearVibrations()
    {
        foreach(var key in activeKeys)
        {
            vibrationCont.RemoveVibration(key);
        }
        activeKeys.Clear();
    }
}

public class PlayerVibrationCont : MonoBehaviour {

    struct TimedVibrationData
    {
        public Timer timer;
        public float strength;
    };

    private static bool disableVibrations = false;
    private PlayerInput playerInput = ControllerManager.instance.nullPlayer;
    private Dictionary<string, float> activeVibrations = new Dictionary<string, float>();
    private Dictionary<string, TimedVibrationData> timedVibrations = new Dictionary<string, TimedVibrationData>();
    private float currentVibrationStrength = 0.0f;
    private bool pauseVibrations = false;
    List<string> removedVibrations = new List<string>(10);

    // Update is called once per frame
    void Update()
    {
        if (disableVibrations) return;

        if (!pauseVibrations)
        {
            // update timed vibrations
            removedVibrations.Clear();
            foreach (KeyValuePair<string, TimedVibrationData> timedVibration in timedVibrations)
            {
                if (timedVibration.Value.timer.isActive)
                {
                    timedVibration.Value.timer.Update(Time.deltaTime);
                    if (!timedVibration.Value.timer.isActive)
                    {
                        removedVibrations.Add(timedVibration.Key);
                    }
                }
            }
            // remove completed vibrations
            foreach (string key in removedVibrations)
            {
                timedVibrations.Remove(key);
            }

            if (removedVibrations.Count > 0)
            {
                UpdateVibration(GetMaxVibration());
            }
        }
    }

    public void Init(PlayerInput playerInput)
    {
        this.playerInput = playerInput;
    }

    private void OnDisable()
    {
        ClearVibrations();
    }

    public void ClearVibrations()
    {
        timedVibrations.Clear();
        activeVibrations.Clear();
        UpdateVibration(0.0f);
    }

    private float GetMaxVibration()
    {
        float max = 0.0f;
        foreach (KeyValuePair<string, TimedVibrationData> timedVibration in timedVibrations)
            if (timedVibration.Value.strength > max) max = timedVibration.Value.strength;

        foreach (KeyValuePair<string, float> vibration in activeVibrations)
            if (vibration.Value > max) max = vibration.Value;
        return max;
    }

    public void SetVibration(string key, float strength)
    {
        if (disableVibrations) return;
        activeVibrations[key] = strength;
        if (strength > currentVibrationStrength)
        {
            UpdateVibration(strength);
        }
    }

    public void RemoveVibration(string key)
    {
        if (activeVibrations.ContainsKey(key))
        {
            activeVibrations.Remove(key);
            UpdateVibration(GetMaxVibration());
        }
    }

    public void SetTimedVibration(string key, float strength, float time)
    {
        if (disableVibrations) return;
        TimedVibrationData timedVibration;
        // update timed vibration if new info given for the key
        if (timedVibrations.TryGetValue(key, out timedVibration))
        {
            timedVibration.timer.Activate(time);
            if (timedVibration.strength < strength)
            {
                timedVibration.strength = strength;
                if (timedVibration.strength > currentVibrationStrength)
                    UpdateVibration(timedVibration.strength);
            }
        }
        // add timed vibration if there isn't one
        else
        {
            timedVibration = new TimedVibrationData();
            timedVibration.strength = strength;
            timedVibration.timer = new Timer(time, true);
            timedVibrations[key] = timedVibration;
            if (strength > currentVibrationStrength)
            {
                UpdateVibration(strength);
            }
        }
    }


    public void UpdateVibration(float strength)
    {
        currentVibrationStrength = strength;

        if (playerInput.controlType != CONTROL_TYPE.CONTROLLER) return;

        if (currentVibrationStrength == 0)
        {
            playerInput.rwPlayer.StopVibration();
        }
        else
        {
            SetJoystickVibrationStrength(currentVibrationStrength);
        }
    }

    private void SetJoystickVibrationStrength(float strength)
    {
#if !UNITY_EDITOR && UNITY_SWITCH
        strength *= 0.2f;
#endif

        if (pauseVibrations || disableVibrations) strength = 0.0f;
        // Set vibration by motor index
        foreach (Joystick j in playerInput.rwPlayer.controllers.Joysticks)
        {
            if (!j.supportsVibration) continue;
            if (j.vibrationMotorCount > 0) j.SetVibration(0, strength);
            if (j.vibrationMotorCount > 1) j.SetVibration(1, strength);
        }
    }
	
    public void SetVibrationPaused(bool isPaused)
    {
        pauseVibrations = isPaused;
        if (isPaused) SetJoystickVibrationStrength(0.0f);
        else
        {
            SetJoystickVibrationStrength(currentVibrationStrength);
        }
    }

    public void OnDestroy()
    {
        if (playerInput.controlType == CONTROL_TYPE.CONTROLLER)
        {
            // stop controller vibration
            ClearVibrations();
        }
    }
}
