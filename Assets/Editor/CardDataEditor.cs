using UnityEngine;
using UnityEditor;

/// <summary>
/// CardData ScriptableObject의 커스텀 인스펙터.
/// 카드 기본 정보 설정과 그리드 상의 문양 페인팅을 하나의 인스펙터에서 수행합니다.
/// </summary>
[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    // ─── 문양별 색상 팔레트 ───
    private static readonly Color ColorNone  = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color ColorFire  = new Color(1.00f, 0.35f, 0.10f);
    private static readonly Color ColorWater = new Color(0.15f, 0.55f, 1.00f);
    private static readonly Color ColorEarth = new Color(0.55f, 0.35f, 0.10f);
    private static readonly Color ColorWind  = new Color(0.40f, 0.85f, 0.45f);
    private static readonly Color ColorLight = new Color(1.00f, 0.95f, 0.35f);
    private static readonly Color ColorDark  = new Color(0.50f, 0.20f, 0.75f);

    // ─── 문양 이모지 라벨 ───
    private static readonly string[] SymbolLabels =
    {
        "✕",   // None
        "🔥",  // Fire
        "💧",  // Water
        "🪨",  // Earth
        "💨",  // Wind
        "✨",  // Light
        "🌑",  // Dark
    };

    // 셀 하나의 픽셀 크기
    private const float CellSize = 48f;
    private const float CellPadding = 3f;

    // 현재 좌클릭으로 페인팅할 문양 (팔레트에서 선택)
    private SymbolType selectedSymbol = SymbolType.Fire;

    // 사이즈 변경 임시값
    private int pendingWidth;
    private int pendingHeight;
    private bool sizeInitialized = false;

    // SerializedProperty 캐시 (Block 부분)
    private SerializedProperty propWidth;
    private SerializedProperty propHeight;
    private SerializedProperty propSymbols;

    // SerializedProperty 캐시 (Card 부분)
    private SerializedProperty propCardName;
    private SerializedProperty propDescription;
    private SerializedProperty propCardType;
    private SerializedProperty propBasePower;

    private void OnEnable()
    {
        // Block
        propWidth   = serializedObject.FindProperty("width");
        propHeight  = serializedObject.FindProperty("height");
        propSymbols = serializedObject.FindProperty("symbols");

        // Card
        propCardName    = serializedObject.FindProperty("cardName");
        propDescription = serializedObject.FindProperty("description");
        propCardType    = serializedObject.FindProperty("cardType");
        propBasePower   = serializedObject.FindProperty("basePower");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CardData data = (CardData)target;

        // 처음 열 때 임시값 초기화
        if (!sizeInitialized)
        {
            pendingWidth  = propWidth.intValue;
            pendingHeight = propHeight.intValue;
            sizeInitialized = true;
        }

        // ─── 섹션 0: 카드 기본 속성 ───
        DrawSectionHeader("📝 카드 정보");
        EditorGUILayout.PropertyField(propCardName);
        EditorGUILayout.PropertyField(propDescription);
        
        EditorGUILayout.Space(4);
        DrawSectionHeader("⚔️ 전투 속성");
        EditorGUILayout.PropertyField(propCardType);
        EditorGUILayout.PropertyField(propBasePower);

        EditorGUILayout.Space(12);

        // ─── 섹션 1: 크기 설정 ───
        DrawSectionHeader("📐 블록 크기");
        EditorGUILayout.BeginHorizontal();
        pendingWidth  = EditorGUILayout.IntSlider("가로 (Width)",  pendingWidth,  1, 8);
        pendingHeight = EditorGUILayout.IntSlider("세로 (Height)", pendingHeight, 1, 8);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("크기 적용 (기존 데이터 유지)", GUILayout.Height(28)))
        {
            ApplySize(data, pendingWidth, pendingHeight);
        }

        EditorGUILayout.Space(6);

        // ─── 섹션 2: 문양 팔레트 ───
        DrawSectionHeader("🎨 페인트 문양 선택");
        DrawSymbolPalette();

        EditorGUILayout.Space(6);

        // ─── 섹션 3: 그리드 편집기 ───
        DrawSectionHeader("🗺️ 블록 형태 편집 (클릭하여 페인팅)");
        DrawGrid(data);

        EditorGUILayout.Space(6);

        // ─── 섹션 4: 범례 ───
        DrawLegend();

        serializedObject.ApplyModifiedProperties();
    }

    // ═══════════════════════════════════════════
    //  그리드 렌더링
    // ═══════════════════════════════════════════

    private void DrawGrid(CardData data)
    {
        int w = propWidth.intValue;
        int h = propHeight.intValue;

        // 그리드 전체 가로 크기 계산 (중앙 정렬)
        float totalWidth  = w * (CellSize + CellPadding) - CellPadding;
        float totalHeight = h * (CellSize + CellPadding) - CellPadding;

        // 레이아웃 여백 확보
        Rect gridArea = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        float offsetX = gridArea.x + Mathf.Max(0, (gridArea.width - totalWidth) * 0.5f);

        // row 0 = 아래쪽(row-major 정의), 화면에는 위에서 아래로 그려야 하므로 row를 역순으로 표시
        for (int row = h - 1; row >= 0; row--)
        {
            for (int col = 0; col < w; col++)
            {
                int visRow = (h - 1 - row); // 화면 상 y 인덱스
                float x = offsetX + col * (CellSize + CellPadding);
                float y = gridArea.y + visRow * (CellSize + CellPadding);

                Rect cellRect = new Rect(x, y, CellSize, CellSize);
                DrawCell(data, col, row, cellRect);
            }
        }

        // Repaint 요청 (마우스 호버 등 즉각 반영)
        if (Event.current.type == EventType.MouseMove ||
            Event.current.type == EventType.MouseDrag)
        {
            Repaint();
        }
    }

    private void DrawCell(CardData data, int col, int row, Rect rect)
    {
        int w = propWidth.intValue;
        int index = row * w + col;

        if (propSymbols.arraySize <= index) return;

        SymbolType currentSym = (SymbolType)propSymbols.GetArrayElementAtIndex(index).intValue;

        // 배경색
        Color cellColor = GetSymbolColor(currentSym);

        // 호버 강조
        bool isHovered = rect.Contains(Event.current.mousePosition);
        if (isHovered)
            cellColor = Color.Lerp(cellColor, Color.white, 0.25f);

        // 셀 배경 그리기
        EditorGUI.DrawRect(rect, cellColor);

        // 테두리
        Color borderColor = isHovered ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        DrawBorder(rect, borderColor, 1.5f);

        // 문양 라벨
        string label = SymbolLabels[(int)currentSym];
        GUI.Label(rect, label, GetCellLabelStyle());

        // 클릭/드래그 처리
        if (isHovered &&
            (Event.current.type == EventType.MouseDown ||
             Event.current.type == EventType.MouseDrag) &&
            Event.current.button == 0)
        {
            Undo.RecordObject(target, "Paint Block Cell");
            propSymbols.GetArrayElementAtIndex(index).intValue = (int)selectedSymbol;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Event.current.Use();
            Repaint();
        }

        // 우클릭 → None으로 지우기
        if (isHovered &&
            (Event.current.type == EventType.MouseDown ||
             Event.current.type == EventType.MouseDrag) &&
            Event.current.button == 1)
        {
            Undo.RecordObject(target, "Erase Block Cell");
            propSymbols.GetArrayElementAtIndex(index).intValue = (int)SymbolType.None;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Event.current.Use();
            Repaint();
        }
    }

    // ═══════════════════════════════════════════
    //  팔레트
    // ═══════════════════════════════════════════

    private void DrawSymbolPalette()
    {
        var symbols = (SymbolType[])System.Enum.GetValues(typeof(SymbolType));

        EditorGUILayout.BeginHorizontal();
        foreach (SymbolType sym in symbols)
        {
            bool isSelected = (selectedSymbol == sym);

            // 선택된 심볼은 테두리 강조
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize        = 18,
                fixedWidth      = 44,
                fixedHeight     = 44,
                normal          = { background = MakeTex(2, 2, GetSymbolColor(sym)) },
                hover           = { background = MakeTex(2, 2, Color.Lerp(GetSymbolColor(sym), Color.white, 0.25f)) },
                fontStyle       = isSelected ? FontStyle.Bold : FontStyle.Normal,
            };

            // 선택 여부 표시용 추가 테두리
            if (isSelected)
            {
                Rect btnAreaRect = GUILayoutUtility.GetRect(46, 46);
                EditorGUI.DrawRect(btnAreaRect, Color.white);
                Rect innerRect = new Rect(btnAreaRect.x + 2, btnAreaRect.y + 2,
                                          btnAreaRect.width - 4, btnAreaRect.height - 4);
                if (GUI.Button(innerRect, SymbolLabels[(int)sym], style))
                    selectedSymbol = sym;
            }
            else
            {
                if (GUILayout.Button(SymbolLabels[(int)sym], style))
                    selectedSymbol = sym;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"현재 선택: {SymbolLabels[(int)selectedSymbol]}  {selectedSymbol}",
            EditorStyles.miniLabel);
    }

    // ═══════════════════════════════════════════
    //  범례
    // ═══════════════════════════════════════════

    private void DrawLegend()
    {
        EditorGUILayout.LabelField("📌 조작법", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "• 좌클릭 / 드래그: 선택한 문양으로 페인팅\n" +
            "• 우클릭 / 드래그: 해당 칸을 None(빈 칸)으로 지우기\n" +
            "• 크기 변경 후 [크기 적용] 버튼을 눌러야 배열이 재조정됩니다.",
            MessageType.Info);
    }

    // ═══════════════════════════════════════════
    //  크기 적용 (기존 데이터 유지)
    // ═══════════════════════════════════════════

    private void ApplySize(CardData data, int newW, int newH)
    {
        Undo.RecordObject(target, "Resize Block");

        int oldW = propWidth.intValue;
        int oldH = propHeight.intValue;
        int oldSize = propSymbols.arraySize;

        // 기존 데이터 복사
        int[] oldSyms = new int[oldSize];
        for (int i = 0; i < oldSize; i++)
            oldSyms[i] = propSymbols.GetArrayElementAtIndex(i).intValue;

        // 새 크기 적용
        propWidth.intValue  = newW;
        propHeight.intValue = newH;
        propSymbols.arraySize = newW * newH;

        // 이전 데이터를 좌표 기반으로 복원 (범위 내 셀만)
        for (int row = 0; row < newH; row++)
        {
            for (int col = 0; col < newW; col++)
            {
                int newIdx = row * newW + col;
                if (col < oldW && row < oldH)
                {
                    int oldIdx = row * oldW + col;
                    propSymbols.GetArrayElementAtIndex(newIdx).intValue =
                        oldIdx < oldSize ? oldSyms[oldIdx] : 0;
                }
                else
                {
                    propSymbols.GetArrayElementAtIndex(newIdx).intValue = 0;
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        Debug.Log($"[CardDataEditor] 크기 변경: {oldW}×{oldH} → {newW}×{newH}");
    }

    // ═══════════════════════════════════════════
    //  유틸리티
    // ═══════════════════════════════════════════

    private static void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(4);
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        EditorGUILayout.LabelField(title, style);
        Rect lineRect = GUILayoutUtility.GetRect(1, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(4);
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static GUIStyle GetCellLabelStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 22,
        };
    }

    private static Color GetSymbolColor(SymbolType sym)
    {
        return sym switch
        {
            SymbolType.None  => ColorNone,
            SymbolType.Fire  => ColorFire,
            SymbolType.Water => ColorWater,
            SymbolType.Earth => ColorEarth,
            SymbolType.Wind  => ColorWind,
            SymbolType.Light => ColorLight,
            SymbolType.Dark  => ColorDark,
            _                => ColorNone,
        };
    }

    // Texture2D를 동적 생성 (GUIStyle 배경용)
    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
