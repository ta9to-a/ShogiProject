using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PromotionDatabase", menuName = "Shogi/Promotion Database")]
public class PromotionDatabase : ScriptableObject
{
    public List<PromotionData> promotionDataList;

    public PromotionData GetPromotionData(PromotionType type)
    {
        PromotionData result = promotionDataList.Find(data => data.promotionType == type);
        Debug.Log($"GetPromotionData: type={type}, result={(result != null ? result.name : "null")}");
        return result;
    }
}