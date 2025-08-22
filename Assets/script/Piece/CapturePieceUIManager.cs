using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CapturePieceUIManager : MonoBehaviour
{
    public static CapturePieceUIManager instance { get; private set; }
    
    public List<GameObject> capturePieceParent = new List<GameObject>();
    public Dictionary<PieceType, List<GameObject>> CloneGroups = new(); // 駒のクローンをグループ

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
    /// 
    /// </summary>
    public void Initialize()
    {
        CloneGroups.Clear();
        foreach (GameObject parent in capturePieceParent.ToList())
        {
            CapturePiece cp = parent.GetComponent<CapturePiece>();
            if (cp == null) continue;

            PieceType type = cp.capturePieceType;

            if (!CloneGroups.TryGetValue(type, out List<GameObject> list))
            {
                list = new List<GameObject>();
                capturePieceParent.Add(parent);
                CloneGroups[type] = list;
            }
            list.Add(parent); // 同じ駒種を同じグループに追加
        }
    }

    /// <summary>
    /// ビジュアルの更新
    /// </summary>
    /// <param name="pieceType"></param>
    public void ApplyVisualUI(PieceType pieceType)
    {
        // 駒の種類に応じてUIを更新
        if (CloneGroups.TryGetValue(pieceType, out List<GameObject> clones))
        {
            foreach (GameObject clone in clones)
            {
                CapturePiece capturePiece = clone.GetComponent<CapturePiece>();
                if (capturePiece != null)
                {
                    capturePiece.UpdateVisualState();
                }
            }
        }
        else
        {
            Debug.LogWarning($"CapturePieceUIManager: 駒の種類 {pieceType} が見つかりません。");
        }
    }
}
