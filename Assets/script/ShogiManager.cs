using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class ShogiManager : MonoBehaviour
{
    // シングルトン管理
    public static ShogiManager Instance { get; private set; }

    // 現在選択されている駒（グローバル）
    public static Piece CurrentSelectedPiece;        // 選択中の駒

    // ゲーム進行・状態管理
    public static bool activePlayer; // 現在のターン（true:先手, false:後手）

    // 二歩チェック用の歩の列情報
    public bool[] senteFuPosition = new bool[9]; // 先手の歩の列状態
    public bool[] goteFuPosition = new bool[9];  // 後手の歩の列状態

    // 駒生成などのプレハブ参照
    [SerializeField] GameObject piecePrefab;

    // スプライト管理
    public Sprite[] defaultSprites = new Sprite[8];
    public Sprite[] promotedSprites = new Sprite[8];
    
    // ハイライトの管理
    [SerializeField] GameObject highlightPrefab; // 駒のハイライト用プレハブ
    List<GameObject> _activeHighlights = new();
    SpriteRenderer _sr;
    
    public static bool CanSelect; // 選択状況を管理するフラグ

    [SerializeField] GameObject buttons;
    [SerializeField] Button trueButton;
    [SerializeField] Button falseButton;

    private bool? _playerChoice;
    private Camera _camera;

    Piece _piece;
    [SerializeField] HeldPieceManager heldPieceManager; // 持ち駒管理
    [SerializeField] ShogiEngineManager shogiEngMan; // エンジン管理

    void Awake()
    {
        _camera = Camera.main;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"{activePlayer}.{CanSelect}");
        }
    }

    void Start()
    {
        buttons.SetActive(false);
        activePlayer = true;
        CanSelect = true; // 初期状態では選択可能
        
        trueButton.onClick.AddListener(() => Choose(true));
        falseButton.onClick.AddListener(() => Choose(false));
        
        // 駒の配置
        CreatePieces(Piece.PieceId.Hu, 9, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 7, 3, "歩兵");
        CreatePieces(Piece.PieceId.Keima, 2, new[] { 2, 8 }, 9, 1, "桂馬");
        CreatePieces(Piece.PieceId.Gin, 2, new[] { 3, 7 }, 9, 1, "銀将");
        CreatePieces(Piece.PieceId.Kin, 2, new[] { 4, 6 }, 9, 1, "金将");
        CreatePieces(Piece.PieceId.Kyosha, 2, new[] { 1, 9 }, 9, 1, "香車");
        CreatePieces(Piece.PieceId.Gyoku, 1, new[] { 5 }, 9, 1, "玉将");
        CreateDiagonalPieces(Piece.PieceId.Kaku, 8, 8, 2, 2, "角");
        CreateDiagonalPieces(Piece.PieceId.Hisha, 2, 8, 8, 2, "飛車");

        shogiEngMan.SetStartPosition();
    }
    
    //----------------------------------
    //------------駒の初期配置------------
    //----------------------------------
    void CreatePieces(Piece.PieceId pieceType, int loopCount, int[] posX, int sentePosY, int gotePosY, string pieceName)
    {
        Sprite defaultSprite = defaultSprites[(int)pieceType];
        Sprite promotedSprite = promotedSprites[(int)pieceType];

        for (int i = 0; i < loopCount; i++)
        {
            int x = posX[i];
        
            // 先手の駒を作成
            CreateSinglePiece(pieceType, x, sentePosY, "Sente", $"先手:{pieceName}.{i + 1}", 
                defaultSprite, promotedSprite, x - 1, true);
        
            // 後手の駒を作成
            CreateSinglePiece(pieceType, x, gotePosY, "Gote", $"後手:{pieceName}.{i + 1}", 
                defaultSprite, promotedSprite, x - 1, false);
        }
    }
    
    void CreateDiagonalPieces(Piece.PieceId pieceType, int senteX, int senteY, int goteX, int goteY, string pieceName)
    {
        Sprite defaultSprite = defaultSprites[(int)pieceType];
        Sprite promotedSprite = promotedSprites[(int)pieceType];
    
        // 先手の駒を作成
        CreateSinglePiece(pieceType, senteX, senteY, "Sente", $"先手:{pieceName}.1", 
            defaultSprite, promotedSprite, -1, true);
    
        // 後手の駒を作成
        CreateSinglePiece(pieceType, goteX, goteY, "Gote", $"後手:{pieceName}.1", 
            defaultSprite, promotedSprite, -1, false);
    }
    
    // -----駒の設置-----
    void CreateSinglePiece(Piece.PieceId pieceType, int posX, int posY, string tag, string pieceName, 
        Sprite defaultSprite, Sprite promotedSprite, int fuPositionIndex, bool isSente)
    {
        // 駒の
        GameObject piece = Instantiate(piecePrefab, new Vector2(posX, posY), Quaternion.identity);
        Piece pieceScript = piece.GetComponent<Piece>();
    
        // 駒の基本設定
        piece.tag = tag;
        piece.name = pieceName;
        pieceScript.ApplyStatePiece(pieceType);
        pieceScript.defaultSprite = defaultSprite;
        pieceScript.promotedSprite = promotedSprite;
    
        // 歩兵の場合は位置情報を記録（fuPositionIndex が -1 でない場合のみ）
        if (pieceType == Piece.PieceId.Hu && fuPositionIndex >= 0)
        {
            if (isSente)
                senteFuPosition[fuPositionIndex] = true;
            else
                goteFuPosition[fuPositionIndex] = true;
        }
    }
    
    //----------------------------------
    //------------選択中の処理------------
    //----------------------------------
    void OnMouseDown()
    {
        // マウス座標をワールド座標に変換
        Vector3 mousePosition = Input.mousePosition;
        if (Camera.main != null)
        {
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
    
            // 駒レイヤーでの当たり判定
            LayerMask pieceLayer = LayerMask.GetMask("Piece");
            Collider2D hitPiece = Physics2D.OverlapPoint(worldMousePos, pieceLayer);
    
            // 駒がクリックされた場合は何もしない
            if (hitPiece != null) return;
        }

        // 盤面がクリックされた場合、駒を設置
        if (CurrentSelectedPiece != null && CurrentSelectedPiece.isSelect)
        {
            // 盤面のクリック処理を実行
            CurrentSelectedPiece.OnBoardClick();
        }
        
        // 持ち駒が選択されている場合は、持ち駒配置処理を実行
        if (HeldPieceManager.FoundPiece != null && HeldPieceManager.IsHeldPieceSelected)
        {
            HeldPieceManager heldPieceManager = FindObjectOfType<HeldPieceManager>();
            if (heldPieceManager != null)
            {
                // 持ち駒配置処理を実行
                Piece.PieceId currentPieceType = HeldPieceManager.SelectedPieceType;
                heldPieceManager.SelectedHeldPiece(HeldPieceManager.FoundPiece, currentPieceType);
            }
        }
    }
    
    public void ClearPieceSelection()
    {
        if (CurrentSelectedPiece != null)
        {
            CurrentSelectedPiece.isSelect = false;
            CurrentSelectedPiece = null;
            ClearHighlights();
        }
    }

    public void ClearHeldPieceSelection()
    {
        HeldPieceManager.IsHeldPieceSelected = false;
        HeldPieceManager.FoundPiece = null;
        ClearHighlights();
    }
    
    //----------------------------------
    //-----------AI専用処理--------------
    //----------------------------------
    
    // エンジンからの移動情報を受信する
    public async void ReceiveEngineMove(string moveString)
    {
        if (moveString[1].ToString() == "*") // 持ち駒の場合の処理
        {
            Debug.Log("持ち駒の移動を処理します: " + moveString);
        }
        else if (moveString.Length > 4　&& moveString[5].ToString() == "+") // 成駒の場合の処理
        {
            Debug.Log("成駒の処理をします: " + moveString);
        }
        else // 通常の移動の場合
        {
            var moveData = await ParseMoveStringAsync(moveString);

            if (moveData != null)
            {
                var data = moveData.Value;
                await ExecuteEngineMoveAsync(data.startIndex, data.endIndex, data.toX, data.toY);
            }
        }
    }
    
    private async UniTask ExecuteEngineMoveAsync(int fromX, int fromY, int toX, int toY)
    {
        // メインスレッドに切り替え
        await UniTask.SwitchToMainThread();
        ExecuteEngineMove(fromX, fromY, toX, toY);
    }
    
    void ExecuteEngineMove(int fromX, int fromY, int toX, int toY)
    {
        // AI手番チェック
        if (activePlayer)
        {
            return;
        }
    
        // 駒を探す
        LayerMask pieceLayer = LayerMask.GetMask("Piece");
        Vector2 fromPosition = new Vector2(fromX, fromY);
        Collider2D fromPieceCollider = Physics2D.OverlapPoint(fromPosition, pieceLayer);

        if (fromPieceCollider != null)
        {
            Piece movingPiece = fromPieceCollider.GetComponent<Piece>();
            if (movingPiece != null)
            {
                string expectedTag = "Gote";
                string actualTag = fromPieceCollider.gameObject.tag;
            
                if (actualTag != expectedTag)
                {
                    Debug.LogError($"❌ Wrong piece! AI trying to move {actualTag} piece, but should move {expectedTag}");
                    return;
                }
                
                Vector2 toPosition = new Vector2(toX, toY);
                movingPiece.ExecuteAIMove(toPosition);
                
                // ✅ AIの手を記譜法に変換して履歴に追加
                string aiMoveNotation = ConvertToShogiNotation(fromPosition, toPosition);
                
                ShogiEngineManager engineManager = FindObjectOfType<ShogiEngineManager>();
                if (engineManager != null)
                {
                    engineManager.AddMoveToHistory(aiMoveNotation);
                }
                
                activePlayer = !activePlayer;
            }
        }
        else
        {
            Debug.LogError($"❌ 駒がない ({fromX},{fromY})");
        }
    }
    
    //---------駒形式の変換------------
    // aiの移動形式の変換
    async UniTask<(int startIndex, int endIndex, int toX, int toY)?> ParseMoveStringAsync(string moveString)
    {
        //　文字列チェック
        if (moveString.Length < 4)
        {
            Debug.LogWarning($"フォーマットが違います: {moveString}");
            return null;
        }
        
        /*if (moveString[1].ToString() == "*") // 持ち駒の場合の処理
        {
            await UniTask.SwitchToMainThread();
            char pieceChar = moveString[0]; // 駒の種類を取得
            switch (pieceChar)
            {
                case 'P': 
                    Debug.Log($"持ち駒: hu.{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Hu, activePlayer);
                    return (0, 0, int.Parse(moveString[2].ToString()), moveString[3] - 'a' + 1); // 歩兵
                case 'N':
                    Debug.Log($"持ち駒: keima{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Keima, activePlayer);
                    return (0, 0, 0, 0); // 桂馬
                case 'S':
                    Debug.Log($"持ち駒: gin{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Gin, activePlayer);
                    return (0, 0, 0, 0); // 銀将
                case 'G':Debug.Log($"持ち駒: kin{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Kin, activePlayer);
                    return (0, 0, 0, 0); // 金将
                case 'K':Debug.Log($"持ち駒: gyoku{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Gyoku, activePlayer);
                    return (0, 0, 0, 0); // 玉将
                case 'L':Debug.Log($"持ち駒: kyosha{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Kyosha, activePlayer);
                    return (0, 0, 0, 0); // 香車
                case 'R':Debug.Log($"持ち駒: hisha{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Hisha, activePlayer);
                    return (0, 0, 0, 0); // 飛車
                case 'B':Debug.Log($"持ち駒: kaku{activePlayer}");
                    heldPieceManager.RemoveHeldPiece(Piece.PieceId.Kaku, activePlayer);
                    return (0, 0, 0, 0); // 角
                default:
                    Debug.LogWarning($"不明な持ち駒: {pieceChar}");
                    return null;
            }
        }*/
        /*if (moveString[4].ToString() == "+") // 成駒の場合の処理
        {
            Debug.LogWarning("成駒");
        }*/
        
        // 駒の種類を取得
        int shogiFromX = int.Parse(moveString[0].ToString());
        char fromYChar = moveString[1];
        int shogiToX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        
        // Debug.Log(moveString);
        
        // 文字を数字に変換
        int fromY = fromYChar - 'a' + 1;
        int toY = toYChar - 'a' + 1;
        return (shogiFromX, fromY, shogiToX, toY);
    }
    
    // *aiの移動形式の変換
    public string ConvertToShogiNotation(Vector2 fromPos, Vector2 toPos)
    {
        char fromYChar = (char)('a' + (int)fromPos.y - 1);
        char toYChar = (char)('a' + (int)toPos.y - 1);
    
        string notation = $"{fromPos.x}{fromYChar}{toPos.x}{toYChar}";
    
        return notation;
    }
    
    //----------------------------------
    //---------ハイライトの管理------------
    //----------------------------------
    
    // ハイライトの生成
    void CreateHighlightSquare(Vector2 position)
    {
        GameObject highlight = Instantiate(highlightPrefab, position, Quaternion.identity);
        highlight.tag = "Highlight";
        highlight.layer = LayerMask.NameToLayer("Default");
        highlight.GetComponent<SpriteRenderer>().sortingOrder = 1;
        
        _sr = highlight.GetComponent<SpriteRenderer>();
        _sr.color = new Color(1f, 1f, 1f, 0.6f);
        
        _activeHighlights.Add(highlight);
        highlight.name = $"{position.x}.{position.y}";
        
        highlight.transform.SetParent(this.transform, false);
    }

    public void CreateMoveHighlightSquares(List<Vector2> canMovePositions, Vector2 position)
    {
        for (int x = 1; x <= 9; x++)
        {
            for (int y = 1; y <= 9; y++)
            {
                Vector2 highlightPosition = new Vector2(x, y);
                GameObject nowCheckedPiece = 
                    Physics2D.OverlapPoint(highlightPosition, LayerMask.GetMask("Piece"))?.gameObject;
                
                if (!canMovePositions.Contains(highlightPosition) && highlightPosition != position)
                {
                    CreateHighlightSquare(highlightPosition);
                }
                else if (canMovePositions.Contains(highlightPosition) && nowCheckedPiece != null)
                {
                    string currentTurnTag = activePlayer ? "Sente" : "Gote";
                    
                    if (nowCheckedPiece.CompareTag(currentTurnTag))
                    {
                        CreateHighlightSquare(highlightPosition);
                    }
                }
            }
        }
    }

    public void CreateDropHighlightSquares(Piece.PieceId pieceType)
    {
        for (int x = 1; x <= 9; x++)
        {
            if (pieceType == Piece.PieceId.Hu)
            {
                bool fuPositionCheck = activePlayer ? senteFuPosition[x - 1] : goteFuPosition[x - 1];
                if (fuPositionCheck)
                {
                    // その列（x座標）の全マスを設置不可としてハイライト
                    for (int fy = 1; fy <= 9; fy++)
                    {
                        Vector2 invalidPosition = new Vector2(x, fy);
                        CreateHighlightSquare(invalidPosition);
                    }
                    continue;
                }
            }
            for (int y = 1; y <= 9; y++)
            {
                Vector2 highlightPosition = new Vector2(x, y);
                GameObject nowCheckedPiece = 
                    Physics2D.OverlapPoint(highlightPosition, LayerMask.GetMask("Piece"))?.gameObject;

                switch (pieceType)
                {
                    case Piece.PieceId.Hu:
                    case Piece.PieceId.Kyosha:
                    case Piece.PieceId.Keima:
                        if (!IsValidDropPosition(pieceType, highlightPosition))
                        {
                            CreateHighlightSquare(highlightPosition);
                            continue;
                        }
                        break;
                }

                // 盤上に駒がない場合はハイライトを生成
                if (nowCheckedPiece != null)
                {
                    CreateHighlightSquare(highlightPosition);
                }
            }
        }
    }

    bool IsValidDropPosition(Piece.PieceId pieceType, Vector2 position)
    {
        int y = (int)position.y;
    
        switch (pieceType)
        {
            case Piece.PieceId.Hu:    // 歩兵
            case Piece.PieceId.Kyosha: // 香車
                return activePlayer ? y > 1 : y < 9;
            
            case Piece.PieceId.Keima:  // 桂馬
                return activePlayer ? y > 2 : y < 8;
            
            default:
                return true;
        }
    }

    // 駒の選択をクリア
    public void ClearHighlights()
    {
        foreach (GameObject highlight in _activeHighlights)
        {
            if (highlight != null) Destroy(highlight);
        }
        _activeHighlights.Clear();
    }

    //----------------------------------
    //---------成駒選択の処理------------
    //----------------------------------
    void Choose(bool choice)
    {
        _playerChoice = choice;
        buttons.SetActive(false);
    }
    
    public async UniTask<bool> WaitForPlayerChoiceAsync(int pieceType, Vector3 pos)
    {
        //　選択画面を表示
        buttons.SetActive(true);
        CanSelect = false;
        
        Image promoteImage = trueButton.GetComponentInChildren<Image>();
        Image defaltImage = falseButton.GetComponentInChildren<Image>();

        promoteImage.sprite = promotedSprites[pieceType];
        defaltImage.sprite = defaultSprites[pieceType];
        
        // RectTransformを取得
        RectTransform rectTransform = buttons.GetComponent<RectTransform>();
    
        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = _camera.WorldToScreenPoint(pos);
    
        // スクリーン座標をCanvas内の座標に変換
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            buttons.transform.parent.GetComponent<RectTransform>(),
            screenPos,
            null, // Canvasがスクリーンスペースオーバーレイの場合はnull
            out localPoint
        );
    
        // RectTransformの位置を設定
        rectTransform.anchoredPosition = localPoint;
        rectTransform.rotation = activePlayer ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 0f, 180f);
    
        _playerChoice = null;
    
        await UniTask.WaitUntil(() => _playerChoice.HasValue);
    
        // 選択結果を返す
        return _playerChoice.Value;
    }
}

