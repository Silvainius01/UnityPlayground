using System.Collections.Generic;
using UnityEngine;

public class LogicLocker {

    private HashSet<string> lockers = new HashSet<string>();
    private Dictionary<string, Timer> timedLockers = new Dictionary<string, Timer>();
	List<string> deadTimers = new List<string>();

    public void SetLocker(string key)
    {
        lockers.Add(key);
    }

    public bool RemoveLocker(string key)
    {
        return lockers.Remove(key);
    }

    public void SetTimedLocker(string key, float time)
    {
        Timer timer;
        if (!timedLockers.TryGetValue(key, out timer))
        {
            timer = new Timer(time, true);
            timedLockers.Add(key, timer);
        }
        else
        {
            timedLockers[key].Activate(time);
        }
    }

    public void RemoveTimedLocker(string key)
    {
        timedLockers.Remove(key);
    }

    public void UpdateTimedValues(float dt)
    {
		if (timedLockers.Count == 0)
			return;

		deadTimers.Clear();
        foreach (KeyValuePair<string, Timer> lockerKVP in timedLockers)
        {
            if (lockerKVP.Value.Update(dt))
            {
				deadTimers.Add(lockerKVP.Key);
            }
        }
		foreach (var key in deadTimers)
			RemoveTimedLocker(key);
    }

    public bool IsLocked()
    {
        return lockers.Count != 0 || timedLockers.Count != 0;
    }

    public int NumLocks()
    {
        return lockers.Count + timedLockers.Count;
    }

    public bool Contains(string key)
    {
        return lockers.Contains(key);
    }

    public bool ContainTimed(string key)
    {
        return timedLockers.ContainsKey(key);
    }

    public Timer GetTimerForLocker(string key)
    {
        Timer timer = null;
        timedLockers.TryGetValue(key, out timer);
        return timer;
    }

    public void Clear()
    {
        deadTimers.Clear();
        lockers.Clear();
        timedLockers.Clear();
    }

    public string GetLockerKeysString()
    {
        var keys = new List<string>(lockers);
        string msg = "locker keys: ";
        foreach (var key in keys)
            msg += key + ", ";
        return msg;
    }
}
