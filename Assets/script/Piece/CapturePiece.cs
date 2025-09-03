using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CapturePiece : MonoBehaviour
{
    public PieceType capturePieceType;  // 駒の種類
    public Turn capturePieceTurn;       // 取得している持ち駒の手番
    
    public Sprite umPromSprite; // 成る前のスプライト
    
    private List<Vector2Int> _movable = new ();
    
    [SerializeField] public GameObject piecePrefab;
    
    /// <summary>
    /// 持ち駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">持ち駒の種類</param>
    /// <param name="captureSprite">持ち駒のスプライト</param>
    public void ApplyStateCapturePiece(PieceType pieceType, Sprite captureSprite)
    {
        // 駒の種類に応じてスプライトを設定
        capturePieceType = pieceType;
        umPromSprite = captureSprite;
        
        // スプライトを設定
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = umPromSprite;
        spriteRenderer.sortingOrder = 20;

        if (transform.CompareTag("Sente"))
        {
            capturePieceTurn = Turn.先手;
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            capturePieceTurn = Turn.後手;
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }
    }
    
    /// <summary>
    /// 持ち駒の選択処理
    /// </summary>
    public void SelectCapturePiece()
    {
        if (ShogiManager.Instance.activePlayer != capturePieceTurn) return;
        
        // 先手と後手の持ち駒の数を取得
        int pieceIndex = (int)capturePieceType;
        
        int currentCount = capturePieceTurn == Turn.先手
            ? ShogiManager.Instance.senteCapturedPieceType[pieceIndex]
            : ShogiManager.Instance.goteCapturedPieceType[pieceIndex];
        
        if (currentCount <= 0)
        {
            Debug.Log("持ち駒がありません: " + capturePieceType);
            return;
        }
        
        // 駒の選択処理
        if (ShogiManager.Instance.activePlayer == capturePieceTurn)
        {
            if (ShogiManager.Instance.curSelPiece == null)
            {
                ShogiManager.Instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.Instance.curSelPiece.name + "が選択されました");
                SettingCapturePiece();
            }
            else
            {
                ShogiManager.Instance.curSelPiece = null;
                Debug.Log("駒の選択が解除されました");
            }
        }
    }
    
    public async void SettingCapturePiece()
    {
        // 持ち駒の設置可能なマス目のチェック
        List<Vector2Int> checkMovablePositions = CheckMovablePositions();
        
        ShogiManager.Instance.moveHighlight.SetCanMovePosHighlight(checkMovablePositions);
        
        Vector2Int clickedPoint = await WaitForMouseClick();
        
        // クリックされた位置が移動可能なマス目かチェック
        if (!checkMovablePositions.Contains(clickedPoint))
        {
            ShogiManager.Instance.CancelSelection();
            Debug.Log("クリックされた位置は設置可能なマス目ではありません: " + clickedPoint);
            return;
        }

        // 駒を指す
        ShogiManager.Instance.SetCapturedPiece(capturePieceType, clickedPoint);
        
        // ターンの終了
        ShogiManager.Instance.EndTurnPhase(clickedPoint);
    }

    private List<Vector2Int> CheckMovablePositions()
    {
        for (int i = 1; i <= 9; i++)
        {
            for (int j = 1; j <= 9; j++)
            {
                Vector2Int position = new Vector2Int(i, j);
                if (ShogiManager.Instance.GetPieceTypeAt(position) == PieceType.None)
                {
                    switch (capturePieceType)
                    {
                        case PieceType.歩兵:
                            bool[] fuPosition = ShogiManager.Instance.activePlayer == Turn.先手
                                ? ShogiManager.Instance.senteFuPosition
                                : ShogiManager.Instance.goteFuPosition;
                            if (position.y >= (capturePieceTurn == Turn.先手 ? 9 : 1) || fuPosition[position.x - 1])
                                continue;
                            break;
                        case PieceType.香車:
                            if (position.y >= (capturePieceTurn == Turn.先手 ? 9 : 1))
                                continue;
                            break;
                        case PieceType.桂馬:
                            if (position.y >= (capturePieceTurn == Turn.先手 ? 8 : 2))
                                continue;
                            break;
                    }
                    _movable.Add(position);
                }
            }
        }
        return _movable;
    }
    
    /// <summary>
    /// マウスクリックを待機する
    /// </summary>
    /// <returns>クリックされた位置の座標</returns>
    private async UniTask<Vector2Int> WaitForMouseClick()
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0));

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
    }

    /// <summary>
    /// 持ち駒のビジュアルを状態に応じて更新する
    /// </summary>
    public void UpdateVisualState()
    {
        int pieceIndex = (int)capturePieceType;
        // 先手と後手の持ち駒の数を取得
        int currentCount = (capturePieceTurn == Turn.先手) ?
            ShogiManager.Instance.senteCapturedPieceType[pieceIndex]:
            ShogiManager.Instance.goteCapturedPieceType[pieceIndex];
        
        (PieceType, Turn) key = (capturePieceType, capturePieceTurn);
        int cloneCount = CapturePieceUIManager.instance.CloneGroups[key].Count;

        if (cloneCount <= 1 && currentCount <= 1)
        {
            // 持ち駒の数が変わった場合、UIを更新
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = currentCount > 0 ? Color.white : Color.gray;
        }
        else
        {
            if (cloneCount < currentCount)
            {
                // 持ち駒が増えた場合
                CapturePieceUIManager.instance.AddCapturedPiece(this.GameObject());
            }
            else if (cloneCount > currentCount)
            {
                // 持ち駒が減った場合
                CapturePieceUIManager.instance.RemoveCapturedPiece(this.GameObject());
            }
        }
    }
}
