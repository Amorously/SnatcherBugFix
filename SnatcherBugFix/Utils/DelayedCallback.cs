using BepInEx.Unity.IL2CPP.Utils.Collections;
using System.Collections;
using UnityEngine;

namespace SnatcherBugFix.Utils;

// from Dinorush's EWC
public sealed class DelayedCallback
{
    private readonly Func<float> _getDelay;
    private readonly Action? _onEnd;
    private float _endTime;
    private Coroutine? _routine;

    public DelayedCallback(Func<float> getDelay, Action? onEnd)
    {
        _getDelay = getDelay;
        _onEnd = onEnd;
    }

    public void Start()
    {
        _endTime = Clock.Time + _getDelay.Invoke();
        _routine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
    }

    public IEnumerator Update()
    {
        while (_endTime > Clock.Time)
            yield return new WaitForSeconds(_endTime - Clock.Time);
        _routine = null;
        _onEnd?.Invoke();
    }

    public void Stop()
    {
        if (_routine != null)
        {
            CoroutineManager.StopCoroutine(_routine);
            _routine = null;
            _onEnd?.Invoke();
        }
    }

    public void Cancel()
    {
        if (_routine != null)
        {
            CoroutineManager.StopCoroutine(_routine);
            _routine = null;
        }
    }
}