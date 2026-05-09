using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 턴 종료 버튼. 클릭 시 OnTurnEndRequested 이벤트를 발행한다.
/// CombatManager가 Placement 상태일 때만 Resolution으로 전이한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class TurnEndButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GameEvents.RaiseTurnEndRequested);
    }
}
