using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 스폰된 데미지 숫자 하나. 포물선을 그리며 떨어지고 페이드 아웃 후 자동 소멸.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageNumber : MonoBehaviour
{
    public void Play(string text, Vector2 startVelocity, float gravity, float duration, Color color)
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        StartCoroutine(Animate(startVelocity, gravity, duration, tmp));
    }

    private IEnumerator Animate(Vector2 velocity, float gravity, float duration, TextMeshProUGUI tmp)
    {
        var rect = GetComponent<RectTransform>();
        Color startColor = tmp.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            velocity.y -= gravity * dt;
            rect.anchoredPosition += velocity * dt;

            float alpha = 1f - (elapsed / duration);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}
