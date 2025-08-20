using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PromotionDatabase", menuName = "Shogi/Promotion Database")]
public class PromotionDatabase : ScriptableObject
{
    public List<PromotionData> promotionDataList;

    public PromotionData GetPromotionData(PromotionType type)
    {
        return promotionDataList.Find(data => data.promotionType == type);
    }
}