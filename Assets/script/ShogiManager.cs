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
    public bool activePlayer; // 現在のターン（true:先手, false:後手）

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
    // [SerializeField] ShogiEngineManager shogiEngineManager;

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
    
    void Start()
    {
        buttons.SetActive(false);
        activePlayer = true;
        CanSelect = true; // 初期状態では選択可能
        
        trueButton.onClick.AddListener(() => Choose(true));
        falseButton.onClick.AddListener(() => Choose(false));
        
        // 全ての駒の配置
        CreatePieces(Piece.PieceId.Hu,9,new [] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 7, 3, "歩兵");
        CreatePieces(Piece.PieceId.Keima,2,new [] { 2, 8 }, 9, 1, "桂馬");
        CreatePieces(Piece.PieceId.Gin, 2, new [] { 3, 7 }, 9, 1, "銀将");
        CreatePieces(Piece.PieceId.Kin, 2,new [] { 4, 6 }, 9, 1, "金将");
        CreatePieces(Piece.PieceId.Kyosha, 2,new [] { 1, 9 }, 9, 1, "香車");
        CreatePieces(Piece.PieceId.Gyoku, 1,new [] { 5, 5 }, 9, 1, "玉将");
        CreateDiagonalPieces(Piece.PieceId.Kaku, 8, 8, 2, 2, "角");
        CreateDiagonalPieces(Piece.PieceId.Hisha, 2, 8, 8, 2, "飛車");
    }
    
    //----------------------------------
    //------------駒の初期配置------------
    //----------------------------------
    void CreatePieces(Piece.PieceId pieceType,int loopCount, int[] posX, int sentePosY, int gotePosY, string pieceName)
    {
        Sprite defaultSprite = defaultSprites[(int)pieceType];
        Sprite promotedSprite = promotedSprites[(int)pieceType];

        for (int i = 0; i < loopCount; i++)
        {
            int x = posX[i];
        
            // 先手の駒
            GameObject sentePiece = Instantiate(piecePrefab, new Vector2(x, sentePosY), Quaternion.identity);
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();
            
            // 先手の駒の詳細設定
            sentePiece.tag = "Sente";
            sentePieceScript.ApplyStatePiece(pieceType);
            sentePieceScript.defaultSprite = defaultSprite;
            sentePieceScript.promotedSprite = promotedSprite;
            if (pieceType == Piece.PieceId.Hu)
            {
                senteFuPosition[x - 1] = true;
            }
            
            sentePiece.name = $"先手:{pieceName}.{i + 1}";
        
            // 後手の駒
            GameObject gotePiece = Instantiate(piecePrefab, new Vector2(x, gotePosY), Quaternion.identity);
            Piece gotePieceScript = gotePiece.GetComponent<Piece>();
            
            // 後手の駒の詳細設定
            gotePiece.tag = "Gote";
            gotePieceScript.ApplyStatePiece(pieceType);
            gotePieceScript.defaultSprite = defaultSprite;
            gotePieceScript.promotedSprite = promotedSprite;
            if (pieceType == Piece.PieceId.Hu)
            {
                goteFuPosition[x - 1] = true;
            }
            
            gotePiece.name = $"後手:{pieceName}.{i + 1}";
            
        }
    }
    void CreateDiagonalPieces(Piece.PieceId pieceType, int senteX, int senteY, int goteX, int goteY, string pieceName)
    {
        Sprite defaultSprite = defaultSprites[(int)pieceType];
        Sprite promotedSprite = promotedSprites[(int)pieceType];
        
        // 先手の駒
        GameObject sentePiece = Instantiate(piecePrefab, new Vector2(senteX, senteY), Quaternion.identity);
        Piece sentePieceScript = sentePiece.GetComponent<Piece>();
            
        // 先手の駒の詳細設定
        sentePiece.tag = "Sente";
        sentePieceScript.ApplyStatePiece(pieceType);
        sentePieceScript.defaultSprite = defaultSprite;
        sentePieceScript.promotedSprite = promotedSprite;
            
        sentePiece.name = $"先手:{pieceName}.1";
        
        // 後手の駒
        GameObject gotePiece = Instantiate(piecePrefab, new Vector2(goteX, goteY), Quaternion.identity);
        Piece gotePieceScript = gotePiece.GetComponent<Piece>();
            
        // 後手の駒の詳細設定
        gotePiece.tag = "Gote";
        gotePieceScript.ApplyStatePiece(pieceType);
        gotePieceScript.defaultSprite = defaultSprite;
        gotePieceScript.promotedSprite = promotedSprite;
            
        gotePiece.name = $"後手:{pieceName}.1";
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
    // 文字列から移動情報を解析する
    /*(int fromX, int fromY, int toX, int toY)? ParseMoveString(string moveString)
    {
        //　文字列チェック
        if (moveString.Length < 4)
        {
            Debug.LogWarning($"Invalid move format: {moveString}");
            return null;
        }
        
        // 駒の種類を取得
        int shogiFromX = int.Parse(moveString[0].ToString());
        char fromYChar = moveString[1];
        int shogiToX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        
        int fromX = 10 - shogiFromX; // 1→9, 2→8, ..., 9→1
        int toX = 10 - shogiToX;
        
        // 文字を数字に変換
        int fromY = fromYChar - 'a' + 1; // a→1, b→2, ..., i→9
        int toY = toYChar - 'a' + 1;
    
        Debug.Log($"🔄 Convert: {moveString} → 将棋({shogiFromX},{fromYChar}) → Unity({fromX},{fromY}) to Unity({toX},{toY})");
        return (fromX, fromY, toX, toY);
    }
    
    // エンジンからの移動情報を受信する
    public void ReceiveEngineMove(string moveString)
    {
        var moveData = ParseMoveString(moveString);
        if (moveData != null)
        {
            Debug.Log($"📍 Engine move: ({moveData.Value.fromX},{moveData.Value.fromY}) → ({moveData.Value.toX},{moveData.Value.toY})");
            
            ExecuteEngineMoveAsync(
                moveData.Value.fromX, moveData.Value.fromY, 
                moveData.Value.toX, moveData.Value.toY).Forget();
        }
    }
    
    private async UniTaskVoid ExecuteEngineMoveAsync(int fromX, int fromY, int toX, int toY)
    {
        // メインスレッドに切り替え
        await UniTask.SwitchToMainThread();
        ExecuteEngineMove(fromX, fromY, toX, toY);
    }

    void ExecuteEngineMove(int fromX, int fromY, int toX, int toY)
    {
        // ✅ 1行だけの簡潔ログ
        Debug.Log($"🎯 Turn: {(activePlayer ? "先手" : "後手")} | Engine trying: ({fromX},{fromY})→({toX},{toY})");
    
        // AI手番チェック
        if (activePlayer)
        {
            Debug.LogWarning($"❌ Wrong turn! Current: 先手, but AI(後手) is trying to move");
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
            
                Debug.Log($"🔍 Piece check: Expected={expectedTag}, Actual={actualTag}");
            
                if (actualTag != expectedTag)
                {
                    Debug.LogError($"❌ Wrong piece! AI trying to move {actualTag} piece, but should move {expectedTag}");
                    return;
                }
                
                Vector2 toPosition = new Vector2(toX, toY);
                movingPiece.ExecuteAIMove(toPosition);
                
                // ✅ AIの手を記譜法に変換して履歴に追加
                string aiMoveNotation = ConvertToShogiNotation(fromPosition, toPosition);
                Debug.Log($"🤖 AI move: {aiMoveNotation}");
            
                ShogiEngineManager engineManager = FindObjectOfType<ShogiEngineManager>();
                if (engineManager != null)
                {
                    engineManager.AddMoveToHistory(aiMoveNotation);
                }
                
                activePlayer = !activePlayer;
                Debug.Log($"✅ Move OK");
            }
        }
        else
        {
            Debug.LogError($"❌ 駒がない ({fromX},{fromY})");
        }
    }
    
    string ConvertToShogiNotation(Vector2 fromPos, Vector2 toPos)
    {
        // Unity座標 → 将棋座標
        int shogiFromX = 10 - (int)fromPos.x;
        int shogiToX = 10 - (int)toPos.x;
    
        char fromYChar = (char)('a' + (int)fromPos.y - 1);
        char toYChar = (char)('a' + (int)toPos.y - 1);
    
        string notation = $"{shogiFromX}{fromYChar}{shogiToX}{toYChar}";
    
        Debug.Log($"🔄 AI move Unity({fromPos.x},{fromPos.y}) → 将棋({shogiFromX},{fromYChar}) = {notation}");
    
        return notation;
    }*/
    
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
    
    public IEnumerator WaitForPlayerChoice(int pieceType, Vector3 pos, Action<bool> onComplete)
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
    
        buttons.SetActive(true);
        _playerChoice = null;
    
        yield return new WaitUntil(() => _playerChoice.HasValue);
    
        onComplete?.Invoke(_playerChoice != null && _playerChoice.Value);
    }
}

