using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Higher priorities will override lower ones. 'NEVER' is only used internally. If passed it will be set to 'LOW' </summary>
public enum TIME_PRIORITY { NEVER, LOW, MEDIUM, HIGH, ALWAYS }
public class TimeManager : MonoBehaviour
{
	public float timeScaleOnHitDur;

	static float defaultTimeScale;
	static float defaultFixedTimeStep;

	static float savedTimeScale;
	static float savedTimeScaleInternal;

	public static bool timeStopped { get { return timeLocker.IsLocked(); } }
	static int timerMode = 0;
	static Timer scaleTimerT = new Timer(0.0f);
	static FrameTimer scaleTimerF = new FrameTimer(0);

	static int _key = 0;
    static LogicLocker timeLocker = new LogicLocker();
	static bool keyIsActive = false;
	static TIME_PRIORITY keyPriority;

	static TimeManager instance;
	static Coroutine timeScaleRoutine;
	static Coroutine timeScaleCurveRoutine;

	void Awake()
	{
		if (instance != null && instance != this)
		{
			DestroyImmediate(gameObject);
			return;
		}

		instance = this;
		
		defaultTimeScale = Time.timeScale;
		savedTimeScale = defaultTimeScale;
		savedTimeScaleInternal = defaultTimeScale;
		defaultFixedTimeStep = Time.fixedDeltaTime;

		SetGameSpecificTimeScale(defaultTimeScale, _key, TIME_PRIORITY.LOW);
		DontDestroyOnLoad(gameObject);

	}
	void Update()
	{
		if (!timeStopped)
		{
			switch (timerMode)
			{
				case 0:
					if (scaleTimerT.Update(Time.unscaledDeltaTime))
						RestoreGameDefaultTimeScale(_key);
					break;
				case 1:
					if (scaleTimerF.Update())
						RestoreGameDefaultTimeScale(_key);
					break;
			}
		}
	}

	static bool CanOverrideScale(int key, ref TIME_PRIORITY priority)
	{
		if (priority == TIME_PRIORITY.NEVER) priority = TIME_PRIORITY.LOW;
		// If priority is not always, or the priority is lower than current
		if (priority != TIME_PRIORITY.ALWAYS && priority <= keyPriority)
			// If there is an active key and the passed key is not the same
			if (keyIsActive && _key != key)
				return false; // Than we cannot override
		return true; // Otherwise we can
	}
	static void LockManager(int key, TIME_PRIORITY priority)
	{
		_key = key;
		keyIsActive = true;
		keyPriority = priority;
		savedTimeScale = Time.timeScale;
	}
	static void UnlockManager()
	{
		keyIsActive = false;
		scaleTimerT.Deactivate();
		scaleTimerF.Deactivate();
		if (timeScaleCurveRoutine != null)
			instance.StopCoroutine(timeScaleCurveRoutine);
	}

	public static float GetGameTimeScale()
	{
		return defaultTimeScale;
	}
	public static float GetSavedTimeScale()
	{
		return savedTimeScale;
	}

    /// <summary>
    /// Forces the time manager to restore time, and close any active time scales. It does not start time.
    /// </summary>
    public static void ForceRestoreTime()
    {
        RestoreRealTimeScale(_key);
    }

	/// <summary> 
	/// Set the game back to real time. 
	/// This also unlocks the manager, and therefore must be called before another key can set a new time scale.
	/// </summary>
	/// <param name="key">This must match the key used to set the timescale</param>
	public static bool RestoreRealTimeScale(string key)
	{
		return RestoreRealTimeScale(key.GetHashCode());
	}
	/// <summary> 
	/// Set the game back to real time, and will clear any unpaired stops.
	/// This also unlocks the manager, and therefore must be called before another key can set a new time scale.
	/// </summary>
	/// <param name="key">This must match the key used to set the timescale</param>
	public static bool RestoreRealTimeScale(int key)
	{
		if(SetTimeScale(1.0f, key, keyPriority))
		{
            timeLocker.Clear();
			keyIsActive = false;
			scaleTimerT.Deactivate();
			scaleTimerF.Deactivate();
			if (timeScaleCurveRoutine != null)
				instance.StopCoroutine(timeScaleCurveRoutine);
			return true;
		}
		return false;
	}

	/// <summary> 
	/// Restores the game to its normal speed. 
	/// This also unlocks the manager, and therefore must be called before another key can set a new time scale.
	/// </summary>
	/// <param name="key">This must match the key used to set the timescale</param>
	public static bool RestoreGameDefaultTimeScale(string key)
	{
		return RestoreGameDefaultTimeScale(key.GetHashCode());
	}
	/// <summary> 
	/// Restores the game to its normal speed. 
	/// This also unlocks the manager, and therefore must be called before another key can set a new time scale.
	/// </summary>
	/// <param name="key">This must match the key used to set the timescale</param>
	public static bool RestoreGameDefaultTimeScale(int key)
	{
		if (SetTimeScale(defaultTimeScale, key, keyPriority))
		{
			UnlockManager();
			return true;
		}
		return false;
	}

	static void SetGameSpecificTimeScale(float scale, string key, TIME_PRIORITY priority)
	{
		SetGameSpecificTimeScale(scale, key.GetHashCode(), priority);
	}
	static void SetGameSpecificTimeScale(float scale, int key, TIME_PRIORITY priority)
	{
		defaultTimeScale = scale;
		SetTimeScale(defaultTimeScale, key, priority);
	}

	/// <summary> Set the time scale. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	public static bool SetTimeScale(float scale, string key, TIME_PRIORITY priority)
	{
		return SetTimeScale(scale, key.GetHashCode(), priority);
	}
	/// <summary> Set the time scale. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	public static bool SetTimeScale(float scale, int key, TIME_PRIORITY priority)
	{
		if(!CanOverrideScale(key, ref priority))
				return false;
		UnlockManager();
		SetTimeScaleInternal(scale, key, priority);
		return true;
	}
	static void SetTimeScaleInternal(float scale, int key, TIME_PRIORITY priority)
	{
		if (timeScaleRoutine != null)
			instance.StopCoroutine(timeScaleRoutine);
		timeScaleRoutine = instance.StartCoroutine(SetTimeScaleInternalRoutine(scale, key, priority));
	}

	static IEnumerator SetTimeScaleInternalRoutine(float scale, int key, TIME_PRIORITY priority)
	{
		while (timeStopped)
			yield return null;
		LockManager(key, priority);
		Time.timeScale = scale;
		Time.fixedDeltaTime = defaultFixedTimeStep * scale;
	}

	/// <summary> Set a temporary time scale that expires after a set time. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	/// <param name="time">the duration of the scale in seconds.</param>
	public static bool SetTimedTimeScale(float scale, float time, string key, TIME_PRIORITY priority)
	{
		return SetTimedTimeScale(scale, time, key.GetHashCode(), priority);
	}
	/// <summary> Set a temporary time scale that expires after a set time. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	/// <param name="time">the duration of the scale in seconds.</param>
	public static bool SetTimedTimeScale(float scale, float time, int key, TIME_PRIORITY priority)
	{
		if (SetTimeScale(scale, key, priority))
		{
			timerMode = 0;
			scaleTimerT.Activate(time);
			return true;
		}
		return false;
	}

	/// <summary> Set a temporary time scale that expires after a set time. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	/// <param name="numFrames">The amount of frames the time scale should affect.</param>
	public static bool SetTimedTimeScale(float scale, int numFrames, string key, TIME_PRIORITY priority)
	{
		return SetTimedTimeScale(scale, numFrames, key.GetHashCode(), priority);
	}
	/// <summary> Set a temporary time scale that expires after a set time. </summary>
	/// <param name="scale">The scale you wish to set</param>
	/// <param name="key">They key that this scale is tied to.</param>
	/// <param name="priority">The priority of the scale.</param>
	/// <param name="numFrames">The amount of frames the time scale should affect.</param>
	public static bool SetTimedTimeScale(float scale, int numFrames, int key, TIME_PRIORITY priority)
	{
		if (SetTimeScale(scale, key, priority))
		{
			timerMode = 1;
			scaleTimerF.Activate(numFrames);
			return true;
		}
		return false;
	}

	static void SetTimeScaleNoSave(float scale)
	{
		Time.timeScale = scale;
		Time.fixedDeltaTime = defaultFixedTimeStep * scale;
	}

	/// <summary> Stop time. NOTE: You MUST pair this stop with a start, or time will be stopped FOREVER. </summary>
	public static bool StopTime(string key)
	{
        if (!timeStopped)
        {
            // if first time stopping time
            if (Time.timeScale != 0.0f)
                savedTimeScaleInternal = Time.timeScale;
            SetTimeScaleNoSave(0.0f);
        }
        timeLocker.SetLocker(key);
		return true;
	}
	/// <summary>
	/// Starts time again. 
	/// NOTE: As multiple stops can be called, they must all be paired with starts.
	/// If the amount of starts is less than the amount of active stops, time will remain stopped. 
	/// </summary>
	/// <returns></returns>
	public static bool StartTime(string key)
	{
        // if time not stopped don't start time
		if (!timeStopped)
			return false;
        timeLocker.RemoveLocker(key);
		if (!timeLocker.IsLocked())
		{
            // if all stops removed, restore time
			SetTimeScaleNoSave(savedTimeScaleInternal);
		}
		return true;
	}

	public static void OnSceneSwitch()
	{
		// restore time scale
		RestoreRealTimeScale(_key);
	}
}
