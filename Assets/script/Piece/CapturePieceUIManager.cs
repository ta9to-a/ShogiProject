using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CapturePieceUIManager : MonoBehaviour
{
    public static CapturePieceUIManager instance { get; private set; }
    
    public List<GameObject> capturePieceParent = new ();
    public Dictionary<(PieceType, Turn), List<GameObject>> CloneGroups = new(); // 駒のクローンをグループ

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// 持ち駒管理の初期化
    /// </summary>
    public void Initialize()
    {
        CloneGroups.Clear();
        foreach (GameObject parent in capturePieceParent)
        {
            CapturePiece cp = parent.GetComponent<CapturePiece>();
            if (cp == null) continue;
            
            PieceType type = cp.capturePieceType;
            Turn turn = cp.capturePieceTurn;
            var key = (type, turn);

            if (!CloneGroups.TryGetValue(key, out List<GameObject> list))
            {
                list = new List<GameObject>();
                CloneGroups[key] = list;
            }
            list.Add(parent); // 同じ駒種を同じグループに追加
            ApplyVisualUI(cp.capturePieceType, cp.capturePieceTurn);
        }
    }

    /// <summary>
    /// ビジュアルの更新
    /// </summary>
    /// <param name="pieceType"></param>
    /// <param name="turn"></param>
    public void ApplyVisualUI(PieceType pieceType ,Turn turn)
    {
        // 駒の種類に応じてUIを更新
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            CapturePiece capturePiece = clones[0].GetComponent<CapturePiece>();
            if (capturePiece != null)
            {
                capturePiece.UpdateVisualState();
            }
        }
        else
        {
            Debug.LogWarning($"CapturePieceUIManager: 駒の種類 {pieceType} が見つかりません。");
        }
    }

    /// <summary>
    /// 持ち駒の追加時のUI処理
    /// </summary>
    /// <param name="capturedPiece"></param>
    public void AddCapturedPiece(GameObject capturedPiece)
    {
        CapturePiece cp = capturedPiece.GetComponent<CapturePiece>();
        
        PieceType pieceType = cp.capturePieceType;
        Turn turn = cp.capturePieceTurn;
        
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            GameObject capturedPieceClone = Instantiate(cp.piecePrefab, capturedPiece.transform, false);
            capturedPieceClone.name = $"{turn} : {pieceType} Clone";
            capturedPieceClone.tag = turn == Turn.先手 ? "Sente" : "Gote";
            
            float interval = 0.225f; // 駒同士の間隔
            float offsetX = (turn == Turn.先手)
                ? clones[0].transform.position.x + (clones.Count * interval)
                : clones[0].transform.position.x - (clones.Count * interval);
            
            capturedPieceClone.transform.position = new Vector3(offsetX, capturedPieceClone.transform.position.y, 0);
            
            SpriteRenderer spriteRenderer = capturedPieceClone.GetComponent<SpriteRenderer>();

            int groupCount = CloneGroups[key].Count;
            spriteRenderer.sortingOrder = 18 - groupCount; // 持ち駒の描画順序を設定
            
            float startGray = 1f; // 最初の色の明るさ
            float step = 0.07f; // 色の変化のステップ
            float colorValue = Mathf.Clamp01(startGray - step * (clones.Count));
            spriteRenderer.color = new Color(colorValue, colorValue, colorValue, 1.0f);
            
            clones.Add(capturedPieceClone);
            CapturePieceChild capturePieceChild = capturedPieceClone.GetComponent<CapturePieceChild>();
            if (capturePieceChild != null)
            {
                capturePieceChild.ApplyStateCapturePiece(pieceType, cp.umPromSprite);
                //Debug.Log(capturedPieceClone.name + "が追加されました。");
            }
        }
        else
        {
            Debug.LogWarning($"CapturePieceUIManager: 駒の種類 {pieceType} が見つかりません。");
        }
    }

    /// <summary>
    /// 持ち駒を設置した際の処理
    /// </summary>
    /// <param name="capturedPiece"></param>
    public void RemoveCapturedPiece(GameObject capturedPiece)
    {
        CapturePiece cp = capturedPiece.GetComponent<CapturePiece>();
        
        PieceType pieceType = cp.capturePieceType;
        Turn turn = cp.capturePieceTurn;
        
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            GameObject lastClone = clones.Last();
            if (lastClone != null)
            {
                Debug.Log(lastClone.name + "を削除します");
                clones.Remove(lastClone);
                Destroy(lastClone);
            }
        }
        else
        {
            Debug.LogWarning($"CapturePieceUIManager: 駒の種類 {pieceType} が見つかりません。");
        }
    }
}
