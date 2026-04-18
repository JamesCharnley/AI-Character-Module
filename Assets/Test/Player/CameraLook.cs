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
    [SerializeField] private Vector2 PunchOffsetResult;
    [SerializeField] private Vector2 DamageOffsetResult;
    [SerializeField] private NPCBehaviourController npcBehaviourController;
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Camera cam = GetComponent<Camera>();
            Vector2 punchZone = GetLookDirection(cam, DodgeTarget);
            //PunchOffsetResult = punchZone;
            //DamageOffsetResult = GetLookDirection(cam, DamageTarget);
            //npcBehaviourController.IncomingAttack(new Vector3(punchZone.x, punchZone.y, 0f));
            //npcBehaviourController.TakeDamage(20, transform.forward, DamageOffsetResult);
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    [SerializeField] private float horizontalThreshold = 0.1f;
    [SerializeField] private float verticalThreshold = 0.1f;
    public Vector2 GetVerticalLookZone(Transform reference, Camera cam, float horizontalCenterThreshold = 0.07f, float verticalCenterThreshold = 0.07f)
    {
        horizontalCenterThreshold = horizontalThreshold;
        verticalCenterThreshold = verticalThreshold;
        Vector3 toCamera = (cam.transform.position - reference.position).normalized;

        // Decouple horizontal and vertical checks so one axis does not suppress the other.
        Vector3 horizontalPlaneDirection = Vector3.ProjectOnPlane(toCamera, reference.up).normalized;
        Vector3 verticalPlaneDirection = Vector3.ProjectOnPlane(toCamera, reference.right).normalized;

        float rightDot = horizontalPlaneDirection.sqrMagnitude > 0f
            ? Vector3.Dot(reference.right, horizontalPlaneDirection)
            : 0f;
        float upDot = verticalPlaneDirection.sqrMagnitude > 0f
            ? Vector3.Dot(reference.up, verticalPlaneDirection)
            : 0f;

        float xZone = 0f;
        if (rightDot > horizontalCenterThreshold)
        {
            xZone = 1f;
        }
        else if (rightDot < -horizontalCenterThreshold)
        {
            xZone = -1f;
        }

        float yZone = 0f;
        if (upDot > verticalCenterThreshold)
        {
            yZone = 1f;
        }
        else if (upDot < -verticalCenterThreshold)
        {
            yZone = -1f;
        }
        
        Debug.LogWarning($"Dam: {new Vector2(xZone, yZone)}");
        return new Vector2(xZone, yZone);
    }
    public static Vector2 GetLookDirection(Camera cam, Transform target)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(target.position);

        if (viewportPoint.z <= 0f)
        {
            return Vector2.zero;
        }

        float xZone = viewportPoint.x < (1f / 3f)
            ? -1f
            : viewportPoint.x > (2f / 3f)
                ? 1f
                : 0f;

        float yZone = viewportPoint.y < (1f / 3f)
            ? -1f
            : viewportPoint.y > (2f / 3f)
                ? 1f
                : 0f;

        return new Vector2(xZone, yZone);
    }
}
