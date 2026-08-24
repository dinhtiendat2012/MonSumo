using UnityEngine;

namespace MonSumo.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundCameraFitter : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Camera _mainCamera;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            FitBackground();
        }

        private void LateUpdate()
        {
            FitBackground();
        }

        public void FitBackground()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_spriteRenderer == null || _mainCamera == null) return;

            Sprite sprite = _spriteRenderer.sprite;
            if (sprite == null) return;

            // Get camera height and width in world units
            float cameraHeight = _mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _mainCamera.aspect;

            // Get sprite size in world units
            Vector2 spriteSize = sprite.rect.size / sprite.pixelsPerUnit;

            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            // Calculate required scale to cover the screen
            float scaleX = cameraWidth / spriteSize.x;
            float scaleY = cameraHeight / spriteSize.y;

            // Cover option (Mathf.Max) ensures no black borders are visible
            float coverScale = Mathf.Max(scaleX, scaleY);

            // Apply scale, compensating for parent scale if any (like the Canvas scale of 0.01)
            Vector3 targetLocalScale = new Vector3(coverScale, coverScale, 1f);
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                if (parentScale.x > 0f && parentScale.y > 0f)
                {
                    targetLocalScale.x /= parentScale.x;
                    targetLocalScale.y /= parentScale.y;
                }
            }

            transform.localScale = targetLocalScale;
        }
    }
}
