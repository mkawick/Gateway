using Assets._Scripts.Player.MoveStates;
using UnityEngine;
using Assets._Scripts.Settings;

public class ProxyNetworkWalkMoveState : IMoveState
{
    private SimulationSettings simulationSettings;
    private ProxyNetworkPlayerMovementController playerController;
    private ProxyNetworkPlayerAnimationController animationController;

    public void OnStateEnter(Transform parent)
    {
        GetNecessaryComponents(parent);
    }

    public void OnStateExit()
    {
        
    }

    public IMoveState StateUpdate(Transform parent, Vector3 inputDirection, Transform cameraTransform, ref Vector3 velocity, ref Quaternion rotation)
    {
        Vector3 animVelocity = new Vector3(inputDirection.x, 0, inputDirection.z);
        float animSpeed = animVelocity.magnitude / playerController.statsComponent.Stats[StatType.RegularWalkSpeed].BaseValue;

        animationController.SetDesiredSpeedAndDirection(animSpeed, parent.rotation * inputDirection.normalized, inputDirection.normalized);
        animationController.SetFalling(false);

        return null;
    }

    public MoveStateCategory GetMoveStateCategory()
    {
        return MoveStateCategory.WALK;
    }

    public SimulationSettings.GenericMoveStateSettings GetSettings()
    {
        return simulationSettings.movementSettings.walkMoveStateSettings.walkGenericMoveStateSettings;
    }

    private void GetNecessaryComponents(Transform parent)
    {
        if (simulationSettings == null)
            simulationSettings = SimulationSettings.GetInstance();

        if (playerController == null)
            playerController = parent.GetComponent<ProxyNetworkPlayerMovementController>();

        if (animationController == null)
            animationController = parent.GetComponent<ProxyNetworkPlayerAnimationController>();
    }
}