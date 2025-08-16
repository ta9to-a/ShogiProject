using UnityEngine;

[CreateAssetMenu(fileName = "PromotionData", menuName = "Shogi/Promotion Data")]
public class PromotionData : ScriptableObject
{
    public PromotionType promotionType;
    public bool moveUpdate;
    public Vector2Int[] moveRange;
}