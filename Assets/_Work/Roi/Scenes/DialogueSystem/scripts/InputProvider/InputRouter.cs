using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InputRouter : MonoBehaviour
{
    public static InputRouter Instance { get; private set; }
    private IInputProvider _active_provider;
    private List<IInputProvider> _provider_list = new();
    public bool _debug; // ideal implementation: Poll a separate manager to see if we should be debugging or not. That way, we can just do manager checking, to see if we're in a production environment. The production environment should never allow for debugging, unless literally hacked to do so.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if(mb is IInputProvider provider)
            {
                _provider_list.Add(provider);
            }
        }
        if (_provider_list.Count > 0)
        {

            _active_provider = _provider_list[0];
            if (_debug)
            {
                Debug.Log($"InputRouter: Using provider {_active_provider.GetType().Name}");
            }
        }
        else
        {
            Debug.LogWarning("inputRouter: No input providers found in scene!");
        }
    }

    public void SetProvider(IInputProvider provider)
    {
        _active_provider = provider;
    }

    public string[] GetProvidernames()
    {
        var names = new string[_provider_list.Count];
        for (int i = 0; i < _provider_list.Count; i++)
        {
            names[i] = _provider_list[i].GetType().Name;
        }
        return names;
    }

    public bool GetChoicePressed(int number)
    {
        if (_active_provider == null) return false;

        // For now, only the keyboard provider implements these.
        return _active_provider.GetChoicePressed(number);
    }


    public bool NextPressed => _active_provider?.GetNextPressed() ?? false;
    public bool SelectPressed => _active_provider?.GetSelectPressed() ?? false;
}
