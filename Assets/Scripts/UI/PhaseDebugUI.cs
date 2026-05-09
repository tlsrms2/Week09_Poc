using UnityEngine;

/// <summary>
/// F1 키를 누르면 현재 전투 페이즈(상태)를 화면 좌측 상단에 띄워주는 디버그용 스크립트.
/// 아무 빈 게임오브젝트에나 붙여두면 동작합니다.
/// </summary>
public class PhaseDebugUI : MonoBehaviour
{
    private bool showDebug = true; // 기본적으로 보이게 설정
    private CombatManager combatManager;

    private void Start()
    {
        combatManager = FindAnyObjectByType<CombatManager>();
    }

    private void Update()
    {
        // F1 키로 디버그 UI 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebug = !showDebug;
        }
    }

    private void OnGUI()
    {
        if (!showDebug || combatManager == null) return;

        // UI 스타일 설정
        GUIStyle style = new GUIStyle();
        style.fontSize = 32;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        // 배경용 박스
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(10, 10, 500, 60), Texture2D.whiteTexture);

        // 텍스트 출력
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 20, 480, 50), $"[F1] Current Phase: {combatManager.CurrentState}", style);
    }
}
