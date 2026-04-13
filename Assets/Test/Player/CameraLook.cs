using System.Collections;
using System.Collections.Generic;
using AICharacterModule.NPC;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    [FormerlySerializedAs("PunchTarget")] [SerializeField] private Transform DodgeTarget;
    [SerializeField] private Transform DamageTarget;
    private float xRotation = 0f;
    [SerializeField] private Vector3 PunchOffsetResult;
    [SerializeField] private Vector3 DamageOffsetResult;
    [SerializeField] private NPCBehaviourController npcBehaviourController;
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Camera cam = GetComponent<Camera>();
            PunchOffsetResult = GetLookDirection(cam, DodgeTarget);
            DamageOffsetResult = GetLookDirection(cam, DamageTarget);
            npcBehaviourController.IncomingAttack(GetLookDirection(cam, DodgeTarget));
            npcBehaviourController.TakeDamage(20, DamageOffsetResult);
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
    public static Vector3 GetLookDirection(Camera cam, Transform target, float centerThreshold = 0.99f)
    {
        Vector3 toTarget = (target.position - cam.transform.position).normalized;

        float forwardDot = Vector3.Dot(cam.transform.forward, toTarget);

        // 1. Check if looking directly at target
        if (forwardDot >= centerThreshold)
        {
            return new Vector3(0f, 0f, -1f);
        }

        // 2. Compare horizontal vs vertical deviation
        float rightDot = Vector3.Dot(cam.transform.right, toTarget);
        float upDot = Vector3.Dot(cam.transform.up, toTarget);

        // Decide dominant axis (whichever is stronger)
        if (Mathf.Abs(rightDot) > Mathf.Abs(upDot))
        {
            // Horizontal
            return new Vector3(Mathf.Sign(rightDot), 0f, 0f);
        }
        else
        {
            // Vertical
            return new Vector3(0f, -Mathf.Sign(upDot), 0f);
        }
    }
}