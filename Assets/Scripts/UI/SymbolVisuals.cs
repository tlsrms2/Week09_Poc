using UnityEngine;

/// <summary>
/// SymbolType별 색상 정의. 고스트 타일과 배치된 셀 모두 이 색을 기준으로 렌더링한다.
/// </summary>
public static class SymbolVisuals
{
    public static Color GetColor(SymbolType symbol) => symbol switch
    {
        SymbolType.Fire  => new Color(0.95f, 0.35f, 0.10f, 1f), // 주황-빨강
        SymbolType.Water => new Color(0.15f, 0.50f, 0.95f, 1f), // 파랑
        SymbolType.Earth => new Color(0.40f, 0.65f, 0.20f, 1f), // 초록-갈색
        SymbolType.Wind  => new Color(0.55f, 0.90f, 0.85f, 1f), // 청록
        SymbolType.Light => new Color(0.95f, 0.90f, 0.30f, 1f), // 노랑
        SymbolType.Dark  => new Color(0.50f, 0.15f, 0.75f, 1f), // 보라
        _                => new Color(0.20f, 0.20f, 0.20f, 1f), // None: 짙은 회색
    };
}
