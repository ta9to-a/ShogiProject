using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CapturePiece : MonoBehaviour
{
    public PieceType capturePieceType; // 駒の種類
    private int _heldPieceCount; // 持ち駒の数
    private Turn _capturePieceTurn;
    
    private Sprite _captureSprite; // 成る前のスプライト
    
    private List<Vector2Int> _movable = new ();
    
    /// <summary>
    /// 持ち駒の情報を保存・更新する
    /// </summary>
    /// <param name="pieceType">持ち駒の種類</param>
    /// <param name="captureSprite">持ち駒のスプライト</param>
    public void ApplyStateCapturePiece(PieceType pieceType, Sprite captureSprite)
    {
        // 駒の種類に応じてスプライトを設定
        capturePieceType = pieceType;
        _capturePieceTurn = transform.CompareTag("Sente") ? Turn.先手 : Turn.後手;
        
        _captureSprite = captureSprite;
        
        // スプライトを設定
        GetComponent<SpriteRenderer>().sprite = _captureSprite;
        
        // 先手と後手のタグを設定
        transform.rotation = transform.CompareTag("Sente") ?
            Quaternion.Euler(0, 0, 0) :     // 先手
            Quaternion.Euler(0, 0, 180);    // 後手

        UpdateVisualState();
    }
    
    /// <summary>
    /// 持ち駒の選択処理
    /// </summary>
    public void SelectCapturePiece()
    {
        if (ShogiManager.instance.activePlayer != _capturePieceTurn) return;
        
        // 駒の選択処理
        if (ShogiManager.instance.activePlayer == _capturePieceTurn)
        {
            if (ShogiManager.instance.curSelPiece == null)
            {
                ShogiManager.instance.curSelPiece = this.gameObject;
                Debug.Log(ShogiManager.instance.curSelPiece.name + "が選択されました");
                SettingCapturePiece();
            }
            else
            {
                ShogiManager.instance.curSelPiece = null;
                Debug.Log("駒の選択が解除されました");
            }
        }
    }
    
    private async void SettingCapturePiece()
    {
        // 持ち駒の設置可能なマス目のチェック
        List<Vector2Int> checkMovablePositions = CheckMovablePositions();
        
        Vector2Int clickedPoint = await WaitForMouseClick();
        
        // クリックされた位置が移動可能なマス目かチェック
        if (!checkMovablePositions.Contains(clickedPoint))
        {
            ShogiManager.instance.CancelSelection();
            Debug.Log("クリックされた位置は設置可能なマス目ではありません: " + clickedPoint);
            return;
        }

        // 駒を指す
        ShogiManager.instance.SetCapturedPiece(capturePieceType, clickedPoint);
        ShogiManager.instance.EndTurnPhase();
    }

    private List<Vector2Int> CheckMovablePositions()
    {
        for (int i = 1; i <= 9; i++)
        {
            for (int j = 1; j <= 9; j++)
            {
                Vector2Int position = new Vector2Int(i, j);
                if (ShogiManager.instance.GetPieceTypeAt(position) == PieceType.None)
                {
                    switch (capturePieceType)
                    {
                        case PieceType.歩兵:
                        case PieceType.香車:
                            if (position.y >= (_capturePieceTurn == Turn.先手 ? 9 : 1))
                                continue;
                            break;
                        case PieceType.桂馬:
                            if (position.y >= (_capturePieceTurn == Turn.先手 ? 8 : 2))
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
        int currentCount = _capturePieceTurn == Turn.先手 ? 
            ShogiManager.instance.senteCapturedPieceType[pieceIndex] : 
            ShogiManager.instance.goteCapturedPieceType[pieceIndex];
        
        // スプライトの色を更新
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = currentCount > 0 ? Color.white : Color.gray;
    }
}
