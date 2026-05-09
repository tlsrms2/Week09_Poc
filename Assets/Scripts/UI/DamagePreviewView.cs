using TMPro;
using UnityEngine;

/// <summary>
/// 배치 중 예상 데미지/방어도를 실시간으로 표시한다.
/// 블록이 놓일 때마다 GridManager에서 계산값을 당겨와 갱신한다.
/// </summary>
public class DamagePreviewView : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI defenseText;

    private void OnEnable()
    {
        GameEvents.OnBlockPlaced      += HandleBlockPlaced;
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockPlaced      -= HandleBlockPlaced;
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
    }

    private void HandleBlockPlaced(CardData _, int x, int y)
    {
        var (damage, defense) = gridManager.GetPreview();

        if (damageText != null)  damageText.text  = $"공격  {damage}";
        if (defenseText != null) defenseText.text = $"방어  {defense}";
    }

    private void HandleDrawPhaseStarted(int _)
    {
        if (damageText != null)  damageText.text  = "공격  0";
        if (defenseText != null) defenseText.text = "방어  0";
    }
}
