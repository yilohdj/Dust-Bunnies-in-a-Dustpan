using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform camHead;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float camTransitionSpeed = 3f;
    
    [Header("Locked Overhead Settings")]
    [SerializeField] private float overheadForwardOffset = 0.3f;
    [SerializeField] private float overheadDownAngle = 60f;
        [Header("Locked Look Around Settings")]
    [SerializeField] private float lookAroundTiltAngle = 15f;
    [SerializeField] private float lookAroundSideOffset = 0.2f;

    enum CamState {
        Free,
        LockedOverhead,
        ReturnFree,
        LockedLookAroundLeft,
        LockedLookAroundRight
    }
    
    private CamState currentCamState = CamState.Free;
    private Vector3 originalCamLocalPos;
    private float originalFOV;


    void Start() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        originalCamLocalPos = camHead.localPosition;
        if (playerCamera != null)
            originalFOV = playerCamera.fieldOfView;
    }

    // Update is called once per frame
    void Update() {
        Vector3 move = new(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        move = transform.TransformDirection(move);
        move.Normalize();
        characterController.SimpleMove(move * speed);

        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotationSpeed);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            SetCamStateLookAroundLeft();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            SetCamStateLookAroundRight();
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            SetCamStateLockedOverhead();
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            SetCamStateReturnFree();
        }
    }

    void LateUpdate() {
        switch (currentCamState) {
            case CamState.Free:
                CamFree();
                break;
            case CamState.LockedOverhead:
                CamLockedOverhead();
                break;
            case CamState.ReturnFree:
                CamReturnFree();
                break;
            case CamState.LockedLookAroundLeft:
            case CamState.LockedLookAroundRight:
                CamLookAround();
                break;
        }
    }
    
    private void CamFree() {
        Vector3 e = camHead.localEulerAngles;
        e.x -= Input.GetAxis("Mouse Y") * rotationSpeed;
        e.x = RestrictAngle(e.x, -85f, 85f);
        camHead.localEulerAngles = e;
        
        // Smoothly return to original position
        if (camHead.localPosition != originalCamLocalPos) {
            camHead.localPosition = Vector3.Lerp(camHead.localPosition, originalCamLocalPos, Time.deltaTime * camTransitionSpeed);
        }
        
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, Time.deltaTime * camTransitionSpeed);
    }
    
    private void CamLockedOverhead() {
        // Move camera slightly forward to lean over desk
        Vector3 targetPos = originalCamLocalPos + new Vector3(0, 0, overheadForwardOffset);
        if (camHead.localPosition != targetPos) {
            camHead.localPosition = Vector3.Lerp(camHead.localPosition, targetPos, Time.deltaTime * camTransitionSpeed);
        }
        
        // Pitch down to look at desk
        Vector3 e = camHead.localEulerAngles;
        e.x = Mathf.LerpAngle(e.x, overheadDownAngle, Time.deltaTime * camTransitionSpeed);
        e.z = Mathf.LerpAngle(e.z, 0f, Time.deltaTime * camTransitionSpeed);
        camHead.localEulerAngles = e;
        
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, Time.deltaTime * camTransitionSpeed);
    }

    private void CamReturnFree() {
        // Return to original position
        camHead.localPosition = Vector3.Lerp(camHead.localPosition, originalCamLocalPos, Time.deltaTime * camTransitionSpeed);
        
        // Return to looking straight ahead
        Vector3 e = camHead.localEulerAngles;
        e.x = Mathf.LerpAngle(e.x, 0f, Time.deltaTime * camTransitionSpeed);
        e.z = Mathf.LerpAngle(e.z, 0f, Time.deltaTime * camTransitionSpeed);
        camHead.localEulerAngles = e;
        
        // Return to original FOV
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, Time.deltaTime * camTransitionSpeed);
        
        // Check if close enough to original position and angle to switch to Free state
        if (Vector3.Distance(camHead.localPosition, originalCamLocalPos) < 0.01f &&
            Mathf.Abs(Mathf.DeltaAngle(camHead.localEulerAngles.x, 0f)) < 0.5f) {
            currentCamState = CamState.Free;
        }
    }

    private void CamLookAround() {
        Vector3 targetPos = originalCamLocalPos + new Vector3(
            currentCamState == CamState.LockedLookAroundLeft ? -lookAroundSideOffset : lookAroundSideOffset,
            0,
            0);
        if (camHead.localPosition != targetPos) {
            camHead.localPosition = Vector3.Lerp(camHead.localPosition, targetPos, Time.deltaTime * camTransitionSpeed);
        }

        Vector3 e = camHead.localEulerAngles;
        float targetTilt = currentCamState == CamState.LockedLookAroundLeft ? lookAroundTiltAngle : -lookAroundTiltAngle;
        e.z = Mathf.LerpAngle(e.z, targetTilt, Time.deltaTime * camTransitionSpeed);
        e.x = Mathf.LerpAngle(e.x, 0f, Time.deltaTime * camTransitionSpeed);
        camHead.localEulerAngles = e;
    }
    

    public void SetCamStateFree() => currentCamState = CamState.Free;
    public void SetCamStateLockedOverhead() => currentCamState = CamState.LockedOverhead;
    public void SetCamStateReturnFree() => currentCamState = CamState.ReturnFree;
    public void SetCamStateLookAroundLeft() => currentCamState = CamState.LockedLookAroundLeft;
    public void SetCamStateLookAroundRight() => currentCamState = CamState.LockedLookAroundRight;

    /// <summary>
    /// Helper method for head rotation, to prevent bending backwards
    /// </summary>
    private float RestrictAngle(float angle, float min, float max) {
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        if (angle > max)
            angle = max;
        if (angle < min)
            angle = min;

        return angle;
    }
}
