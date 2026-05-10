using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 이벤트를 수신해 숫자를 포물선으로 스폰한다.
/// Canvas 위에 배치하고 인스펙터에서 적/플레이어 스폰 앵커를 지정한다.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [Tooltip("적 데미지 숫자가 스폰될 RectTransform 위치")]
    [SerializeField] private RectTransform enemyAnchor;
    [Tooltip("플레이어 데미지 숫자가 스폰될 RectTransform 위치")]
    [SerializeField] private RectTransform playerAnchor;

    [Header("Motion")]
    [SerializeField] private float yForce = 250f;
    [SerializeField] private float xRange = 100f;
    [SerializeField] private float gravity = 500f;
    [SerializeField] private float duration = 0.9f;

    [Header("Style")]
    [SerializeField] private float fontSize = 48f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] private Color enemyDamageColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.85f, 0.15f);
    [SerializeField] private Color shieldAbsorbColor = new Color(0.4f, 0.8f, 1f);

    private void OnEnable()
    {
        GameEvents.OnDamageDealtToEnemy      += HandleDamageToEnemy;
        GameEvents.OnDamageDealtToPlayer     += HandleDamageToPlayer;
        GameEvents.OnDamageAbsorbedByPlayer  += HandleShieldAbsorb;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>()?.rootCanvas ?? FindAnyObjectByType<Canvas>();
    }

    private void OnDisable()
    {
        GameEvents.OnDamageDealtToEnemy     -= HandleDamageToEnemy;
        GameEvents.OnDamageDealtToPlayer    -= HandleDamageToPlayer;
        GameEvents.OnDamageAbsorbedByPlayer -= HandleShieldAbsorb;
    }

    private void HandleDamageToEnemy(int damage)  => Spawn(damage, enemyAnchor,  enemyDamageColor);
    private void HandleDamageToPlayer(int damage) => Spawn(damage, playerAnchor, playerDamageColor);
    private void HandleShieldAbsorb(int absorbed) => Spawn(absorbed, playerAnchor, shieldAbsorbColor);

    private void Spawn(int damage, RectTransform anchor, Color color)
    {
        if (canvas == null || anchor == null) return;

        var go = new GameObject("DamageNumber",
            typeof(RectTransform), typeof(TextMeshProUGUI), typeof(DamageNumber));
        go.transform.SetParent(canvas.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 80f);
        rect.position  = anchor.position;  // 캔버스 RectTransform 위치 그대로 사용

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize      = fontSize;
        tmp.fontStyle     = fontStyle;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        float vx = Random.Range(-xRange, xRange);
        go.GetComponent<DamageNumber>().Play(damage.ToString(), new Vector2(vx, yForce), gravity, duration, color);
    }

}
