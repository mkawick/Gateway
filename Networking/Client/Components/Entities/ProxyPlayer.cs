using Packets;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


public class ProxyPlayer : ClientWorldEntity
{
    protected class MovementUpdate
    {
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public long Timestamp { get; private set; }


        public MovementUpdate(Vector3 pos, Quaternion rot, long timestamp)
        {
            Position = pos; Rotation = rot; Timestamp = timestamp;
        }

        public MovementUpdate(WorldEntityPacket packet, DateTime timestamp)
        {
            Position = packet.position.Get();
            Rotation = Quaternion.Euler(packet.rotation.Get());
            Timestamp = timestamp.ToUnixMilliseconds();
        }
    }


    Queue<MovementUpdate> moves;
    MovementUpdate nextMove;

    float elapsedRotationLerpTime;
    float elapsedLerpTime;

    //To account for the ReachedDestination check not being exact.
    Vector3 realStartMovePos;
    Quaternion realStartRotation;

    private ProxyNetworkPlayerMovementController movementController;

    public override void Init(IClientNetworking client, int entityId)
    {
        base.Init(client, entityId);

        movementController = GetComponent<ProxyNetworkPlayerMovementController>();

        moves = new Queue<MovementUpdate>(4);
        //This helps kick us off at the start, the update relies on having at least 2 movement updates (to calculate the time between them).
        moves.Enqueue(new MovementUpdate(transform.position, transform.rotation, DateTime.UtcNow.ToUnixMilliseconds()));
        moves.Enqueue(new MovementUpdate(transform.position, transform.rotation, DateTime.UtcNow.ToUnixMilliseconds()));

        AdvanceToNextMove();

        client.AddListener<WorldEntityPacket>(entityId, OnWorldEntityPacket);
    }

    private void OnWorldEntityPacket(WorldEntityPacket packet)
    {
        moves.Enqueue(
            new MovementUpdate(
                packet.position.Get(),
                Quaternion.Euler(packet.rotation.Get()),
                DateTime.UtcNow.ToUnixMilliseconds()));
    }

    void AdvanceToNextMove()
    {
        elapsedLerpTime = 0;

        moves.Dequeue();
        nextMove = moves.Peek();

        realStartRotation = transform.rotation;
        realStartMovePos = transform.position;
    }


    protected override void Update()
    {
        base.Update();

        //We must have at least 2 moves in the buffer, to calculate the delta time

        if (moves.Count > 1)
        {
            if (ReachedDestination(nextMove.Position))
            {
                AdvanceToNextMove();
            }
        }

        Vector3 velocity = Vector3.zero;
        var deltaRate = Time.deltaTime * NetworkConstants.TickRate;
        elapsedLerpTime += deltaRate;

        if (!ReachedDestination(nextMove.Position))
        {
            Vector3 oldPosition = transform.position;
            transform.position = Vector3.Lerp(realStartMovePos, nextMove.Position, elapsedLerpTime);
            velocity = (oldPosition - transform.position) / Time.deltaTime;
        }
        if (!ReachedRotation(nextMove.Rotation))
        {
            transform.rotation = Quaternion.Lerp(realStartRotation, nextMove.Rotation, elapsedLerpTime);
        }

        movementController.SetPositionRotationAndVelocity(transform.position, transform.rotation, velocity);
    }

    bool ReachedDestination(Vector3 dest)
    {
        float threshold = 0.01f;

        if ((transform.position - dest).sqrMagnitude < threshold)
            return true;
        else
            return false;
    }

    bool ReachedRotation(Quaternion dest)
    {
        float threshold = 0.5f;

        if (Quaternion.Angle(transform.rotation, dest) < threshold)
            return true;
        else
            return false;
    }
}
