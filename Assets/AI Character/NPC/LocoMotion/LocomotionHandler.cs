using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LocomotionHandler : MonoBehaviour
{

    private Animator anim;

    private NavMeshAgent agent;

    [SerializeField] private Transform target;

    [SerializeField] private bool walk01 = true;
    [SerializeField] private bool run02 = false;
    
    [SerializeField] private float Walk01GoIdleDistance = 3;
    [SerializeField] private float run01GoIdleDistance = 8.5f;
    
    private float goIdleDistance = 3;
    [SerializeField] private float goWalkDistance = 3;

    private bool isIdle = false;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (walk01)
        {
            goIdleDistance = Walk01GoIdleDistance;
        }
        else if (run02)
        {
            goIdleDistance = run01GoIdleDistance;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        agent.speed = anim.GetFloat("Speed");
        agent.SetDestination(target.position);
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= goIdleDistance)
            {
                Debug.Log($"RD {agent.remainingDistance}, GID {goIdleDistance} CD {dist}");
                if (!isIdle)
                {
                    anim.SetTrigger("Idle");
                    isIdle = true;
                }
            
            }
            else
            {
                if (isIdle && dist > goWalkDistance)
                {
                    anim.SetTrigger("Walk");
                    isIdle = false;
                }
            }
        }
        
    }

    private void OnAnimatorMove()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(anim.velocity);
        anim.SetFloat("Speed", localVelocity.z);
    }
}
