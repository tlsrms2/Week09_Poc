using UnityEngine;

/// <summary>
/// 전투 씬의 진입점(Entry Point).
/// 씬 로드 시 자동으로 전투를 시작하는 부트스트랩 컴포넌트.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CombatBootstrap : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;

    private void Start()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }

        if (combatManager != null)
        {
            combatManager.StartCombat();
        }
        else
        {
            Debug.LogError("[CombatBootstrap] CombatManager를 찾을 수 없습니다!");
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시 좀비 리스너 방지
        GameEvents.ClearAll();
    }
}
