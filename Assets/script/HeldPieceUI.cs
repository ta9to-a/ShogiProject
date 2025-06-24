using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeldPieceUI : MonoBehaviour
{
    [SerializeField] GameObject heldPiecePrefab;
    [SerializeField] ShogiManager shogiManager;

    private Dictionary<string, List<GameObject>> _cloneGroups = new();
    void Start()
    {
        CreateHeldPieces(true);   // 先手
        CreateHeldPieces(false);  // 後手
    }

    public void CreateHeldPieces(bool isSente)
    {
        int cols = 2;  // 横に2個ずつ
        float pieceWidth = isSente ? -1.5f : 1.5f; // 先手は右向き、後手は左向き
        float pieceHeight = 1f;

        // ベースポジション（先手 or 後手）
        Vector3 basePosition = isSente 
            ? new Vector3(-0.75f, 6.2f, 0f)          // 先手：左上
            : new Vector3(10.75f, 3.7f, 0f);         // 後手：右下

        int placedIndex = 0;
        for (int i = 0; i < shogiManager.defaultSprites.Length - 1; i++)
        {
            Vector3 position;

            if (i == 0)
            {
                // 歩だけは特別な位置に
                position = basePosition + (isSente
                    ? new Vector3(0f, 3f * pieceHeight, 0f) // 先手：下に配置
                    : new Vector3(0f, -3f * pieceHeight, 0f)); // 後手：上に配置
            }
            else
            {
                int row = placedIndex / cols;  // 先に行（縦）を数える
                int col = placedIndex % cols;  // 横方向にずらす

                position = basePosition + (isSente
                    ? new Vector3(col * pieceWidth, row * pieceHeight, 0f) // 先手：下方向
                    : new Vector3(col * pieceWidth, -row * pieceHeight, 0f)); // 後手：上方向

                placedIndex++;
            }

            GameObject heldPieceObj = Instantiate(heldPiecePrefab, position, Quaternion.identity);
            heldPieceObj.transform.SetParent(this.transform, false); // 子オブジェクトに設定
            // スプライト設定
            heldPieceObj.GetComponent<SpriteRenderer>().sprite = shogiManager.defaultSprites[i];

            // 持ち駒データの設定
            HeldPieceData data = heldPieceObj.AddComponent<HeldPieceData>();
            data.pieceType = (Piece.PieceId)i;
            data.isSente = isSente;
            data.isHeldPieceCount = true;

            // 後手は180度回転
            if (isSente) heldPieceObj.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            
            SpriteRenderer originalRenderer = heldPieceObj.GetComponent<SpriteRenderer>();
            originalRenderer.sortingOrder = 18;
            
            string isSenteString = isSente ? "先手" : "後手";
            heldPieceObj.name = $"{isSenteString}:{data.pieceType},1";
        }
    }

    public void ManageClones(Piece.PieceId pieceType, bool isSente, Vector3 basePosition, int count, Transform parentTransform)
    {
        string cloneKey = $"{pieceType}_{isSente}"; // クローングループの識別キー
    
        // 既存クローンの削除
        if (_cloneGroups.ContainsKey(cloneKey))
        {
            foreach (GameObject clone in _cloneGroups[cloneKey])
            {
                if (clone != null) Destroy(clone);
            }
            _cloneGroups[cloneKey].Clear();
        }
        else
        {
            _cloneGroups[cloneKey] = new List<GameObject>();
        }
    
        // 新しいクローンの生成（count-1個、元の駒は除く）
        for (int i = 1; i < count; i++)
        {
            int isSenteDirection = isSente ? -1 : 1; // 先手なら1、後手なら-1
            Vector3 clonePos = basePosition + new Vector3(0.25f * i * isSenteDirection, 0, 0);
            GameObject clone = Instantiate(heldPiecePrefab, clonePos, Quaternion.identity);
    
            clone.transform.SetParent(parentTransform, true);
            clone.transform.localScale = Vector3.one;
    
            SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
            cloneRenderer.sprite = shogiManager.defaultSprites[(int)pieceType];
    
            // 左側の駒ほど高いsortingOrderを設定（左側が上に表示）
            cloneRenderer.sortingOrder = count - i;
            float colorValue = 0.7f + 0.05f * (count - i); // 色の明るさを調整
            cloneRenderer.color = new Color(colorValue, colorValue, colorValue, 1.0f);
            
            HeldPieceData data = clone.AddComponent<HeldPieceData>();
            data.pieceType = pieceType;
            data.isSente = isSente;
            data.isHeldPieceCount = false;
    
            if (isSente) clone.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            
            string isSenteString = isSente ? "先手" : "後手";
            clone.name = $"{isSenteString}:{pieceType},{count}";
            
            _cloneGroups[cloneKey].Add(clone);
        }
    }
}