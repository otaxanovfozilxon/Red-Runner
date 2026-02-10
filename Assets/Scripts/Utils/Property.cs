using System;
using System.Collections.Generic;
using UnityEngine;

public class Property<T>
{
    // Non-generic inner class — avoids IL2CPP nested generic issues
    class Act
    {
        public bool CallEvenIfDisabled = false;
        public MonoBehaviour Mb;
        public bool HasMb;

        public Action<T> Changed = null;
        public Action<T, T> ChangedWithPrev = null;
    }

    List<Act> Callbacks = new List<Act>();

    T currentValue;

    public Property() { }

    public Property(T defaultValue)
    {
        currentValue = defaultValue;
    }

    public void AddEvent(Action<T> onChanged, MonoBehaviour mb, bool callEvenIfDisabled = false)
    {
        Callbacks.Add(new Act()
        {
            Mb = mb,
            HasMb = mb != null,
            Changed = onChanged,
            CallEvenIfDisabled = callEvenIfDisabled,
        });
    }

    public void AddEvent(Action<T, T> onChanged, MonoBehaviour mb, bool callEvenIfDisabled = false)
    {
        Callbacks.Add(new Act()
        {
            Mb = mb,
            HasMb = mb != null,
            ChangedWithPrev = onChanged,
            CallEvenIfDisabled = callEvenIfDisabled,
        });
    }

    public void AddEventAndFire(Action<T> onChanged, MonoBehaviour mb, bool callEvenIfDisabled = false)
    {
        AddEvent(onChanged, mb, callEvenIfDisabled);
        onChanged(currentValue);
    }

    public void AddEventAndFire(Action<T, T> onChanged, MonoBehaviour mb, bool callEvenIfDisabled = false)
    {
        AddEvent(onChanged, mb, callEvenIfDisabled);
        onChanged(currentValue, currentValue);
    }

    public void RemoveEvent(Action<T> onChanged)
    {
        for (int i = Callbacks.Count - 1; i >= 0; i--)
        {
            if (Callbacks[i].Changed == onChanged)
                Callbacks.RemoveAt(i);
        }
    }

    public void RemoveEvent(Action<T, T> onChanged)
    {
        for (int i = Callbacks.Count - 1; i >= 0; i--)
        {
            if (Callbacks[i].ChangedWithPrev == onChanged)
                Callbacks.RemoveAt(i);
        }
    }

    public void RemoveEvent(MonoBehaviour mb)
    {
        for (int i = Callbacks.Count - 1; i >= 0; i--)
        {
            if (Callbacks[i].Mb == mb)
                Callbacks.RemoveAt(i);
        }
    }

    public void RemoveAllEvents()
    {
        Callbacks.Clear();
    }

    public void Fire() { ChangeValue(currentValue); }
    public void Fire(MonoBehaviour mb) { ChangeValue(currentValue, mb); }
    public void Fire(T newValue) { Value = newValue; }

    public virtual T Value
    {
        get { return currentValue; }
        set { ChangeValue(value); }
    }

    void ChangeValue(T value, MonoBehaviour mb = null)
    {
        var oldValue = currentValue;
        currentValue = value;

        // Use explicit for-loop instead of RemoveAll lambda to avoid IL2CPP generic closure issues
        var callbacksCopy = new List<Act>(Callbacks);
        for (int i = callbacksCopy.Count - 1; i >= 0; i--)
        {
            var el = callbacksCopy[i];
            try
            {
                // If monoBehaviour has been already removed we'll have null here
                if (el.HasMb && el.Mb == null)
                    continue;

                if (!el.HasMb || (el.Mb.gameObject.activeInHierarchy && el.Mb.enabled) || el.CallEvenIfDisabled)
                    if (mb == null || el.Mb == mb)
                    {
                        if (el.Changed != null)
                            el.Changed(currentValue);
                        if (el.ChangedWithPrev != null)
                            el.ChangedWithPrev(currentValue, oldValue);
                    }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
        // Cleanup dead MonoBehaviours from original list
        for (int i = Callbacks.Count - 1; i >= 0; i--)
        {
            if (Callbacks[i].HasMb && Callbacks[i].Mb == null)
                Callbacks.RemoveAt(i);
        }
    }
}
