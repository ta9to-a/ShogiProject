using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class Piece : MonoBehaviour, IPointerClickHandler
{
    //【位置・状態管理関連】
    int _shogiPositionX; // x現在地
    int _shogiPositionY; // y現在地
    public bool isSelect; // 駒の選択状態
    bool _isPromote; // 成駒かどうか
    bool _isFastPromote; // 初期の成駒状況
    public int moveDirection; // 先手なら+1、後手なら-1
    int _lastHuPositionY; // 二歩防止用に座標を保存
    public bool leftEnemyCampThisTurn; // 成駒選択が可能済の駒か
    public bool nowInEnemyCamp; // 現在の駒が敵陣にいるかどうか
    public bool wasPromote; // 敵陣にいたかどうか
    
    //【操作範囲・制限関連】
    Vector2 _mouseMinPos = new(0.5f, 0.5f); // マウス選択の座標の下限値
    Vector2 _mouseMaxPos = new(9.5f, 9.5f); // マウス選択の座標の上限値
    [SerializeField] List<Vector2> canMovePositions = new(); // 駒の移動範囲（リスト）

    //【見た目】
    public Sprite defaultSprite; // デフォルトの見た目
    public Sprite promotedSprite; // 成駒の見た目
    SpriteRenderer _renderer; // Sprite描画コンポーネント

    //【他コンポーネント・管理スクリプト】
    ShogiManager _shogiManager; // ゲーム管理スクリプト
    HeldPieceManager _heldPieceManager; // 持ち駒管理クラス
    
    //【UI関連】
    private Vector2 _lastMovePosition;
    private PieceId _lastPieceType;
    
    //【駒の種類識別】
    public enum PieceId
    {
        Hu,
        Kyosha,
        Keima,
        Gin,
        Kin,
        Kaku,
        Hisha,
        Gyoku,
    }
    
    [SerializeField] public PieceId pieceType; // 駒の種類
    
    void Start()
    {
        _heldPieceManager = FindObjectOfType<HeldPieceManager>();
        _shogiManager = FindObjectOfType<ShogiManager>();

        _shogiPositionX = (int)transform.position.x;
        _shogiPositionY = (int)transform.position.y;

        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sprite = defaultSprite;

        _isFastPromote = false;
    }
    
    //-------駒のタイプ設定-------
    public void ApplyStatePiece(PieceId type)
    {
        if (gameObject.CompareTag("Sente"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 180f);
            moveDirection = -1;
        }
        else if (gameObject.CompareTag("Gote"))
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0);
            moveDirection = 1;
        }

        pieceType = type;
    }
    
    //-----駒の状態をリセット-----
    void ResetCapState(GameObject enemyPiece)
    {
        Piece capturedPiece = enemyPiece.GetComponent<Piece>();
        if (capturedPiece != null)
        {
            capturedPiece.isSelect = false;
            capturedPiece._isPromote = false;
            
            capturedPiece.wasPromote = false;
            capturedPiece.nowInEnemyCamp = false;
            capturedPiece.leftEnemyCampThisTurn = false;

            capturedPiece._renderer.sprite = capturedPiece.defaultSprite;
        }
    }

    //-------------------------
    //---------駒の移動---------
    //-------------------------
    
    // 駒をクリックしたときの処理
    public void OnPointerClick(PointerEventData eventData)
    {
        if (ShogiManager.CanSelect)
        {
            if (ShogiManager.CurrentSelectedPiece != null)
            {
                // 持ち駒の選択状況をリセット
                if (ShogiManager.CurrentSelectedPiece == this)
                {
                    isSelect = false;
                    ShogiManager.CurrentSelectedPiece = null;

                    canMovePositions.Clear(); // 移動可能位置のリストをクリア
                    _shogiManager.ClearHighlights();

                    return;
                }

                // 選択中の駒がある場合は移動処理を実行
                if (ShogiManager.CurrentSelectedPiece.isSelect)
                {
                    ShogiManager.CurrentSelectedPiece.OnBoardClick();
                    return;
                }
            }

            if (ShogiManager.ActivePlayer && gameObject.CompareTag("Sente") ||
                !ShogiManager.ActivePlayer && gameObject.CompareTag("Gote"))
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
                }
            }
        }
    }

    // 選択状態での処理
    public async void OnBoardClick()
    {
        if (Camera.main != null)
        {
            // マウス座標をオブジェクト座標に変換し、int型に変更
            Vector2 mousePosition = Input.mousePosition;
            Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector2 intMousePos = new Vector2((int)Math.Round(worldMousePos.x), (int)Math.Round(worldMousePos.y));

            GameObject selectedPieceForTeam = null;

            // 将棋盤の範囲外をクリックしたら、選択状態を解除
            if (worldMousePos.x <= _mouseMinPos.x || worldMousePos.y <= _mouseMinPos.y ||
                worldMousePos.x >= _mouseMaxPos.x || worldMousePos.y >= _mouseMaxPos.y)
            {
                _shogiManager.ClearPieceSelection();
                return;
            }

            //-----移動可能なマス目なら-----
            if (canMovePositions.Contains(intMousePos))
            {
                // 選択した場所にすでに駒があるか
                LayerMask pieceLayer = LayerMask.GetMask("Piece");
                Collider2D collidedPiece = Physics2D.OverlapPoint(worldMousePos, pieceLayer);
                if (collidedPiece != null) selectedPieceForTeam = collidedPiece.gameObject;
                if (selectedPieceForTeam != null)
                {
                    // 選択した駒のマスにあるオブジェクトタグ
                    GameObject enemyPiece = collidedPiece.gameObject;
                    if (!enemyPiece.CompareTag(gameObject.tag))
                    {
                        Piece capturedPiece = enemyPiece.GetComponent<Piece>();

                        if (!gameObject.CompareTag(enemyPiece.tag)) //敵オブジェクトなら
                        {
                            // 持ち駒に追加
                            bool tagIsSente = gameObject.CompareTag("Sente");
                            _heldPieceManager.AddHeldPiece(enemyPiece, capturedPiece.pieceType, tagIsSente);
                            // 駒の状態をリセット
                            ResetCapState(enemyPiece);

                            if (capturedPiece.pieceType == PieceId.Hu)
                            {
                                bool[] targetFuPositions = !tagIsSente
                                    ? _shogiManager.senteFuPosition
                                    : _shogiManager.goteFuPosition;
                                targetFuPositions[(int)intMousePos.x - 1] = false;
                            }
                        }
                    }
                    else
                    {
                        _shogiManager.ClearHighlights();
                        isSelect = false;
                        return;
                    }
                }

                // ---駒の移動---
                Vector2 fromPosition = transform.position;
                MovePiece(intMousePos);
                _shogiManager.ClearHighlights();
                isSelect = false;
                
                // -----敵陣の判定処理-----
                // 敵陣にいたかどうかを判定
                wasPromote =
                    (gameObject.CompareTag("Sente") && fromPosition.y <= 3) ||
                    (gameObject.CompareTag("Gote") && fromPosition.y >= 7);

                // 敵陣にいるかどうかを判定
                nowInEnemyCamp =
                    (gameObject.CompareTag("Sente") && _shogiPositionY <= 3) ||
                    (gameObject.CompareTag("Gote") && _shogiPositionY >= 7);
                
                leftEnemyCampThisTurn = wasPromote && !nowInEnemyCamp;

                // --- 成駒選択の処理 ---
                if (nowInEnemyCamp || leftEnemyCampThisTurn)
                {
                    if (!_isPromote)
                    {
                        // 強制成りが必要な駒（歩・香・桂）のみ適用する
                        switch (pieceType)
                        {
                            case PieceId.Hu:
                            case PieceId.Kyosha: 
                                if (_shogiPositionY <= 1 && gameObject.CompareTag("Sente") ||
                                    _shogiPositionY >= 9 && gameObject.CompareTag("Gote"))
                                    ForcePromote(intMousePos);
                                break;
        
                            case PieceId.Keima:
                                if (_shogiPositionY <= 2 && gameObject.CompareTag("Sente") ||
                                    _shogiPositionY >= 8 && gameObject.CompareTag("Gote")) 
                                    ForcePromote(intMousePos);
                                break;
                        }

                        // 成駒選択の処理
                        if (!_isPromote)
                        {
                            bool promote =
                                await _shogiManager.WaitForPlayerChoiceAsync((int)pieceType, transform.position);
                            HandlePromotionChoice(promote, pieceType, intMousePos);
                            
                            ShogiManager.CurrentSelectedPiece = null;
                        }
                    }
                }
                // 駒の形式変換
                string moveNotation = _shogiManager.ConvertToShogiNotation(fromPosition, intMousePos);
                if (_isFastPromote) // 成駒選択が行われたターンのみ+をつける
                {
                    moveNotation += "+";
                    _isFastPromote = false;
                }
                
                string objectTag = gameObject.CompareTag("Sente") ? "☗" : "☖";
                Debug.Log(objectTag + " " + moveNotation);
                
                // 履歴に追加
                ShogiEngineManager engineManager = FindObjectOfType<ShogiEngineManager>();
                if (engineManager != null)
                {
                    engineManager.AddMoveToHistory(moveNotation);
                }
                
                ShogiManager.ActivePlayer = !ShogiManager.ActivePlayer; // ターンを切り替える
                
                if (!ShogiManager.ActivePlayer) // 後手（AI）のターン
                {
                    if (engineManager != null)
                    {
                        // AIに最善手を要求
                        engineManager.RequestBestMoveWithHistory();
                    }
                }
                ShogiManager.CurrentSelectedPiece = null;
            }
            else // 選択された場所が移動不可な場所な場合
            {
                isSelect = false;
                ShogiManager.CurrentSelectedPiece = null;

                // ハイライトをクリア
                _shogiManager.ClearHighlights();
            }
        }
    }
    
    // 成駒選択の処理
    private void HandlePromotionChoice(bool promote, PieceId pieceType, Vector2 position)
    {
        if (promote)
        {
            _isPromote = true;
            leftEnemyCampThisTurn = false;
            _renderer.sprite = promotedSprite;
            _isFastPromote = true;

            if (pieceType == PieceId.Hu)
            {
                bool isSente = gameObject.CompareTag("Sente");
                bool[] targetFuPositions = isSente ? _shogiManager.senteFuPosition : _shogiManager.goteFuPosition;
                targetFuPositions[(int)position.x - 1] = false;
            }
        }
        else
        {
            leftEnemyCampThisTurn = true;
        }

        ShogiManager.CanSelect = true;
    }

    // 強制的に成る処理
    public void ForcePromote(Vector2 pos)
    {
        _isPromote = true;
        leftEnemyCampThisTurn = false;
        _renderer.sprite = promotedSprite;
        _isFastPromote = true;

        // この駒がと金なら、二歩防止リストからこの筋を外す
        if (pieceType == PieceId.Hu)
        {
            bool isSente = gameObject.CompareTag("Sente");
            bool[] targetFuPositions =
                isSente ? _shogiManager.senteFuPosition : _shogiManager.goteFuPosition;

            targetFuPositions[(int)pos.x - 1] = false;
        }

        ShogiManager.CanSelect = true;
    }
    
    // AIの持ち駒追加
    public void ExecuteAIMove(Vector2 position, bool fastPromote)
    {
        LayerMask pieceLayer = LayerMask.GetMask("Piece");
        Collider2D targetPieceCollider = Physics2D.OverlapPoint(position, pieceLayer);

        if (targetPieceCollider != null)
        {
            GameObject capturedPiece = targetPieceCollider.gameObject;
            Piece capturedPieceComponent = capturedPiece.GetComponent<Piece>();
            if (capturedPieceComponent != null && !capturedPiece.CompareTag(gameObject.tag))
            {
                // 持ち駒に追加
                bool tagIsSente = gameObject.CompareTag("Sente");
                _heldPieceManager.AddHeldPiece(capturedPiece, capturedPieceComponent.pieceType, tagIsSente);
                
                // 駒の状態をリセット
                ResetCapState(capturedPiece);

                if (capturedPieceComponent.pieceType == PieceId.Hu)
                {
                    bool[] targetFuPositions = !tagIsSente
                        ? _shogiManager.senteFuPosition
                        : _shogiManager.goteFuPosition;
                    targetFuPositions[(int)position.x - 1] = false;
                }
            }
        }
        
        if (fastPromote)
        {
            _isPromote = true;
            _renderer.sprite = promotedSprite;
        }
        
        MovePiece(position);
    }
    
    // 移動処理(player or AI)
    public void MovePiece(Vector2 position)
    {
        _shogiPositionX = (int)position.x;
        _shogiPositionY = (int)position.y;
        transform.position = new Vector3(_shogiPositionX, _shogiPositionY, 0);
    }
    
    //----------------------------
    //-----移動範囲のリストを管理-----
    //----------------------------
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

        // ハイライト表示
        _shogiManager.CreateMoveHighlightSquares(canMovePositions, position);
    }
    
    void CheckLinearPaths(Vector2 startPosition, Vector2[] directions) // 直線的な移動
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
}