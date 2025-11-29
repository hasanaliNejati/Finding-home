using UnityEngine;
using UnityEngine.EventSystems;

namespace Script
{
    /// <summary>
    /// Standard top-down camera controller compatible with Cinemachine 3.x
    /// Works like professional RTS/strategy games (Age of Empires, StarCraft, Dota)
    /// - Fixed angle looking at a target point
    /// - Zoom changes DISTANCE from target (not just Y position)
    /// - Pan moves the target point on ground plane
    /// </summary>
    public class CinemachineTopDownController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("The camera angle in degrees (45° is standard for RTS games)")]
        [SerializeField] private float cameraAngle = 45f;
        
        [Tooltip("Rotation around Y axis (0 = looking north)")]
        [SerializeField] private float cameraRotation = 0f;

        [Header("Dynamic Angle (Optional)")]
        [Tooltip("Enable dynamic angle that changes with zoom")]
        [SerializeField] private bool useDynamicAngle = false;
        
        [Tooltip("Angle when zoomed in (closer to ground)")]
        [SerializeField] private float minAngle = 30f;
        
        [Tooltip("Angle when zoomed out (more top-down)")]
        [SerializeField] private float maxAngle = 60f;

        [Header("Zoom Settings")]
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float zoomSmoothTime = 0.1f;

        [Header("Pan Settings")]
        [SerializeField] private int panMouseButton = 0; // 0=Left, 1=Right, 2=Middle
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private float panSmoothTime = 0.1f;

        [Header("Keyboard Pan (Optional)")]
        [SerializeField] private bool enableKeyboardPan = true;
        [SerializeField] private float keyboardPanSpeed = 10f;
        [SerializeField] private KeyCode panUpKey = KeyCode.W;
        [SerializeField] private KeyCode panDownKey = KeyCode.S;
        [SerializeField] private KeyCode panLeftKey = KeyCode.A;
        [SerializeField] private KeyCode panRightKey = KeyCode.D;

        [Header("Edge Pan (Optional)")]
        [SerializeField] private bool enableEdgePan = false;
        [SerializeField] private float edgePanSpeed = 10f;
        [SerializeField] private float edgePanBorder = 20f; // pixels from screen edge

        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

        [Header("References (Optional)")]
        [Tooltip("Target point on the ground (will be auto-created if empty)")]
        [SerializeField] private Transform targetPoint;

        // Private state
        private float currentDistance;
        private float targetDistance;
        private float zoomVelocity;
        
        private Vector3 targetPosition;
        private Vector3 panVelocity;
        
        private bool isPanning;
        private Vector3 lastMousePosition;
        
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            
            // Create target point if not assigned
            if (targetPoint == null)
            {
                GameObject target = new GameObject("CameraTarget");
                targetPoint = target.transform;
                targetPoint.position = Vector3.zero;
            }

            // Initialize
            targetPosition = targetPoint.position;
            currentDistance = (minDistance + maxDistance) / 2f;
            targetDistance = currentDistance;
            
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            // Calculate effective angle (dynamic or fixed)
            float effectiveAngle = cameraAngle;
            
            if (useDynamicAngle)
            {
                // Interpolate angle based on current distance
                // Close (minDistance) → minAngle (more horizontal view)
                // Far (maxDistance) → maxAngle (more top-down view)
                float t = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
                effectiveAngle = Mathf.Lerp(minAngle, maxAngle, t);
            }

            // Calculate camera position based on angle and distance
            float angleRad = effectiveAngle * Mathf.Deg2Rad;
            float rotationRad = cameraRotation * Mathf.Deg2Rad;

            // Height and horizontal distance from target
            float height = currentDistance * Mathf.Sin(angleRad);
            float horizontalDist = currentDistance * Mathf.Cos(angleRad);

            // Calculate offset from target
            Vector3 offset = new Vector3(
                horizontalDist * Mathf.Sin(rotationRad),
                height,
                -horizontalDist * Mathf.Cos(rotationRad)
            );

            // Position camera relative to target
            transform.position = targetPoint.position + offset;

            // Look at target
            transform.LookAt(targetPoint);
        }

        private void Update()
        {
            HandleZoom();
            HandleMousePan();
            HandleKeyboardPan();
            HandleEdgePan();
            ApplyTargetPosition();
        }

        private void LateUpdate()
        {
            // Smooth zoom
            if (!Mathf.Approximately(currentDistance, targetDistance))
            {
                currentDistance = Mathf.SmoothDamp(
                    currentDistance, 
                    targetDistance, 
                    ref zoomVelocity, 
                    zoomSmoothTime
                );
            }
            
            UpdateCameraPosition();
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                // Zoom in/out by changing DISTANCE from target
                targetDistance -= scroll * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        private void HandleMousePan()
        {
            // Check if over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (!isPanning) return;
            }

            // Start panning
            if (Input.GetMouseButtonDown(panMouseButton))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;
                
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }

            // Stop panning
            if (Input.GetMouseButtonUp(panMouseButton))
            {
                isPanning = false;
            }

            // Perform panning
            if (isPanning && Input.GetMouseButton(panMouseButton))
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                // Convert screen movement to world movement
                if (mainCamera != null)
                {
                    // Calculate world movement based on camera distance and FOV
                    float worldHeight = 2f * currentDistance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    float worldWidth = worldHeight * mainCamera.aspect;
                    
                    float moveX = -(mouseDelta.x / Screen.width) * worldWidth * panSpeed;
                    float moveZ = -(mouseDelta.y / Screen.height) * worldHeight * panSpeed;
                    
                    // Apply movement relative to camera rotation
                    float rotRad = cameraRotation * Mathf.Deg2Rad;
                    Vector3 move = new Vector3(
                        moveX * Mathf.Cos(rotRad) - moveZ * Mathf.Sin(rotRad),
                        0f,
                        moveX * Mathf.Sin(rotRad) + moveZ * Mathf.Cos(rotRad)
                    );
                    
                    targetPosition += move;
                }
                
                lastMousePosition = Input.mousePosition;
            }
        }

        private void HandleKeyboardPan()
        {
            if (!enableKeyboardPan) return;

            Vector3 move = Vector3.zero;

            if (Input.GetKey(panUpKey)) move.z += 1f;
            if (Input.GetKey(panDownKey)) move.z -= 1f;
            if (Input.GetKey(panLeftKey)) move.x -= 1f;
            if (Input.GetKey(panRightKey)) move.x += 1f;

            if (move != Vector3.zero)
            {
                // Apply movement relative to camera rotation
                float rotRad = cameraRotation * Mathf.Deg2Rad;
                Vector3 rotatedMove = new Vector3(
                    move.x * Mathf.Cos(rotRad) - move.z * Mathf.Sin(rotRad),
                    0f,
                    move.x * Mathf.Sin(rotRad) + move.z * Mathf.Cos(rotRad)
                );
                
                targetPosition += rotatedMove * keyboardPanSpeed * Time.deltaTime;
            }
        }

        private void HandleEdgePan()
        {
            if (!enableEdgePan) return;

            Vector3 move = Vector3.zero;
            Vector3 mousePos = Input.mousePosition;

            if (mousePos.x < edgePanBorder) move.x -= 1f;
            if (mousePos.x > Screen.width - edgePanBorder) move.x += 1f;
            if (mousePos.y < edgePanBorder) move.z -= 1f;
            if (mousePos.y > Screen.height - edgePanBorder) move.z += 1f;

            if (move != Vector3.zero)
            {
                // Apply movement relative to camera rotation
                float rotRad = cameraRotation * Mathf.Deg2Rad;
                Vector3 rotatedMove = new Vector3(
                    move.x * Mathf.Cos(rotRad) - move.z * Mathf.Sin(rotRad),
                    0f,
                    move.x * Mathf.Sin(rotRad) + move.z * Mathf.Cos(rotRad)
                );
                
                targetPosition += rotatedMove * edgePanSpeed * Time.deltaTime;
            }
        }

        private void ApplyTargetPosition()
        {
            // Clamp to bounds
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
            }

            // Smooth movement
            targetPoint.position = Vector3.SmoothDamp(
                targetPoint.position,
                targetPosition,
                ref panVelocity,
                panSmoothTime
            );
        }

        // Public API
        public void SetCameraAngle(float angle)
        {
            cameraAngle = Mathf.Clamp(angle, 10f, 89f);
        }

        public void SetCameraRotation(float rotation)
        {
            cameraRotation = rotation % 360f;
        }

        public void SetTargetPosition(Vector3 position)
        {
            targetPosition = position;
            targetPosition.y = 0f;
        }

        public void SetZoom(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        public void FocusOnPoint(Vector3 worldPosition, float? distance = null)
        {
            SetTargetPosition(worldPosition);
            if (distance.HasValue)
            {
                SetZoom(distance.Value);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            // Draw bounds rectangle
            Gizmos.color = Color.yellow;
            Vector3 bottomLeft = new Vector3(minBounds.x, 0, minBounds.y);
            Vector3 bottomRight = new Vector3(maxBounds.x, 0, minBounds.y);
            Vector3 topLeft = new Vector3(minBounds.x, 0, maxBounds.y);
            Vector3 topRight = new Vector3(maxBounds.x, 0, maxBounds.y);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
