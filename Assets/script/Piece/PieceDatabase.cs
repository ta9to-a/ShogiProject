using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

[CreateAssetMenu(fileName = "PieceDatabase", menuName = "Shogi/PieceDatabase")]
public class PieceDatabase : ScriptableObject
{
    [SerializeField] public List<PieceData> pieceDataList;
    [SerializeField] public List<PieceData> promotionPieceDataList;
    private Dictionary<PieceType, PieceData> _pieceDataDict;
    private Dictionary<PieceData.PromotionType, PieceData> _promotionPieceDataDict;

    private void OnEnable()
    {
        _pieceDataDict = pieceDataList.ToDictionary(data => data.pieceType);
        if (promotionPieceDataList != null)
        {
            _promotionPieceDataDict = promotionPieceDataList.ToDictionary(data => data.promotionType);
        }
    }
    
    /// <summary>
    /// 駒の種類から関連するデータを取得する
    /// </summary>
    public PieceData GetPieceData(PieceType pieceType)
    {
        if (_pieceDataDict == null)
        {
            OnEnable();
            Debug.Assert(_pieceDataDict != null,
                "PieceDatabaseが初期化されていません。OnEnableメソッドを確認してください。");
        }
        
        _pieceDataDict.TryGetValue(pieceType, out PieceData data);
        return data;
    }
    
    /// <summary>
    /// 成駒タイプから関連するデータを取得する
    /// </summary>
    public PieceData GetPromotionPieceData(PieceData.PromotionType promotionType)
    {
        if (_promotionPieceDataDict == null)
        {
            OnEnable();
        }
        
        if (_promotionPieceDataDict != null && _promotionPieceDataDict.TryGetValue(promotionType, out PieceData data))
        {
            return data;
        }
        
        return null;
    }
}