using UnityEngine;

[System.Serializable]

public class PID
{

    // P I D
    public string name;

    [SerializeField]
    public float Kp = 1;
    [SerializeField]
    public float Ki = 0;
    [SerializeField]
    public float Kd = 0.1f;

    private float P, I, D;
    private float prevError;
    private bool firstOutput;

    public PID(string n)
    {
        name = n;
        Reset();
    }
    
    /// <param name="p"> Multiplier for current error </param>
    /// <param name="i"> Multiplier for error over time </param>
    /// <param name="d"> Multiplier for difference between last error and current error </param>
    public PID(float p, float i, float d, string name = "")
    {
        Kp = p;
        Ki = i;
        Kd = d;
        this.name = name;
        Reset();
    }

    public PID(PID pid)
    {
        name = pid.name;
        Kp = pid.Kp;
        Ki = pid.Ki;
        Kd = pid.Kd;
        Reset();
    }

    public void Reset()
    {
        firstOutput = true;
        P = I = D = prevError = 0.0f;
    }

    public float GetOutput(float currentError, float deltaTime)
    {
        P = currentError;
        I += P * deltaTime;
        if (firstOutput)
        {
            D = 0.0f;
            firstOutput = false;
        }
        else
        {
            D = (P - prevError) / deltaTime;
        }

        prevError = currentError;
        return P * Kp + I * Ki + D * Kd;
    }

    public void InitCurrError(float currentError)
    {
        if (firstOutput)
        {
            D = 0.0f;
            firstOutput = false;
        }
        prevError = currentError;
    }

    public float GetOutputPOnly(float currError, float deltaTime)
    {
        P = currError;
        return P * Kp;
    }
}
