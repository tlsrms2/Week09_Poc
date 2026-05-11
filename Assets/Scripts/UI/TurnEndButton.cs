using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 턴 종료 버튼. Placement 상태일 때만 활성화되며 클릭 시 OnTurnEndRequested 이벤트를 발행한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class TurnEndButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(GameEvents.RaiseTurnEndRequested);
        button.interactable = false;
    }

    private void OnEnable()
    {
        GameEvents.OnCombatStateChanged += HandleCombatStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnCombatStateChanged -= HandleCombatStateChanged;
    }

    private void HandleCombatStateChanged(CombatState state)
    {
        button.interactable = state == CombatState.Placement;
    }
}
