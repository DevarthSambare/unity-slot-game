using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Feel
{
    /// <summary>
    /// UI "particle" system for coins flying out on a win.
    /// Did this with pooled Images instead of a ParticleSystem because the canvas is
    /// Screen Space Overlay, where particle systems don't render on top of UI.
    /// </summary>
    public class CoinBurst : MonoBehaviour
    {
        [SerializeField] private Image coinPrefab;
        [SerializeField] private int poolSize = 80;

        [Header("Motion")]
        [SerializeField] private Vector2 launchSpeed = new Vector2(500f, 1100f);
        [SerializeField] private float spreadAngle = 70f;
        [SerializeField] private float gravity = 2200f;
        [SerializeField] private float lifetime = 1.4f;

        private class Coin
        {
            public Image Image;
            public RectTransform Rect;
            public Vector2 Velocity;
            public float Spin;
            public float Age;
            public float Scale;
        }

        private readonly List<Coin> _pool = new List<Coin>();
        private readonly List<Coin> _active = new List<Coin>();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                Image image = Instantiate(coinPrefab, transform);
                image.raycastTarget = false;
                image.gameObject.SetActive(false);
                _pool.Add(new Coin { Image = image, Rect = image.rectTransform });
            }
        }

        /// <summary>Fires count coins upward from a point in this object's local space.</summary>
        public void Burst(Vector2 localOrigin, int count)
        {
            for (int i = 0; i < count && _pool.Count > 0; i++)
            {
                Coin coin = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);

                float angle = (90f + Random.Range(-spreadAngle, spreadAngle)) * Mathf.Deg2Rad;
                float speed = Random.Range(launchSpeed.x, launchSpeed.y);

                coin.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                coin.Spin = Random.Range(-720f, 720f);
                coin.Age = 0f;
                coin.Scale = Random.Range(0.7f, 1.15f);
                coin.Rect.anchoredPosition = localOrigin + Random.insideUnitCircle * 30f;
                coin.Rect.localRotation = Quaternion.identity;
                coin.Image.color = Color.white;
                coin.Image.gameObject.SetActive(true);
                _active.Add(coin);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Coin coin = _active[i];
                coin.Age += dt;

                if (coin.Age >= lifetime)
                {
                    coin.Image.gameObject.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Add(coin);
                    continue;
                }

                coin.Velocity.y -= gravity * dt;
                coin.Rect.anchoredPosition += coin.Velocity * dt;
                coin.Rect.Rotate(0f, 0f, coin.Spin * dt);

                // fake a 3D flip by squashing X, reads as a spinning coin
                float flip = Mathf.Abs(Mathf.Cos(coin.Age * 9f + coin.Spin));
                coin.Rect.localScale = new Vector3(coin.Scale * Mathf.Max(0.15f, flip), coin.Scale, 1f);

                // fade only in the last 30% of its life
                float life01 = coin.Age / lifetime;
                var c = coin.Image.color;
                c.a = life01 < 0.7f ? 1f : 1f - (life01 - 0.7f) / 0.3f;
                coin.Image.color = c;
            }
        }
    }
}
