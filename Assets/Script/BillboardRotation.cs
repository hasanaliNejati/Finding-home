using UnityEngine;

namespace Script
{
    /// <summary>
    /// Syncs the X rotation of this object with the camera's X rotation.
    /// Used for cards in top-down view to face the camera properly.
    /// </summary>
    public class BillboardRotation : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool syncOnStart = true;
        [SerializeField] private bool continuousSync = true;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (syncOnStart)
            {
                SyncRotation();
            }
        }

        private void LateUpdate()
        {
            if (continuousSync)
            {
                SyncRotation();
            }
        }

        private void SyncRotation()
        {
            if (targetCamera == null)
                return;

            // Get current rotation
            Vector3 currentRotation = transform.eulerAngles;
            
            // Get camera's X rotation
            float cameraXRotation = targetCamera.transform.eulerAngles.x;
            
            // Apply only X rotation from camera, keep Y and Z as is
            transform.eulerAngles = new Vector3(cameraXRotation, currentRotation.y, currentRotation.z);
        }

        /// <summary>
        /// Manually sync rotation (useful if continuousSync is disabled)
        /// </summary>
        public void ManualSync()
        {
            SyncRotation();
        }
    }
}

