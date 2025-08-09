using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardInitializer : MonoBehaviour
{
    [Header("プレファブ")]
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject capturePiecePrefab;
    
    [Header("親オブジェクト")]
    [SerializeField] private GameObject senteParent;
    [SerializeField] private GameObject goteParent;
    [Space(5)]
    [SerializeField] private GameObject capturePieceParent;
    [Space(5)]
    [SerializeField] private GameObject moveHighlightParent;
    
    [Header("持ち駒")]
    [SerializeField] private Vector2 senteBasePosition = new (10.75f, 3.7f); // 先手の持ち駒のベース位置
    [SerializeField] private Vector2 goteBasePosition = new (-0.75f, 6.2f); // 後手の持ち駒のベース位置
    [Space(5)]
    [Tooltip("持ち駒の横幅")]
    [SerializeField] private float capturePieceWidth = 1.5f; // 持ち駒の横幅
    [Tooltip("持ち駒の縦幅")]
    [SerializeField] private float capturePieceHeight = 1.0f; // 持ち駒の縦幅
    private Dictionary<string, List<GameObject>> _cloneGroups = new(); // 駒のクローンをグループ

    void Start()
    {
        DefaultPosition();
        CreateCapturePieces(Turn.先手);
        CreateCapturePieces(Turn.後手);
    }

    private void DefaultPosition()
    {
        CreateLinePieces(PieceType.歩兵, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 3, 7);
        CreateLinePieces(PieceType.香車, new[] { 1, 9 }, 1, 9);
        CreateLinePieces(PieceType.桂馬, new[] { 2, 8 }, 1, 9);
        CreateLinePieces(PieceType.銀将, new[] { 3, 7 }, 1, 9);
        CreateLinePieces(PieceType.金将, new[] { 4, 6 }, 1, 9);
        CreateMirroredPieces(PieceType.角行, new Vector2Int(2, 2), new Vector2Int(8, 8));
        CreateMirroredPieces(PieceType.飛車, new Vector2Int(8, 2), new Vector2Int(2, 8));
        CreateLinePieces(PieceType.玉将, new[] { 5 }, 1, 9);
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
    /// <param name="sentePos">先手の座標</param>
    /// <param name="gotePos">後手のY座標</param>
    public void CreateMirroredPieces(PieceType pieceType, Vector2Int sentePos, Vector2Int gotePos)
    {
        CreatePiece(pieceType, sentePos, Turn.先手);
        CreatePiece(pieceType, gotePos, Turn.後手);
    }

    /// <summary>
    /// 駒の生成と配置
    /// </summary>
    public void CreatePiece(PieceType pieceType, Vector2Int position, Turn turn)
    {
        PieceData data = ShogiManager.Instance.pieceDatabase.GetPieceData(pieceType);
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
        pieceScript.ApplyStatePiece(pieceType, position, unpromotedSprite, promotedSprite);
        pieceObj.GetComponent<SpriteRenderer>().sortingOrder = 10;
        
        ShogiManager.Instance.PlacePiece(position, pieceType, pieceObj.GetComponent<Piece>());
    }

    /// <summary>
    /// 持ち駒の生成と配置条件を設定
    /// </summary>
    public void CreateCapturePieces(Turn turn)
    {
        Vector2 basePos = (turn == Turn.先手) ? senteBasePosition : goteBasePosition;
        List<PieceType?[]> pieceLayout = new List<PieceType?[]>
        {
            new PieceType?[] { PieceType.香車, PieceType.桂馬 },
            new PieceType?[] { PieceType.銀将, PieceType.金将 },
            new PieceType?[] { PieceType.角行, PieceType.飛車 },
            new PieceType?[] { PieceType.歩兵, null }
        };
        for (int row = 0; row < pieceLayout.Count; row++)
        {
            for (int col = 0; col < pieceLayout[row].Length; col++)
            {
                PieceType? type = pieceLayout[row][col];
                if (!type.HasValue) continue;
                
                PieceData data = ShogiManager.Instance.pieceDatabase.GetPieceData(type.Value);
                Vector2 pos = new Vector2(
                    basePos.x + col * capturePieceWidth * (turn == Turn.先手 ? 1f : -1f),
                    basePos.y + row * capturePieceHeight * (turn == Turn.先手 ? -1f : 1f)
                );

                CreateCapturePieceObject(turn, data, pos);
            }
        }
    }

    /// <summary>
    /// 持ち駒UIの生成処理
    /// </summary>
    private void CreateCapturePieceObject(Turn turn, PieceData pieceData, Vector2 pos)
    {
        GameObject obj = Instantiate(capturePiecePrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
        obj.name = $"{turn} : {pieceData.pieceType}";
        obj.transform.SetParent(capturePieceParent.transform, false);
        
        SpriteRenderer capPieceRenderer = obj.GetComponent<SpriteRenderer>();
        capPieceRenderer.sprite = (turn == Turn.先手) ? pieceData.unpromotedSenteSprite : pieceData.unpromotedGoteSprite;
        capPieceRenderer.sortingOrder = 18;
        
        obj.layer = LayerMask.NameToLayer("CapturePiece");
        obj.tag = (turn == Turn.先手) ? "Sente" : "Gote";

        obj.GetComponent<CapturePiece>().ApplyStateCapturePiece(pieceData.pieceType, capPieceRenderer.sprite);
    }
}