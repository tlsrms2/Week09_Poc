using UnityEngine;

/// <summary>
/// 블록의 형태(Shape)와 문양 배열을 정의하는 ScriptableObject.
/// 기획자가 Inspector에서 직접 편집할 수 있는 데이터 에셋이다.
/// 
/// <para>
/// <b>사용 예시:</b>
/// 3×2 L자 블록의 경우, Width=3, Height=2로 설정하고
/// Symbols 배열을 row-major 순서(좌→우, 아래→위)로 채운다.
/// None인 칸은 블록이 차지하지 않는 빈 영역이다.
/// </para>
/// </summary>
[CreateAssetMenu(fileName = "NewBlockData", menuName = "DeckBuilder/Block Data")]
public class BlockData : ScriptableObject
{
    [Header("블록 형태")]
    [Tooltip("블록의 가로 크기 (열 수)")]
    [Min(1)]
    [SerializeField] private int width = 1;

    [Tooltip("블록의 세로 크기 (행 수)")]
    [Min(1)]
    [SerializeField] private int height = 1;

    [Header("문양 배열 (Row-Major: 좌→우, 아래→위)")]
    [Tooltip("길이 = Width × Height. None인 칸은 블록이 차지하지 않는 빈 영역")]
    [SerializeField] private SymbolType[] symbols;

    // ── Public Properties ──
    public int Width => width;
    public int Height => height;

    /// <summary>
    /// 특정 좌표(col, row)의 문양을 반환한다.
    /// 범위를 벗어나면 <see cref="SymbolType.None"/>을 반환한다.
    /// </summary>
    public SymbolType GetSymbol(int col, int row)
    {
        if (col < 0 || col >= width || row < 0 || row >= height)
            return SymbolType.None;

        int index = row * width + col;
        if (index < 0 || index >= symbols.Length)
            return SymbolType.None;

        return symbols[index];
    }

    /// <summary>
    /// 블록이 실제로 차지하는 칸(Symbol != None)의 좌표 목록을 반환한다.
    /// 배치 검증이나 겹치기 판별 시 반복 순회에 사용된다.
    /// </summary>
    public (int col, int row, SymbolType symbol)[] GetOccupiedCells()
    {
        // 일단 최대 크기로 할당 후, 실제 개수만 복사
        var buffer = new (int, int, SymbolType)[width * height];
        int count = 0;

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                SymbolType sym = GetSymbol(c, r);
                if (sym != SymbolType.None)
                {
                    buffer[count++] = (c, r, sym);
                }
            }
        }

        var result = new (int, int, SymbolType)[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;

        int requiredLength = width * height;
        if (symbols == null || symbols.Length != requiredLength)
        {
            var old = symbols ?? System.Array.Empty<SymbolType>();
            symbols = new SymbolType[requiredLength];
            System.Array.Copy(old, symbols, Mathf.Min(old.Length, requiredLength));
        }
    }
#endif
}
