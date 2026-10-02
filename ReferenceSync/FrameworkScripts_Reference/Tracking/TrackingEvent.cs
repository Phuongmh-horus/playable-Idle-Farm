using System.Collections;
using System.Collections.Generic;
#if ADJUST_SDK
using AdjustSdk;
#endif
using AdMod;
using Cysharp.Threading.Tasks;
using Firebase.Analytics;
using UnityEngine;


public class TrackingEvent
{
    public string EventName { get; set; }
    public List<TrackingParameter> Parameters { get; set; }

    public TrackingEvent(string eventName)
    {
        EventName = eventName;
        Parameters = new List<TrackingParameter>();
    }

    public void AddParameter(string parameterName, string value)
    {
        if(value.IsNullOrEmpty()) return;
        Parameters.Add(new TrackingParameter
            {
                ParameterName = parameterName,
                StringValue = value
            }
        );
    }

    public void AddParameter(string parameterName, double value)
    {
        Parameters.Add(new TrackingParameter
            {
                ParameterName = parameterName,
                DoubleValue = value
            }
        );
    }

    public void AddParameter(string parameterName, int value)
    {
        Parameters.Add(new TrackingParameter
            {
                ParameterName = parameterName,
                IntValue = value
            }
        );
    }

    public List<Parameter> GetFirebaseParameters()
    {
        var firebaseParameters = new List<Parameter>();
        foreach (var parameter in Parameters)
        {
            firebaseParameters.Add(parameter.ToFirebaseParameter());
        }

        return firebaseParameters;
    }

    public void TrackEventWithoutParam()
    {
        TrackEventWithoutParamAsync()
            .Forget();
    }

    public static bool IsBlockTracking()
    {
#if !TEST_TRACKING
        if (DataManager.PlayerData.IsNative) return true;
#if ACCOUNT_TOOL || ENABLE_LOG
        return true;
#endif
        if (UserBlocker.Instance != null)
        {
            if (UserBlocker.Instance.IsBlocked)
            {
                return true;
            }
        }
#if UNITY_IOS
        if (TestFlightChecker.IsTestFlight)
        {
            return true;
        }
#endif
#endif

        return false;
    }
    public async UniTask TrackEventWithoutParamAsync()
    {
        if(IsBlockTracking()) return;

        FirebaseManager.FirebaseLogEventWithoutParam(EventName);

    }

    public void TrackEventFireBase()
    {
        if(IsBlockTracking()) return;
        if (Parameters.Count == 0)
        {
            Parameters.Add(new()
                {
                    ParameterName = "event_name",
                    StringValue = EventName
                }
            );
        }

        FirebaseManager.FirebaseLogEvent(EventName, GetFirebaseParameters()
            .ToArray()
        );
    }
}

public class TrackingParameter
{
    public string ParameterName { get; set; }
    public string StringValue { get; set; }
    public double DoubleValue { get; set; }
    public int IntValue { get; set; }

    public Parameter ToFirebaseParameter()
    {
        if (DoubleValue != 0)
        {
            return new Parameter(ParameterName, DoubleValue);
        }

        if (IntValue != 0)
        {
            return new Parameter(ParameterName, IntValue);
        }

        return new Parameter(ParameterName, StringValue);
    }

    public string GetValue()
    {
        if (DoubleValue != 0)
        {
            return DoubleValue.ToString("F2");
        }

        if (IntValue != 0)
        {
            return IntValue.ToString();
        }

        return StringValue;
    }
}