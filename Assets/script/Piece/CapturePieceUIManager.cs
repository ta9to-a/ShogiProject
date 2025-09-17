using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CapturePieceUIManager : MonoBehaviour
{
    public static CapturePieceUIManager Instance { get; private set; }
    
    public List<GameObject> capturePieceParent = new ();
    public Dictionary<(PieceType, Turn), List<GameObject>> CloneGroups = new(); // 駒のクローンをグループ

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 持ち駒管理の初期化
    /// </summary>
    public void Initialize()
    {
        CloneGroups.Clear();
        foreach (GameObject parent in capturePieceParent)
        {
            CapturePieceParent cp = parent.GetComponent<CapturePieceParent>();
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
    public void ApplyVisualUI(PieceType pieceType ,Turn turn)
    {
        // 駒の種類に応じてUIを更新
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            CapturePieceParent capturePieceParent = clones[0].GetComponent<CapturePieceParent>();
            if (capturePieceParent != null)
            {
                capturePieceParent.UpdateVisualState();
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
    public void AddCapturedPiece(GameObject capturedPiece)
    {
        CapturePieceParent cp = capturedPiece.GetComponent<CapturePieceParent>();
        
        PieceType pieceType = cp.capturePieceType;
        Turn turn = cp.capturePieceTurn;
        
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            GameObject capturedPieceClone = Instantiate(cp.capturePieceChildPrefab, capturedPiece.transform, false);
            capturedPieceClone.name = $"{turn} : {pieceType} Clone";
            capturedPieceClone.tag = turn == Turn.先手 ? "Sente" : "Gote";
            
            float interval = 0.225f; // 駒同士の間隔
            float offsetX = (turn == Turn.先手)
                ? clones[0].transform.position.x - (clones.Count * interval)
                : clones[0].transform.position.x + (clones.Count * interval);
            
            capturedPieceClone.transform.position = new Vector3(offsetX, capturedPieceClone.transform.position.y, 0);
            
            SpriteRenderer spriteRenderer = capturedPieceClone.GetComponent<SpriteRenderer>();

            int groupCount = CloneGroups[key].Count;
            spriteRenderer.sortingOrder = 18 - groupCount; // 持ち駒の描画順序を設定
            
            float startGray = 1f;   // 最初の色の明るさ
            float step = 0.07f;     // 色の変化の段階
            float colorValue = Mathf.Clamp01(startGray - step * (clones.Count));
            spriteRenderer.color = new Color(colorValue, colorValue, colorValue, 1.0f);
            
            clones.Add(capturedPieceClone);
            CapturePieceChild capturePieceChild = capturedPieceClone.GetComponent<CapturePieceChild>();
            if (capturePieceChild != null)
            {
                capturePieceChild.ApplyStateCapturePiece(pieceType, cp.umPromSprite);
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
    public void RemoveCapturedPiece(GameObject capturedPiece)
    {
        CapturePieceParent cp = capturedPiece.GetComponent<CapturePieceParent>();
        
        PieceType pieceType = cp.capturePieceType;
        Turn turn = cp.capturePieceTurn;
        
        (PieceType, Turn) key = (pieceType, turn);
        if (CloneGroups.TryGetValue(key, out List<GameObject> clones) && clones.Count > 0)
        {
            GameObject lastClone = clones.Last();
            if (lastClone != null)
            {
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
