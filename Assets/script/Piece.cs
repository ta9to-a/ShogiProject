using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class Piece : MonoBehaviour, IPointerClickHandler
{
    //【位置・状態管理関連】
    int _shogiPositionX;         // x現在地
    int _shogiPositionY;         // y現在地
    public bool isSelect;        // 駒の選択状態
    bool _isPromote;             // 成駒かどうか
    bool _isHeldPiece;           // 持ち駒として選択されているか
    int _lastHuPositionY;        // 二歩防止用に座標を保存
    public int moveDirection;    // 先手なら+1、後手なら-1
    
    //【操作範囲・制限関連】
    Vector2 _mouseMinPos = new (0.5f, 0.5f);                    // マウス選択の座標の下限値
    Vector2 _mouseMaxPos = new (9.5f, 9.5f);                    // マウス選択の座標の上限値
    [SerializeField] List<Vector2> canMovePositions = new ();   // 駒の移動範囲（リスト）
    
    //【見た目】
    public Sprite defaultSprite;      // デフォルトの見た目
    public Sprite promotedSprite;     // 成駒の見た目
    SpriteRenderer _renderer;         // Sprite描画コンポーネント
    
    //【他コンポーネント・管理スクリプト】
    ShogiManager _shogiManager;         // ゲーム管理スクリプト
    HeldPieceManager _heldPieceManager; // 持ち駒管理クラス
    
    //【駒の種類識別】
    public enum PieceId
    {
        Hu, Kyosha, Keima, Gin, Kin, Kaku, Hisha, Gyoku,
    }
    [SerializeField] public PieceId pieceType; // 駒の種類
    
    //-------駒のタイプ設定-------
    public void ApplyStatePiece(PieceId type)
    {
        if (gameObject.CompareTag("Sente"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            moveDirection = 1;
        }
        else if(gameObject.CompareTag("Gote"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 180f);
            moveDirection = -1;
        }
        pieceType = type;
    }
    
    void Start()
    {
        _shogiManager = ShogiManager.Instance;
        _heldPieceManager = FindObjectOfType<HeldPieceManager>();

        _shogiPositionX = (int)transform.position.x;
        _shogiPositionY = (int)transform.position.y;
        
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sprite = defaultSprite;
    }
    
    //-----駒のクリックを検知-----
    public void OnPieceClick(PointerEventData eventData)
    {
        
        if (ShogiManager.CurrentSelectedPiece != null)
        {
            // 同じ駒をクリックした場合は選択解除
            if (ShogiManager.CurrentSelectedPiece == this)
            {
                isSelect = false;
                ShogiManager.CurrentSelectedPiece = null;
                Debug.Log("駒の選択を解除しました");
                return;
            }
        
            // 選択中の駒がある場合は移動処理を実行
            if (ShogiManager.CurrentSelectedPiece.isSelect)
            {
                ShogiManager.CurrentSelectedPiece.OnBoardClick();
                return;
            }
        }
        
        if (_shogiManager.nowTurn && gameObject.CompareTag("Sente") ||
            !_shogiManager.nowTurn && gameObject.CompareTag("Gote"))
        {
            // 現在のターンを確認
            if (!isSelect)
            {
                isSelect = true; // 選択中に変更
                ShogiManager.CurrentSelectedPiece = this; // 現在選択中の駒として設定
                
                // 持ち駒の選択状況をリセット
                if (HeldPieceManager.IsHeldPieceSelected || HeldPieceManager.FoundPiece != null)
                {
                    _shogiManager.ClearHeldPieceSelection();
                }

                // リストを再読み込み
                canMovePositions.Clear();
                ApplyMovePosition(transform.position);
                
                foreach (Vector2 pos in canMovePositions)
                {
                    Debug.Log($"移動可能位置: ({pos.x}, {pos.y})");
                }
                
                Debug.Log("選択中：" + isSelect);
            }
        }
    }

    public void OnBoardClick()
    { 
        if (Camera.main != null)
        {
            // マウス座標をオブジェクト座標に変換し、int型に変更
            Vector2 mousePosition = Input.mousePosition;
            Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector2 intMousePos = new Vector2((int)Math.Round(worldMousePos.x), (int)Math.Round(worldMousePos.y));
            
            GameObject selectedPieceForTeam = null;
        
            // 将棋盤の範囲外のクリック判定をなくす
            if (worldMousePos.x <= _mouseMinPos.x || worldMousePos.y <= _mouseMinPos.y || 
                worldMousePos.x >= _mouseMaxPos.x || worldMousePos.y >= _mouseMaxPos.y) return;
            
            //---設置ポジションがリストに含まれるか---
            if (canMovePositions.Contains(intMousePos))
            {
                // Pieceのみのコライダーを取得する
                LayerMask pieceLayer = LayerMask.GetMask("Piece");
                Collider2D collidedPiece = Physics2D.OverlapPoint(worldMousePos, pieceLayer);
                if (collidedPiece != null)
                {
                    selectedPieceForTeam = collidedPiece.gameObject;

                }
            
                if (selectedPieceForTeam != null) //設置ポジションにすでに駒があるか
                {
                    // 選択した駒のマスにあるオブジェクトタグ
                    GameObject enemyPiece = collidedPiece.gameObject;
                    if (!enemyPiece.CompareTag(gameObject.tag))
                    {
                        Piece capturedPiece = enemyPiece.GetComponent<Piece>();

                        if (!gameObject.CompareTag(enemyPiece.tag))
                        {
                            // 持ち駒に追加
                            bool tagIsSente = gameObject.CompareTag("Sente");
                            _heldPieceManager.AddHeldPiece(enemyPiece, capturedPiece.pieceType, tagIsSente);

                            if (capturedPiece.pieceType == PieceId.Hu)
                            {
                                bool[] targetFuPositions = !tagIsSente ? _shogiManager.senteFuPosition : _shogiManager.goteFuPosition;
                                targetFuPositions[(int)intMousePos.x - 1] = false;
                            }
                        }
                    }
                    else
                    {
                        isSelect = false;
                        Debug.Log("すでに駒があります");
                        return;
                    }
                }

                // 駒を設置
                _shogiPositionX = (int)intMousePos.x;
                _shogiPositionY = (int)intMousePos.y;
                transform.position = new Vector2(_shogiPositionX, _shogiPositionY); //駒の座標を変数の値にする

                isSelect = false;
                Debug.Log("Piece.OnBoardClick: 駒移動成功のため isSelect = false");
                _shogiManager.nowTurn = !_shogiManager.nowTurn;
                Debug.Log("現在のターン = " + _shogiManager.nowTurn);

                //---成駒選択---
                if (_shogiPositionY >= 7 && gameObject.CompareTag("Sente") || _shogiPositionY <= 3 && gameObject.CompareTag("Gote"))
                {
                    if (!_isPromote) 
                    {
                        // この駒がと金なら、二歩防止リストからこの筋を外す
                        if (pieceType == PieceId.Hu)
                        {
                            bool isSente = gameObject.CompareTag("Sente");
                            bool[] targetFuPositions = isSente ? _shogiManager.senteFuPosition : _shogiManager.goteFuPosition;
                        
                            targetFuPositions[(int)intMousePos.x - 1] = false;
                        }
                        
                        _renderer.sprite = promotedSprite;
                        _isPromote = true;
                    }
                }
            }
            else
            {
                isSelect = false;
                ShogiManager.CurrentSelectedPiece = null;
                Debug.Log("Piece.OnBoardClick: 移動可能範囲外のため isSelect = false");
            }
        }
    }
    
    //-----移動範囲のリストを管理-----
    void ApplyMovePosition(Vector2 position)
    {
        if (!_isPromote)
        {
            switch (pieceType)
            {
                case PieceId.Hu:
                    canMovePositions.Add(new Vector2(position.x, position.y + 1 * moveDirection));
                    break;

                case PieceId.Keima:
                    canMovePositions.Add(new Vector2(position.x + 1, position.y + 2 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x - 1, position.y + 2 * moveDirection));
                    break;

                case PieceId.Gin:
                    canMovePositions.Add(new Vector2(position.x, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x - 1, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x + 1, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x - 1, position.y - 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x + 1, position.y - 1 * moveDirection));
                    break;

                case PieceId.Kin:
                    GetKinMovement(position);
                    break;

                case PieceId.Gyoku:
                    canMovePositions.Add(new Vector2(position.x, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y));
                    canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y));
                    canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y + 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x, position.y - 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y - 1 * moveDirection));
                    canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y - 1 * moveDirection));
                    break;

                case PieceId.Kyosha:
                    CheckLinearPaths(position, new[] { Vector2.up * moveDirection });
                    break;

                case PieceId.Kaku:
                    CheckLinearPaths(position,
                        new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) });
                    break;

                case PieceId.Hisha:
                    CheckLinearPaths(position, new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right });
                    break;
            }
        }
        else
        {
            // 成金の判別
            if (PieceId.Hu == pieceType || PieceId.Kyosha == pieceType || 
                PieceId.Keima == pieceType || PieceId.Gin == pieceType)
            {
                GetKinMovement(position);
            }
            
            else if (PieceId.Kaku == pieceType)
            {
                CheckLinearPaths(position,
                    new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) });
                
                canMovePositions.Add(new Vector2(position.x, position.y + 1 * moveDirection));
                canMovePositions.Add(new Vector2(position.x, position.y - 1 * moveDirection));
                canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y));
                canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y));
            }
            
            else if (PieceId.Hisha == pieceType)
            {
                CheckLinearPaths(position,
                    new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right });
                
                canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y + 1 * moveDirection));
                canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y + 1 * moveDirection));
                canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y - 1 * moveDirection));
                canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y - 1 * moveDirection));
            }
        }
    }

void CheckLinearPaths(Vector2 startPosition, Vector2[] directions)
{
    foreach (var direction in directions)
    {
        for (int i = 1; i < 9; i++)
        {
            Vector2 targetPosition = startPosition + direction * i;
            if (targetPosition.x < 1 || targetPosition.x > 9 || targetPosition.y < 1 || targetPosition.y > 9) break;
            
            LayerMask pieceLayer = LayerMask.GetMask("Piece");
            Collider2D foundPieceCol = Physics2D.OverlapPoint(targetPosition, pieceLayer);
            
            if (foundPieceCol != null)
            {
                // 確認したマスのオブジェクトの有無
                Piece otherPiece = foundPieceCol.GetComponent<Piece>();
                if (otherPiece != null)
                {
                    if (!otherPiece.CompareTag(gameObject.tag))
                    {
                        canMovePositions.Add(targetPosition);
                    }
                    break;
                }
            }
            canMovePositions.Add(targetPosition);
        }
    }
}
    
    void GetKinMovement(Vector2 position) //金・成金の関数
    {
        canMovePositions.Add(new Vector2(position.x, position.y + 1 * moveDirection));
        canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y));
        canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y));
        canMovePositions.Add(new Vector2(position.x + 1 * moveDirection, position.y + 1 * moveDirection));
        canMovePositions.Add(new Vector2(position.x - 1 * moveDirection, position.y + 1 * moveDirection));
        canMovePositions.Add(new Vector2(position.x, position.y - 1 * moveDirection));
    }

    public void Reset()
    {
        _isPromote = false;
        _renderer.sprite = defaultSprite;
    }

    IEnumerator ResetGameAfterDelay(float delay)
    {
        // 指定された秒数だけ待機
        yield return new WaitForSeconds(delay);

        // シーンをリロード
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}