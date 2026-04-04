using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class TurnAnimations : MonoBehaviour
{
    private Animator anim;

    private NavMeshAgent agent;

    private Quaternion startRotation;

    private Vector3 startPosition;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        startPosition = transform.position;
        startRotation = transform.rotation;
        
        StartCoroutine(DelayedSetDestination(2));
    }

    void Reset()
    {
        agent.isStopped = true;
        transform.position = startPosition;
        transform.rotation = startRotation;
        isGoingIdle = false;
        StartCoroutine(DelayedSetDestination(2));
    }

    [SerializeField] private float remainingDistanceIdleTransition = 2;
    [SerializeField] private float secondTravelDistance = 2;
    [SerializeField] private Transform target;
    private bool isGoingIdle = false;

    private bool isFacingPlayer = false;

    [SerializeField] private float rotationSpeed = 300;

    private bool turnToPlayer = false;
    // Update is called once per frame
    void Update()
    {
        if (agent.hasPath)
        {
            if (!isGoingIdle && agent.remainingDistance < remainingDistanceIdleTransition)
            {
                isGoingIdle = true;
                anim.SetTrigger("GoIdle");
            }
            

            if (isGoingIdle && agent.speed == 0)
            {
                turnToPlayer = true;
                
            }
        }

        if (turnToPlayer)
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            if (!isFacingPlayer)
            {
                Vector3 toPlayer = target.position - transform.position;

// Ignore vertical difference if needed
                toPlayer.y = 0;

                float dot = Vector3.Dot(transform.right, toPlayer);

                if (dot > 0.1f)
                {
                    // Player is to the RIGHT of NPC
                    anim.SetBool("TurningRight", true);
                }
                else if (dot < -0.1f)
                {
                    // Player is to the LEFT of NPC
                    anim.SetBool("TurningLeft", true);
                }
                else
                {
                    anim.SetBool("TurningLeft", false);
                    anim.SetBool("TurningRight", false);
                    isGoingIdle = false;
                    isFacingPlayer = true;
                    turnToPlayer = false;
                }
                    
                if (toPlayer.sqrMagnitude > 0.001f && !isFacingPlayer)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
    
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }
        
    }

    [SerializeField] private float travelDistance = 5;
    void SetDestination()
    {
        agent.SetDestination(transform.position + -transform.right * travelDistance);
        anim.SetTrigger("StartTurnLeft");
    }

    IEnumerator DelayedSetDestination(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        agent.isStopped = false;
        SetDestination();
    }

    private void OnAnimatorMove()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(anim.velocity);
        agent.speed = localVelocity.z;
        
    }
}
