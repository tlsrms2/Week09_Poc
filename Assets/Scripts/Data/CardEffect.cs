using System;

/// <summary>
/// 카드 효과 한 항목. CardData의 effects 리스트 원소로 사용된다.
/// </summary>
[Serializable]
public struct CardEffect
{
    public CardType effectType;
    public int power;
}
