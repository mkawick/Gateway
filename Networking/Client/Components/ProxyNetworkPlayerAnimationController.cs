using Assets._Scripts.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProxyNetworkPlayerAnimationController : MonoBehaviour
{
    public Animator CurrentAnimatorComponent { set; get; }
    [SerializeField]
    private Animator humanAnimatorComponent;
    [SerializeField]
    private float SpeedMaxDeltaSeconds;
    [SerializeField]
    private float DirectionMaxDeltaSeconds;
    [SerializeField]
    private float LeanAngleMaxDeltaSeconds;

    private AnimatorOverrideController animatorOverrideController;
    private AnimatorStateInfo stateInfo;
    private AnimatorStateInfo nextStateInfo;
    private Vector3 previousMoveDirection;
    private Vector3 moveDirection;

    public float smoothingStep = 1f;

    // Speed
    private float currentSpeed;
    private float desiredSpeed;
    // X Direction (left/right steering input)
    private float currentXDirection;
    private float desiredXDirection;
    // Z Direction (forwards/back input)
    private float currentZDirection;
    private float desiredZDirection;
    // Lean angle (lean for quick turns with mouse look direction and keys)
    private float currentLeanAngle;
    private float desiredLeanAngle;

    private void Start()
    {
        if (humanAnimatorComponent == null)
        {
            Debug.LogError("Animator.Start(): humanAnimatorComponent is null");
        }

        // Initialize to Human creature first.
        SwitchCreatureAnimator(humanAnimatorComponent);

        currentLeanAngle = 0.0f;
        moveDirection = Vector3.zero;
    }

    private void Update()
    {
        if (CurrentAnimatorComponent.gameObject.activeInHierarchy)
        {
            // Get the current and next state infos from current animator component.
            stateInfo = CurrentAnimatorComponent.GetCurrentAnimatorStateInfo(0);
            nextStateInfo = CurrentAnimatorComponent.GetNextAnimatorStateInfo(0);


            // If the character is locomoting forwards...
            if (stateInfo.IsTag("ForwardsLocomotion"))
            {
                // Only update StopSpeed value if not transitioning into Stop (so it freezes the value on stop).
                if (!nextStateInfo.IsTag("Stop"))
                {
                    CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.SpeedBeforeStopHash, currentSpeed);
                }
                // Then compare moveDirection to previousMoveDirection and work out how much 
                // lean animation to add for mouse turning.
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    desiredLeanAngle = Vector3.Angle(moveDirection, previousMoveDirection);
                    // Check for left side anti-clockwise angles (negative lean value).
                    if (Vector3.Cross(moveDirection, previousMoveDirection).y > 0.0f)
                    {
                        desiredLeanAngle *= -1.0f;
                    }
                }
                else
                    desiredLeanAngle = 0;
            }
            // Otherwise zero leaning angle.
            else
            {
                desiredLeanAngle = 0.0f;
            }
        }
        // Boost lean angle if travelling diagonally forwards using the up + L/R move keys.
        if (desiredLeanAngle > -1.0f && desiredLeanAngle < 1.0f && currentZDirection > 0.8f)
        {
            if (currentXDirection > 0.8f)
            {
                desiredLeanAngle = 1.0f;
            }
            else if (currentXDirection < -0.8f)
            {
                desiredLeanAngle = -1.0f;
            }
        }

        if (CurrentAnimatorComponent.gameObject.activeInHierarchy)
        {
            // Move lean angle value towards desired and send to animator.
            currentLeanAngle = Mathf.MoveTowards(currentLeanAngle, desiredLeanAngle, LeanAngleMaxDeltaSeconds * Time.deltaTime);
            CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.LeanAngleHash, currentLeanAngle);
            // Send desired speed 'raw' to animator.
            CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.DesiredSpeedHash, desiredSpeed);

            // Move the speed and direction values towards their desired values.
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, SpeedMaxDeltaSeconds * Time.deltaTime);
            currentXDirection = Mathf.MoveTowards(currentXDirection, desiredXDirection, DirectionMaxDeltaSeconds * Time.deltaTime);
            currentZDirection = Mathf.MoveTowards(currentZDirection, desiredZDirection, DirectionMaxDeltaSeconds * Time.deltaTime);

            // Finally set the speed and direction values on the animator.
            CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.SpeedHash, currentSpeed);
            CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.DirectionXHash, currentXDirection);
            CurrentAnimatorComponent.SetFloat(AnimHashesNew.Locomotion.DirectionZHash, currentZDirection);
        }
    }

    public void SetDesiredSpeedAndDirection(float speed, Vector3 inputDirection, Vector3 moveDirection)
    {
        if (Mathf.Approximately(speed, 0f))
            SetDesiredSpeed(Mathf.MoveTowards(desiredSpeed, 0f, smoothingStep * Time.deltaTime));
        else
            SetDesiredSpeed(speed);

        if (Mathf.Approximately(inputDirection.x, 0f))
            desiredXDirection = Mathf.MoveTowards(desiredXDirection, 0f, smoothingStep * Time.deltaTime);
        else
            desiredXDirection = inputDirection.x;

        if (Mathf.Approximately(inputDirection.z, 0f))
            desiredZDirection = Mathf.MoveTowards(desiredZDirection, 0f, smoothingStep * Time.deltaTime);
        else
            desiredZDirection = inputDirection.z;

        // Remember old value to calculate an angle diff for lean L&R animations.
        previousMoveDirection = this.moveDirection;
        // We only want the direction in a 2d x/z plain.
        this.moveDirection = moveDirection.Flattened();
    }

    public void SetDesiredSpeed(float speed)
    {
        desiredSpeed = speed;
    }

    public void SetFalling(bool isFalling)
    {
        CurrentAnimatorComponent.SetBool(AnimHashesNew.Airborne.IsFallingHash, isFalling);
    }

    public void SwitchCreatureAnimator(Animator animator)
    {
        // Set new animator.
        CurrentAnimatorComponent = animator;
        // Create and set a new animation override controller, used to dynamically swap out action animation clips at runtime.
        animatorOverrideController = new AnimatorOverrideController(CurrentAnimatorComponent.runtimeAnimatorController);
        CurrentAnimatorComponent.runtimeAnimatorController = animatorOverrideController;
    }
}