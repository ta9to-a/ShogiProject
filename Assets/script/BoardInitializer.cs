using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardInitializer : MonoBehaviour
{
    [Header("データベース")]
    [SerializeField] private PieceDatabase pieceDatabase;
    
    // 各種駒の動作が格納されたプレファブ
    [Header("Piece")]
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject heldPiecePrefab;
    
    [Header("親オブジェクト")]
    [SerializeField] private GameObject senteParent;
    [SerializeField] private GameObject goteParent;

    void Start()
    {
        DefaultPosition();
    }

    private void DefaultPosition()
    {
        CreateLinePieces(PieceType.歩兵, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 3, 7);
        CreateLinePieces(PieceType.桂馬, new[] { 2, 8 }, 1, 9);
        CreateLinePieces(PieceType.銀将, new[] { 3, 7 }, 1, 9);
        CreateLinePieces(PieceType.金将, new[] { 4, 6 }, 1, 9);
        CreateLinePieces(PieceType.香車, new[] { 1, 9 }, 1, 9);
        CreateLinePieces(PieceType.玉将, new[] { 5 }, 1, 9);
        CreateMirroredPieces(PieceType.角行, 2, 2, 8, 8);
        CreateMirroredPieces(PieceType.飛車, 8, 2, 2, 8);
    }

    /// <summary>
    /// 駒を横一列に配置
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="posX">X座標の配列</param>
    /// <param name="sentePosY">先手のY座標</param>
    /// <param name="gotePosY">後手のY座標</param>
    public void CreateLinePieces(PieceType pieceType,
        int[] posX, int sentePosY, int gotePosY)
    {
        foreach (int x in posX)
        {
            CreatePiece(pieceType, new Vector2Int(x, sentePosY), Turn.先手);
            CreatePiece(pieceType, new Vector2Int(x, gotePosY), Turn.後手);
        }
    }

    /// <summary>
    /// 左右対称に駒を配置
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="senteX">先手のX座標</param>
    /// <param name="senteY">先手のY座標</param>
    /// <param name="goteX">後手のX座標</param>
    /// <param name="goteY">後手のY座標</param>
    public void CreateMirroredPieces(PieceType pieceType, int senteX, int senteY, int goteX, int goteY)
    {
        CreatePiece(pieceType, new Vector2Int(senteX, senteY), Turn.先手);
        CreatePiece(pieceType, new Vector2Int(goteX, goteY), Turn.後手);
    }

    /// <summary>
    /// 単一の駒の配置
    /// </summary>
    /// <param name="pieceType"></param>
    /// <param name="posX"></param>
    /// <param name="posY">駒のX座標</param>
    /// <param name="turn">駒のターン</param>
    public void CreateSinglePiece(PieceType pieceType,
        int posX, int posY, Turn turn)
    {
        CreatePiece(pieceType, new Vector2Int(posX, posY), turn);
    }

    /// <summary>
    /// 駒の生成と配置
    /// </summary>
    private void CreatePiece(PieceType pieceType, Vector2Int position, Turn turn)
    {
        PieceData data = pieceDatabase.GetPieceData(pieceType);
        if (data == null)
        {
            Debug.LogError($"PieceDataが見つかりませんでした : {pieceType}");
            return;
        }
        GameObject pieceObj = Instantiate(piecePrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
        pieceObj.name = $"{turn} : {pieceType}";
        
        Piece pieceScript = pieceObj.GetComponent<Piece>();
        pieceObj.layer = LayerMask.NameToLayer("Piece");

        Sprite unpromotedSprite;
        Sprite promotedSprite;
        GameObject parentObject;
        
        if (turn == Turn.先手)
        {
            pieceObj.tag = "Sente";
            unpromotedSprite = data.unpromotedSenteSprite;
            promotedSprite = data.promotedSenteSprite;
            parentObject = senteParent;
        }
        else
        {
            pieceObj.tag = "Gote";
            unpromotedSprite = data.unpromotedGoteSprite;
            promotedSprite = data.promotedGoteSprite;
            parentObject = goteParent;
        }
        pieceObj.transform.SetParent(parentObject.transform, false);
        pieceScript.ApplyStatePiece(pieceType, unpromotedSprite, promotedSprite);
    }
}