using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionHelper : MonoBehaviour
{
    private static ActionHelper _instance;
    public static ActionHelper Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new GameObject("ACTION_HELPER").AddComponent<ActionHelper>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator CoroutineDelayCall(float sec, Action action)
    {        
        yield return new WaitForSeconds(sec);
        if(action != null)
        {
            action.Invoke();
        }
        yield break;
    }

    public void DelayCall(float sec, Action action)
    {
        StartCoroutine(CoroutineDelayCall(sec, action));        
    }

}
