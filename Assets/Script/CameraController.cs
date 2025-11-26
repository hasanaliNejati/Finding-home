using UnityEngine;
using UnityEngine.EventSystems;

namespace Script
{
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [Tooltip("Mouse button for panning (0=Left, 1=Right, 2=Middle)")]
        [SerializeField] private int panButton = 0;
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private bool invertPan = false;

        [Header("Zoom Settings (Perspective)")]
        [Tooltip("For perspective camera: controls Z position")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoomDistance = -20f;
        [SerializeField] private float maxZoomDistance = -2f;
        
        [Header("Zoom Settings (Orthographic)")]
        [Tooltip("For orthographic camera: controls orthographic size")]
        [SerializeField] private float minOrthoSize = 2f;
        [SerializeField] private float maxOrthoSize = 20f;
        
        [SerializeField] private float zoomSmoothSpeed = 10f;

        [Header("Bounds (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

        private Camera cam;
        private Vector3 lastMousePosition;
        private bool isPanning = false;
        private float targetZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }
            
            // Initialize target zoom based on camera type
            if (cam.orthographic)
            {
                targetZoom = cam.orthographicSize;
            }
            else
            {
                targetZoom = transform.position.z;
            }
        }

        private void Update()
        {
            HandlePanning();
            HandleZoom();
        }

        private void HandlePanning()
        {
            // Check if mouse is over UI element
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // If we're not already panning, don't start panning over UI
                if (!isPanning)
                {
                    return;
                }
            }

            // Start panning
            if (Input.GetMouseButtonDown(panButton))
            {
                // Double check we're not clicking on UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }

            // Stop panning
            if (Input.GetMouseButtonUp(panButton))
            {
                isPanning = false;
            }

            // Perform panning
            if (isPanning && Input.GetMouseButton(panButton))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                Vector3 difference = currentMousePosition - lastMousePosition;

                // Convert screen movement to world movement
                float screenToWorldRatio;
                if (cam.orthographic)
                {
                    screenToWorldRatio = cam.orthographicSize * 2f / Screen.height;
                }
                else
                {
                    // For perspective camera, calculate based on distance from camera
                    float distance = Mathf.Abs(transform.position.z);
                    screenToWorldRatio = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height;
                }
                
                Vector3 move = new Vector3(
                    -difference.x * screenToWorldRatio * panSpeed,
                    -difference.y * screenToWorldRatio * panSpeed,
                    0f
                );

                if (invertPan)
                {
                    move = -move;
                }

                // Apply movement
                transform.position += move;

                // Clamp to bounds if enabled
                if (useBounds)
                {
                    ClampToBounds();
                }

                lastMousePosition = currentMousePosition;
            }
        }

        private void HandleZoom()
        {
            // Get scroll input
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (cam.orthographic)
            {
                // Orthographic camera - adjust orthographic size
                if (scrollInput != 0f)
                {
                    targetZoom -= scrollInput * zoomSpeed;
                    targetZoom = Mathf.Clamp(targetZoom, minOrthoSize, maxOrthoSize);
                }

                if (!Mathf.Approximately(cam.orthographicSize, targetZoom))
                {
                    cam.orthographicSize = Mathf.Lerp(
                        cam.orthographicSize, 
                        targetZoom, 
                        Time.deltaTime * zoomSmoothSpeed
                    );

                    if (useBounds)
                    {
                        ClampToBounds();
                    }
                }
            }
            else
            {
                // Perspective camera - adjust Z position
                if (scrollInput != 0f)
                {
                    targetZoom += scrollInput * zoomSpeed;
                    targetZoom = Mathf.Clamp(targetZoom, minZoomDistance, maxZoomDistance);
                }

                float currentZ = transform.position.z;
                if (!Mathf.Approximately(currentZ, targetZoom))
                {
                    Vector3 pos = transform.position;
                    pos.z = Mathf.Lerp(currentZ, targetZoom, Time.deltaTime * zoomSmoothSpeed);
                    transform.position = pos;

                    if (useBounds)
                    {
                        ClampToBounds();
                    }
                }
            }
        }

        private void ClampToBounds()
        {
            Vector3 pos = transform.position;
            
            // Calculate visible area based on orthographic size
            float verticalSize = cam.orthographicSize;
            float horizontalSize = verticalSize * cam.aspect;

            // Clamp position so camera doesn't go outside bounds
            pos.x = Mathf.Clamp(pos.x, minBounds.x + horizontalSize, maxBounds.x - horizontalSize);
            pos.y = Mathf.Clamp(pos.y, minBounds.y + verticalSize, maxBounds.y - verticalSize);

            transform.position = pos;
        }

        /// <summary>
        /// Set camera position directly
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            
            if (useBounds)
            {
                ClampToBounds();
            }
        }

        /// <summary>
        /// Set zoom level directly
        /// </summary>
        public void SetZoom(float zoom)
        {
            if (cam.orthographic)
            {
                targetZoom = Mathf.Clamp(zoom, minOrthoSize, maxOrthoSize);
                cam.orthographicSize = targetZoom;
            }
            else
            {
                targetZoom = Mathf.Clamp(zoom, minZoomDistance, maxZoomDistance);
                Vector3 pos = transform.position;
                pos.z = targetZoom;
                transform.position = pos;
            }
        }

        /// <summary>
        /// Reset camera to default position and zoom
        /// </summary>
        public void ResetCamera()
        {
            if (cam.orthographic)
            {
                transform.position = new Vector3(0, 0, transform.position.z);
                targetZoom = (minOrthoSize + maxOrthoSize) / 2f;
                cam.orthographicSize = targetZoom;
            }
            else
            {
                targetZoom = (minZoomDistance + maxZoomDistance) / 2f;
                transform.position = new Vector3(0, 0, targetZoom);
            }
        }
    }
}

