using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

public class ShogiManager : MonoBehaviour
{
    // シングルトン管理
    public static ShogiManager Instance { get; private set; }

    // 現在選択されている駒（グローバル）
    public static Piece CurrentSelectedPiece = null;        // 選択中の駒

    // ゲーム進行・状態管理
    public bool nowTurn; // 現在のターン（true:先手, false:後手）

    // 二歩チェック用の歩の列情報
    public bool[] senteFuPosition = new bool[9]; // 先手の歩の列状態
    public bool[] goteFuPosition = new bool[9];  // 後手の歩の列状態

    // 駒生成などのプレハブ参照
    [SerializeField] GameObject piecePrefab;

    // スプライト管理
    public Sprite[] defaultSprites = new Sprite[8];
    public Sprite[] promotedSprites = new Sprite[8];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        nowTurn = true;
        
        // 全ての駒の配置
        CreatePieces(Piece.PieceId.Hu,9,new [] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 3, 7, "歩兵");
        CreatePieces(Piece.PieceId.Keima,2,new [] { 2, 8 }, 1, 9, "桂馬");
        CreatePieces(Piece.PieceId.Gin, 2, new [] { 3, 7 }, 1, 9, "銀将");
        CreatePieces(Piece.PieceId.Kin, 2,new [] { 4, 6 }, 1, 9, "金将");
        CreatePieces(Piece.PieceId.Kyosha, 2,new [] { 1, 9 }, 1, 9, "香車");
        CreatePieces(Piece.PieceId.Gyoku, 1,new [] { 5, 5 }, 1, 9, "玉将");
        CreateDiagonalPieces(Piece.PieceId.Kaku, 2, 2, 8, 8, "角");
        CreateDiagonalPieces(Piece.PieceId.Hisha, 8, 2, 2, 8, "飛車");
    }
    
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
                heldPieceManager.SelectedHeldPiece(HeldPieceManager.FoundPiece);
            }
        }
    }
    
    public void ClearPieceSelection()
    {
        if (CurrentSelectedPiece != null)
        {
            CurrentSelectedPiece.isSelect = false;
            CurrentSelectedPiece = null;
            Debug.Log("駒の選択をクリアしました。");
        }
    }

    public void ClearHeldPieceSelection()
    {
        HeldPieceManager.IsHeldPieceSelected = false;
        HeldPieceManager.FoundPiece = null;
        Debug.Log("持ち駒の選択をクリアしました。");
    }

    // 駒の配置の詳細
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
}