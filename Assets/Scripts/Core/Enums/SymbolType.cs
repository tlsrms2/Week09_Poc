/// <summary>
/// 마법 블록의 각 칸에 새겨지는 문양 타입.
/// Grid 배열의 원소 타입으로 사용되며, 겹치기 판별의 핵심 키가 된다.
/// </summary>
public enum SymbolType
{
    None = 0,   // 빈 칸 (블록이 차지하지 않는 영역)
    Fire,       // 불 문양
    Water,      // 물 문양
    Earth,      // 땅 문양
    Wind,       // 바람 문양
    Light,      // 빛 문양
    Dark,       // 어둠 문양
}
