using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MillionaireGame
{
    /// <summary>
    /// Creates a smooth, modern floating shape effect on a UI Canvas.
    /// Spawns a few UI elements that slowly float upwards, rotate, and fade in/out.
    /// </summary>
    public class FloatingShapesEffect : MonoBehaviour
    {
        [Header("Settings")]
        public int shapeCount = 15;
        public float minSpeed = 30f;
        public float maxSpeed = 80f;
        public float minSize = 20f;
        public float maxSize = 100f;

        private RectTransform _parent;
        private RectTransform[] _shapes;

        private void Start()
        {
            _parent = GetComponent<RectTransform>();
            if (_parent == null) return;

            _shapes = new RectTransform[shapeCount];

            for (int i = 0; i < shapeCount; i++)
            {
                _shapes[i] = CreateShape($"Shape_{i}");
                StartShapeCycle(_shapes[i], true);
            }
        }

        private RectTransform CreateShape(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            // Use standard circle or square (since we don't know accessible sprites, we just use a default white square with low alpha)
            img.color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.15f));

            float size = Random.Range(minSize, maxSize);
            rt.sizeDelta = new Vector2(size, size);

            return rt;
        }

        private void StartShapeCycle(RectTransform shape, bool initialSpawn)
        {
            float width = _parent.rect.width;
            float height = _parent.rect.height;

            float xPos = Random.Range(-width / 2f, width / 2f);
            // If it's the very first time, distribute them vertically so they don't all start at bottom
            float startY = initialSpawn ? Random.Range(-height / 2f, height / 2f) : (-height / 2f - 100f);
            float endY = height / 2f + 100f;

            shape.anchoredPosition = new Vector2(xPos, startY);

            float distance = endY - startY;
            float speed = Random.Range(minSpeed, maxSpeed);
            float duration = distance / speed;

            // Optional subtle wiggle in X
            float targetX = xPos + Random.Range(-100f, 100f);

            // Floating Tween
            Tween.UIAnchoredPosition(shape, new Vector2(targetX, endY), duration, Ease.Linear)
                .OnComplete(shape, (s) => StartShapeCycle(s, false));

            // Rotation Tween
            float rotDuration = Random.Range(5f, 10f);
            Tween.LocalRotation(shape, Quaternion.Euler(0, 0, Random.Range(-360f, 360f)), rotDuration, Ease.Linear, cycles: -1); // loop infinitely
        }
    }
}
