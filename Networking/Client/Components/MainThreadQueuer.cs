using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainThreadQueuer : MonoBehaviour
{
    private static MainThreadQueuer instance;

    public static MainThreadQueuer Instance
    {
        get
        {
            return instance;
        }
    }

    Queue<Action> MessageQueue;

    private void Awake()
    {
        if (instance == null)
        {
            var instances = FindObjectsOfType<MainThreadQueuer>();

            if (instances.Length > 0)
            {
                if (instances.Length > 1)
                {
                    for (int i = 1; i < instances.Length; i++)
                        Destroy(instances[i].gameObject); 
                }
            }

            if (instance == null)
                instance = this;
        }
        MessageQueue = new Queue<Action>();
    }

    public void AddMessage(Action action)
    {
        MessageQueue.Enqueue(action);
    }

    private void Update()
    {
        while(MessageQueue.Count > 0)
        {
            MessageQueue.Dequeue()();
        }
    }
}
