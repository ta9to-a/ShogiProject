using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class HeldPieceManager : MonoBehaviour
{
    //【持ち駒管理】
    public int[] senteHeldPieceType = new int[7]; // 先手の持ち駒の種類ごとの数
    public int[] goteHeldPieceType = new int[7]; // 後手の持ち駒の種類ごとの数

    public List<GameObject> senteInactivePieces = new (); // 先手の持ち駒オブジェクトリスト
    public List<GameObject> goteInactivePieces = new(); // 後手の持ち駒オブジェクトリスト
    
    public static bool IsHeldPieceSelected; // 持ち駒が選択されているかどうか
    public static GameObject FoundPiece; // 持ち駒の中から見つかった駒
    
    public System.Action OnHeldPieceChanged;
    
    ShogiManager _shogiManager;
    
    public static Piece.PieceId SelectedPieceType;

    void Start()
    {
        _shogiManager = FindObjectOfType<ShogiManager>();
        FindObjectOfType<HeldPieceManager>();
    }
    
    public void AddHeldPiece(GameObject enemyPiece, Piece.PieceId pieceType, bool tagIsSente)
    {
        // 駒の種類に応じて持ち駒の配列を更新
        int pieceTypeIndex = (int)pieceType;
        
        // 駒のタグを変更
        enemyPiece.tag = tagIsSente ? "Sente" : "Gote";
        
        // 駒の種類を設定
        Piece pieceScript = enemyPiece.GetComponent<Piece>();
        pieceScript.ApplyStatePiece(pieceType);
        
        enemyPiece.SetActive(false);
        Debug.Log($"駒を入手: {enemyPiece.tag} ({pieceType})");
        
        // 先手か後手かを判定
        bool capturerIsSente = enemyPiece.CompareTag("Sente");
        if (capturerIsSente) senteInactivePieces.Add(enemyPiece);   // 先手の持ち駒リストに追加
        else goteInactivePieces.Add(enemyPiece);                    // 後手の持ち駒リストに追加
        
        // その駒の種類の持ち駒数を増やす
        if (capturerIsSente) senteHeldPieceType[pieceTypeIndex]++;
        else goteHeldPieceType[pieceTypeIndex]++;
        
        OnHeldPieceChanged?.Invoke();
    }

    public void RemoveHeldPiece(Piece.PieceId pieceType, bool tagIsSente)
    {
        // 先手・後手の持ち駒リストを選ぶ
        List<GameObject> targetList = tagIsSente ? senteInactivePieces : goteInactivePieces;
        // 持ち駒リストから指定された駒の種類を探す
        FoundPiece = targetList.Find(
            target => target.GetComponent<Piece>()?.pieceType == pieceType
        );

        if (FoundPiece != null)
        {
            Debug.Log($"持ち駒の {FoundPiece} を選択しました！");
            IsHeldPieceSelected = true; // 持ち駒が選択された状態にする
            _shogiManager.CreateDropHighlightSquares(pieceType);
        }
        else
        {
            Debug.Log($"持ち駒に {pieceType} はありません。");
        }
    }

    public void SelectedHeldPiece(GameObject foundPiece, Piece.PieceId pieceType)
    {
        Vector2 mouseMinPos = new (0.5f, 0.5f); // マウス選択の座標の下限値
        Vector2 mouseMaxPos = new (9.5f, 9.5f); // マウス選択の座標の上限値
        
        Vector2 mousePosition = Input.mousePosition;
        if (Camera.main != null)
        {
            // マウス座標をワールド座標に変換
            Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector2 intMousePos = new Vector2(Mathf.Round(worldMousePos.x), Mathf.Round(worldMousePos.y));
            
            // マウスの座標が範囲内にあるかをチェック
            if (intMousePos.x <= mouseMinPos.x || intMousePos.y <= mouseMinPos.y ||
                intMousePos.x >= mouseMaxPos.x || intMousePos.y >= mouseMaxPos.y)
            {
                Debug.Log("マウスの位置が盤面外です。");
                _shogiManager.ClearHeldPieceSelection();
                return;
            }
            
            // 駒がすでにある場合は設置できなくする
            LayerMask pieceLayer = LayerMask.GetMask("Piece");
            Collider2D collidedPiece = Physics2D.OverlapPoint(worldMousePos, pieceLayer);
            if (collidedPiece != null)
            {
                Debug.Log("すでに駒があるため、設置できません。");
                return;
            }

            if (foundPiece.GetComponent<Piece>().pieceType == Piece.PieceId.Hu)
            { 
                // 二歩チェック;
                bool isSente = foundPiece.CompareTag("Sente");
                bool[] targetFuPositions = isSente ? _shogiManager.senteFuPosition : _shogiManager.goteFuPosition;

                if (targetFuPositions[(int)intMousePos.x - 1])
                {
                    Debug.Log("二歩のため配置できません。");
                    _shogiManager.ClearHeldPieceSelection();
                    return;
                }
                
                // 配置成功時に二歩チェック配列を更新
                targetFuPositions[(int)intMousePos.x - 1] = true;
            }

            switch (pieceType)
            {
                case Piece.PieceId.Hu:
                case Piece.PieceId.Kyosha:
                    if (intMousePos.y >= 9 && foundPiece.CompareTag("Sente"))
                    {
                        Debug.Log($"{pieceType}は9段目に配置できません。");
                        _shogiManager.ClearHeldPieceSelection();
                        return;
                    }
                    else if (intMousePos.y <= 1 && foundPiece.CompareTag("Gote"))
                    {
                        Debug.Log($"{pieceType}は1段目に配置できません。");
                        _shogiManager.ClearHeldPieceSelection();
                        return;
                    }
                    break;
                case Piece.PieceId.Keima:
                    if (intMousePos.x >= 8 && foundPiece.CompareTag("Sente"))
                    {
                        Debug.Log("桂馬は8列目に配置できません。");
                        _shogiManager.ClearHeldPieceSelection();
                        return;
                    }
                    else if (intMousePos.x <= 2 && foundPiece.CompareTag("Gote"))
                    {
                        Debug.Log("桂馬は2列目に配置できません。");
                        _shogiManager.ClearHeldPieceSelection();
                        return;
                    }
                    break;
            }

            // 駒をマウスの位置に移動
            foundPiece.transform.position = intMousePos; // マウスの位置に駒を移動
            foundPiece.SetActive(true);
            
            // 持ち駒リストから削除
            bool capturerIsSente = foundPiece.CompareTag("Sente");
            if (capturerIsSente) senteInactivePieces.Remove(foundPiece);   // 先手の持ち駒リストに追加
            else goteInactivePieces.Remove(foundPiece); 
            
            // その駒の種類の持ち駒数を減らす
            int pieceTypeIndex = (int)foundPiece.GetComponent<Piece>().pieceType;
            if (capturerIsSente) senteHeldPieceType[pieceTypeIndex]--;
            else goteHeldPieceType[pieceTypeIndex]--;
            
            IsHeldPieceSelected = false;
            FoundPiece = null;

            _shogiManager.ClearHighlights();
            
            _shogiManager.activePlayer = !_shogiManager.activePlayer; // ターンを切り替える
            
            OnHeldPieceChanged?.Invoke();
        }
    }
}