using UnityEngine;
using System.Collections.Generic;

public class ValueAdjustor {

    private struct TimedValue
    {
        public float value;
        public Timer timer;

        public TimedValue(float value, float time)
        {
            this.value = value;
            timer = new Timer(time, true);
        }
    }

    private Dictionary<string, float> adjustors = new Dictionary<string, float>();
    private Dictionary<string, TimedValue> timedAdjustors = new Dictionary<string, TimedValue>(); 
    private float totalValue = 0.0f;

    public void SetAdjustor(string key, float val)
    {
        float oldVal;
        if (adjustors.TryGetValue(key, out oldVal))
            totalValue -= oldVal;

        adjustors[key] = val;
        totalValue += val;
    }

    public void RemoveAdjustor(string key)
    {
        float oldVal;
        if (adjustors.TryGetValue(key, out oldVal))
        {
            totalValue -= oldVal;
            adjustors.Remove(key);
        }
    }

    public bool GetAdjustorValue(string key, out float adjustor)
    {
        float val;
        if (adjustors.TryGetValue(key, out val))
        {
            adjustor = val;
            return true;
        }
        adjustor = 0.0f;
        return false;
    }

    public void SetTimedAdjustor(string key, float val, float time)
    {
        TimedValue oldVal;
        if (timedAdjustors.TryGetValue(key, out oldVal))
            totalValue -= oldVal.value;

        timedAdjustors[key] = new TimedValue(val, time);
        totalValue += val;
    }

    public void RemoveTimedAdjustor(string key)
    {
        TimedValue oldVal;
        if (timedAdjustors.TryGetValue(key, out oldVal))
        {
            totalValue -= oldVal.value;
            timedAdjustors.Remove(key);
        }
    }

    public bool GetTimedAdjustorValue(string key, out float adjustor)
    {
        TimedValue val;
        if (timedAdjustors.TryGetValue(key, out val))
        {
            adjustor = val.value;
            return true;
        }
        adjustor = 0.0f;
        return false;
    }

    List<string> toRemoveList = new List<string>();
    public void UpdateTimedValues(float dt)
    {
		toRemoveList.Clear();
        foreach(KeyValuePair<string, TimedValue> timedValue in timedAdjustors)
        {
            if (timedValue.Value.timer.Update(dt))
            {
                toRemoveList.Add(timedValue.Key);
            }
        }
        foreach(string key in toRemoveList)
        {
            RemoveTimedAdjustor(key);
        }
    }

    public float GetValue()
    {
        return totalValue;
    }

    public void Clear()
    {
        adjustors.Clear();
        timedAdjustors.Clear();
        totalValue = 0.0f;
    }

    public void Print()
    {
        // pring normal adjustors
        foreach(KeyValuePair<string, float> adjustor in adjustors)
        {
            Debug.Log("key: " + adjustor.Key + " val: " + adjustor.Value);
        }
        // print timed adjustors
        foreach(KeyValuePair<string, TimedValue> timedAdjustor in timedAdjustors)
        {
            Debug.Log("key: " + timedAdjustor.Key + " val: " + timedAdjustor.Value.value + " time left: " + timedAdjustor.Value.timer.timeLeft);
        }
    }
}
