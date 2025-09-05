using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

[CreateAssetMenu(fileName = "PieceDatabase", menuName = "Shogi/PieceDatabase")]
public class PieceDatabase : ScriptableObject
{
    [SerializeField] public List<PieceData> pieceDataList;
    private Dictionary<PieceType, PieceData> _pieceDataDict;

    private void OnEnable()
    {
        _pieceDataDict = pieceDataList.ToDictionary(data => data.pieceType);
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
}