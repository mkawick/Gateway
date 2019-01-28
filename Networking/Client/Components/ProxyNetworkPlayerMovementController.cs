using Assets._Scripts.Player;
using Assets._Scripts.Player.MoveStates;
using Assets._Scripts.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProxyNetworkPlayerMovementController : MonoBehaviour
{
    public Stats.StatsComponent statsComponent { get; private set; }
    private IMoveState moveState;

    private Vector3 proxyPosition;
    private Quaternion proxyRotation;
    private Vector3 proxyVelocity;

    public void SetPositionRotationAndVelocity(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        proxyPosition = position;
        proxyRotation = rotation;
        proxyVelocity = velocity;
    }

    private void Start()
    {
        statsComponent = GetComponent<Stats.StatsComponent>();

        moveState = new ProxyNetworkWalkMoveState();
        moveState.OnStateEnter(transform);
    }

    private void Update()
    {
        if (moveState == null)
        {
            Debug.LogWarning("UpdateMoveState: No move state!");
            return;
        }

        IMoveState nextState = null;
        nextState = moveState.StateUpdate(transform, proxyVelocity, null, ref proxyVelocity, ref proxyRotation);

        if (nextState != null)
        {
            moveState.OnStateExit();
            moveState = nextState;
            moveState.OnStateEnter(transform);
        }
    }
}