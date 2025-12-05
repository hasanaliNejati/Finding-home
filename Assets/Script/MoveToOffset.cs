using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script
{
    public class MoveToOffset : MonoBehaviour
    {
        [Title("Target Settings")]
        [Tooltip("The object to move. If null, this GameObject will be moved.")]
        [SerializeField] private Transform targetObject;
        
        [Title("Movement Settings")]
        [Tooltip("The offset to move to (relative to current position)")]
        [SerializeField] private Vector3 offset = Vector3.zero;
        
        [Tooltip("Duration of the movement in seconds")]
        [SerializeField] private float duration = 1f;
        
        [Title("Animation Settings")]
        [Tooltip("Easing type for the animation")]
        [SerializeField] private Ease easeType = Ease.OutQuad;
        
        [Tooltip("If true, the movement will be relative to local space instead of world space")]
        [SerializeField] private bool useLocalSpace = false;

        private Vector3 originalPosition;
        private bool hasMoved = false;

        private void Awake()
        {
            if (targetObject == null)
            {
                targetObject = transform;
            }
            
            originalPosition = useLocalSpace ? targetObject.localPosition : targetObject.position;
        }

        [Button("Move To Offset")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void MoveToOffsetPosition()
        {
            if (targetObject == null)
            {
                Debug.LogWarning("[MoveToOffset] Target object is null!");
                return;
            }

            Vector3 currentPosition = useLocalSpace ? targetObject.localPosition : targetObject.position;
            Vector3 targetPosition = currentPosition + offset;

            if (useLocalSpace)
            {
                targetObject.DOLocalMove(targetPosition, duration)
                    .SetEase(easeType);
            }
            else
            {
                targetObject.DOMove(targetPosition, duration)
                    .SetEase(easeType);
            }

            hasMoved = true;
        }

        [Button("Reset Position")]
        [GUIColor(1f, 0.6f, 0.4f)]
        [EnableIf("hasMoved")]
        private void ResetPosition()
        {
            if (targetObject == null)
            {
                Debug.LogWarning("[MoveToOffset] Target object is null!");
                return;
            }

            if (useLocalSpace)
            {
                targetObject.DOLocalMove(originalPosition, duration)
                    .SetEase(easeType);
            }
            else
            {
                targetObject.DOMove(originalPosition, duration)
                    .SetEase(easeType);
            }

            hasMoved = false;
        }

        private void OnDestroy()
        {
            if (targetObject != null)
            {
                targetObject.DOKill();
            }
        }
    }
}

