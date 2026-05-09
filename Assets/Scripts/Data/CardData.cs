using UnityEngine;

/// <summary>
/// 카드(스킬) 한 장의 정적 데이터를 정의하는 ScriptableObject.
/// 블록 형태 데이터(<see cref="BlockData"/>)와 전투 속성(위력, 타입)을 조합한다.
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "DeckBuilder/Card Data")]
public class CardData : ScriptableObject
{
    [Header("카드 기본 정보")]
    [Tooltip("카드 이름 (UI 표시용)")]
    [SerializeField] private string cardName = "New Card";

    [Tooltip("카드 설명 (UI 표시용)")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Header("블록 형태")]
    [Tooltip("이 카드에 연결된 블록 형태 데이터")]
    [SerializeField] private BlockData blockData;

    [Header("전투 속성")]
    [Tooltip("카드 효과 유형 (공격 / 방어)")]
    [SerializeField] private CardType cardType = CardType.Attack;

    [Tooltip("기본 위력 (증폭 전 원본 값)")]
    [Min(0)]
    [SerializeField] private int basePower = 5;

    // ── Public Properties ──
    public string CardName => cardName;
    public string Description => description;
    public BlockData BlockData => blockData;
    public CardType Type => cardType;
    public int BasePower => basePower;
}
