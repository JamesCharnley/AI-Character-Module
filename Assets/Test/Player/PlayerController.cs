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
    

    public float drag = 5f;        // how fast impulse dies off
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift);
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
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float speedBuff = isSprinting ? sprintMulti : 1;
        controller.Move(move * (moveSpeed * speedBuff * Time.deltaTime));

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        
        controller.Move(velocity * Time.deltaTime);
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
