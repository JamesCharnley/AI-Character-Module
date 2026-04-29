using System.Collections;
using System.Collections.Generic;
using AICharacterModule.NPC.StateMachine.Core;
using FirstPersonCharacter;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, ITakeDamage
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpForce = 1.5f;
    public float sprintMulti = 1.5f;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting = false;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    // external velocity (impulses, knockbacks, etc.)
    private Vector3 externalVelocity;
    [SerializeField] private float MaxHealth = 100;
    private float CurrentHealth = 100;
    public bool IsMovementInputLocked { get; private set; }
    

    public float drag = 5f;        // how fast impulse dies off
    [Header("Push To Ground")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float pushBackDistance = 2.2f;
    [SerializeField] private float pushDownDistance = 0.9f;
    [SerializeField] private float pushDuration = 0.28f;
    [SerializeField] private float standUpDuration = 0.45f;
    [SerializeField] private AnimationCurve pushSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve cameraGroundMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve cameraLookUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float cameraLookUpDegrees = 14f;
    private Vector3 initialCameraLocalPosition;
    private Quaternion initialCameraLocalRotation;
    private Coroutine pushRoutine;
    private Coroutine standUpRoutine;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        if (cameraRoot != null)
        {
            initialCameraLocalPosition = cameraRoot.localPosition;
            initialCameraLocalRotation = cameraRoot.localRotation;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            TriggerPushToGround();
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            TriggerStandUp();
        }
        isSprinting = !IsMovementInputLocked && Input.GetKey(KeyCode.LeftShift);
        HandleMovement();
        float dt = Time.deltaTime;

        // Apply gravity
        externalVelocity.y += gravity * dt;

        // Move character using external velocity
        controller.Move(externalVelocity * dt);

        // Apply drag (decay impulse)
        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, drag * dt);

        // Optional: stop tiny values to prevent micro sliding
        if (externalVelocity.magnitude < 0.01f)
            externalVelocity = Vector3.zero;
    }
    
    public void AddImpulse(Vector3 impulse)
    {
        externalVelocity += impulse;
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Input
        float x = IsMovementInputLocked ? 0f : Input.GetAxis("Horizontal");
        float z = IsMovementInputLocked ? 0f : Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float speedBuff = isSprinting ? sprintMulti : 1;
        controller.Move(move * (moveSpeed * speedBuff * Time.deltaTime));

        // Jump
        if (!IsMovementInputLocked && Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        
        controller.Move(velocity * Time.deltaTime);
    }

    public void TriggerPushToGround()
    {
        if (pushRoutine != null)
            StopCoroutine(pushRoutine);
        if (standUpRoutine != null)
            StopCoroutine(standUpRoutine);
        pushRoutine = StartCoroutine(PushToGroundRoutine());
    }

    public void TriggerStandUp()
    {
        if (cameraRoot == null)
        {
            IsMovementInputLocked = false;
            return;
        }

        if (standUpRoutine != null)
            StopCoroutine(standUpRoutine);
        standUpRoutine = StartCoroutine(StandUpRoutine());
    }

    private IEnumerator PushToGroundRoutine()
    {
        IsMovementInputLocked = true;
        float elapsed = 0f;
        float movedBack = 0f;
        float movedDown = 0f;

        while (elapsed < pushDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pushDuration);
            float pushCurveValue = Mathf.Clamp01(pushSpeedCurve.Evaluate(t));
            float cameraGroundCurveValue = Mathf.Clamp01(cameraGroundMoveCurve.Evaluate(t));
            float targetBack = pushBackDistance * pushCurveValue;
            float targetDown = pushDownDistance * pushCurveValue;

            float deltaBack = targetBack - movedBack;
            float deltaDown = targetDown - movedDown;
            movedBack = targetBack;
            movedDown = targetDown;

            Vector3 move = (-transform.forward * deltaBack) + (Vector3.down * deltaDown);
            controller.Move(move);

            if (cameraRoot != null)
            {
                Vector3 cameraPos = cameraRoot.localPosition;
                cameraPos.y = Mathf.Lerp(initialCameraLocalPosition.y, initialCameraLocalPosition.y - pushDownDistance, cameraGroundCurveValue);
                cameraRoot.localPosition = cameraPos;

                float lookUpCurveValue = Mathf.Clamp01(cameraLookUpCurve.Evaluate(t));
                Quaternion lookUpRotation = Quaternion.Euler(-cameraLookUpDegrees * lookUpCurveValue, 0f, 0f);
                cameraRoot.localRotation = initialCameraLocalRotation * lookUpRotation;
            }

            yield return null;
        }

        pushRoutine = null;
    }

    private IEnumerator StandUpRoutine()
    {
        float elapsed = 0f;
        Vector3 startCamPos = cameraRoot.localPosition;

        while (elapsed < standUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / standUpDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            cameraRoot.localPosition = Vector3.Lerp(startCamPos, initialCameraLocalPosition, eased);
            cameraRoot.localRotation = Quaternion.Slerp(cameraRoot.localRotation, initialCameraLocalRotation, eased);
            yield return null;
        }

        cameraRoot.localPosition = initialCameraLocalPosition;
        cameraRoot.localRotation = initialCameraLocalRotation;
        IsMovementInputLocked = false;
        standUpRoutine = null;
    }


    public void TakeDamage(float _amount)
    {
        throw new System.NotImplementedException();
    }

    private bool damageSoundCooldown = false;
    [SerializeField] private AudioClip[] LightFaceHits;
    [SerializeField] private AudioClip[] HeavyFaceHits;
    [SerializeField] private FirstPersonPunchRig PunchRig;
    [SerializeField] private float lungeBackDistance;
    [SerializeField] private float upDegrees;
    public void TakeDamage(float _amount, Vector3 _direction, Vector3 _offset)
    {
        Debug.LogError("Player take damage");
        
        CurrentHealth -= _amount;
        if (CurrentHealth <= 0)
        {
            Debug.Log("PLAYER DEAD");
            CurrentHealth = MaxHealth;
            return;
        }
        AddImpulse(_direction);
        PunchRig.TriggerCameraPunchReaction(lungeBackDistance, upDegrees);
        PunchRig.TriggerBlockHitReaction();
        if (!damageSoundCooldown)
        {
            damageSoundCooldown = true;
            StartCoroutine(DamageSoundCooldown());
            int maxRand = _direction.magnitude < 2 ? LightFaceHits.Length : HeavyFaceHits.Length;
            AudioClip clip = _direction.magnitude < 2
                ? LightFaceHits[Random.Range(0, maxRand)]
                : HeavyFaceHits[Random.Range(0, maxRand)];
            AudioSource.PlayClipAtPoint(clip, transform.position + transform.up + transform.forward, 0.7f);
        }
        
        
    }

    public void TakeDamage(float _amount, Vector3 _direction, HitZoneInfo _hitZoneInfo)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator DamageSoundCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        damageSoundCooldown = false;
    }
}
