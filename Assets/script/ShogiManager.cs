using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ShogiManager : MonoBehaviour
{
    public bool nowTurn; //現在のターン
    // Start is called before the first frame update

    [SerializeField] GameObject piece;

    public Sprite[] defaultSprites = new Sprite[8];
    public Sprite[] promotedSprites = new Sprite[8];

    public GameObject sentePiece; //先手の駒
    public GameObject gotePiece; //後手の駒

    [SerializeField] public List<GameObject> capturedPieceSente = new List<GameObject>();
    [SerializeField] public List<GameObject> capturedPieceGote = new List<GameObject>();

    //二歩防止
    public bool[] senteFuPosition = new bool[9];
    public bool[] goteFuPosition = new bool[9];

    Piece _pieceScript;

    [Header("Held Piece Positionig - Sente")] [SerializeField]
    private Vector2 senteHeldPiecePosition = new Vector2(11f, 3f); //先手の持ち駒の開始位置

    [SerializeField] private float senteHeldPieceXoffset = 0.8f; //先手の持ち駒間の水平方向の間隔
    [SerializeField] private float senteHeldPieceYoffset = 0.8f; //先手の持ち駒間の垂直方向の間隔
    [SerializeField] private float sentePiecePerRow = 3; //先手の一行に並べる駒の数

    [Header("Held Piece Positionig - Gote")] [SerializeField]
    private Vector2 goteHeldPiecePosition = new Vector2(-1f, 7f); //後手の持ち駒の開始位置

    [SerializeField] private float goteHeldPieceXoffset = -0.8f; //後手の持ち駒間の水平方向の間隔
    [SerializeField] private float goteHeldPieceYoffset = -0.8f; //後手の持ち駒間の垂直方向の間隔
    [SerializeField] private float gotePiecePerRow = 3;

    // --- 次の持ち駒の配置位置を取得するメソッド ---
    public Vector2 GetNextheldPiecePosition(GameObject piece, bool isSente)
    {
        List<GameObject> targetList = isSente ? capturedPieceSente : capturedPieceGote;
        Vector2 startPositon;
        float xOffset;
        float yOffset;
        int piecePerRow;

        if (isSente)
        {
            startPositon = senteHeldPiecePosition;
            xOffset = senteHeldPieceXoffset;
            yOffset = senteHeldPieceYoffset;
            piecePerRow = (int)sentePiecePerRow;
        }
        else
        {
            startPositon = goteHeldPiecePosition;
            xOffset = goteHeldPieceXoffset;
            yOffset = goteHeldPieceYoffset;
            piecePerRow = (int)gotePiecePerRow;
        }
        int count = targetList.Count;
        int row = count / piecePerRow;
        int column = count % piecePerRow;
        
        return new Vector2(startPositon.x + xOffset * column, startPositon.y + yOffset * row);
    }
    
    // --- 持ち駒が取られた後、その駒を新しい持ち駒の位置に配置するメソッド ---
    public void AddCapturedPiece(GameObject capturedPiese, bool isSente)
    {
        Piece pieceScript = capturedPiese.GetComponent<Piece>();
        if (pieceScript == null) return;

        Vector2 nextPosition = GetNextheldPiecePosition(capturedPiese, isSente);
        capturedPiese.transform.position = nextPosition;
        pieceScript.SetPosition((int)nextPosition.x, (int)nextPosition.y);
        if (isSente)
        {
            capturedPiese.tag = "Sente";
            capturedPieceSente.Add(capturedPiese);
        }
        else
        {
            capturedPiese.tag = "Gote";
            capturedPieceGote.Add(capturedPiese);
        }
        Debug.Log($"{ (isSente ? "先手" : "後手")}が駒 ({capturedPiese.name}) を取り、持ち駒に追加した。");
    }

    void Start()
    {
        nowTurn = true;
        FuCreateInstance(); 
        KeimaCreateInstance(); 
        GinCreateInstance(); 
        KinCreateInstance(); 
        GyokuCreateInstance(); 
        KyoshaCreateInstance(); 
        HishaCreateInstance(); 
        KakuCreateInstance();
    }

    void FuCreateInstance()
    {
        //歩兵作成
        int startPositionx = 1;
        Sprite fuDefaultSprite = defaultSprites[(int)Piece.PieceId.Hu];
        Sprite fuPromotedSprites = promotedSprites[(int)Piece.PieceId.Hu];

        for (int i = 0; i < 9; i++)
        {
            //先手の駒を作成
            GameObject sentePiece = Instantiate(piece, new Vector2(startPositionx, 3), Quaternion.identity);
            sentePiece.tag = "Sente";
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();

            senteFuPosition[i] = true;

            sentePieceScript.defaultSprite = fuDefaultSprite;
            sentePieceScript.promotedSprite = fuPromotedSprites;
            sentePieceScript.SetPieceType(Piece.PieceId.Hu);

            //後手の駒を作成
            GameObject gotePiece = Instantiate(piece, new Vector2(startPositionx, 7), Quaternion.identity);
            gotePiece.tag = "Gote";
            Piece gotePieceScript = gotePiece.GetComponent<Piece>(); //-の移動方向に変更

            goteFuPosition[i] = true;

            gotePieceScript.defaultSprite = fuDefaultSprite;
            gotePieceScript.promotedSprite = fuPromotedSprites;
            gotePieceScript.SetPieceType(Piece.PieceId.Hu);

            sentePiece.name = "先手:歩兵." + (i + 1);
            gotePiece.name = "後手:歩兵." + (i + 1);
            startPositionx += 1;
        }
    }

    void KeimaCreateInstance()
    {
        // 桂馬の作成
        int[] startX = { 2, 8 }; // 配置列（2列目と8列目）

        Sprite keimaDefaultSprite = defaultSprites[(int)Piece.PieceId.Keima];
        Sprite keimaPromotedSprite = promotedSprites[(int)Piece.PieceId.Keima];

        for (int i = 0; i < 2; i++)
        {
            int x = startX[i];

            // 先手 桂馬
            sentePiece = Instantiate(piece, new Vector2(x, 1), Quaternion.identity);
            sentePiece.tag = "Sente";
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();

            sentePieceScript.defaultSprite = keimaDefaultSprite;
            sentePieceScript.promotedSprite = keimaPromotedSprite;
            sentePieceScript.SetPieceType(Piece.PieceId.Keima); // pieceType を設定するメソッド

            // 後手 桂馬
            gotePiece = Instantiate(piece, new Vector2(x, 9), Quaternion.identity);
            gotePiece.tag = "Gote";
            Piece gotePieceScript = gotePiece.GetComponent<Piece>();

            gotePieceScript.defaultSprite = keimaDefaultSprite;
            gotePieceScript.promotedSprite = keimaPromotedSprite;
            gotePieceScript.SetPieceType(Piece.PieceId.Keima); // pieceType を設定

            sentePiece.name = "先手:桂馬." + (i + 1);
            gotePiece.name = "後手:桂馬." + (i + 1);
        }
    }

    void GinCreateInstance()
    {
        // 銀の作成
        int[] startX = { 3, 7 };
        Sprite ginDefaultSprite = defaultSprites[(int)Piece.PieceId.Gin];
        Sprite ginPromotedSprite = promotedSprites[(int)Piece.PieceId.Gin];

        for (int i = 0; i < 2; i++)
        {
            int x = startX[i];

            GameObject sentePiece = Instantiate(piece, new Vector2(x, 1), Quaternion.identity);
            sentePiece.tag = "Sente";
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();

            sentePieceScript.defaultSprite = ginDefaultSprite;
            sentePieceScript.promotedSprite = ginPromotedSprite;
            sentePieceScript.SetPieceType(Piece.PieceId.Gin);

            GameObject gotePiece = Instantiate(piece, new Vector2(x, 9), Quaternion.identity);
            gotePiece.tag = "Gote";
            Piece gotePieceScript = gotePiece.GetComponent<Piece>();

            gotePieceScript.defaultSprite = ginDefaultSprite;
            gotePieceScript.promotedSprite = ginPromotedSprite;
            gotePieceScript.SetPieceType(Piece.PieceId.Gin);

            sentePiece.name = "先手:銀将." + (i + 1);
            gotePiece.name = "後手:銀将." + (i + 1);
        }
    }

// ↓ここで独立させて定義
    void KinCreateInstance()
    {
        // 金の作成
        int[] startX = { 4, 6 };
        Sprite kinDefaultSprite = defaultSprites[(int)Piece.PieceId.Kin];
        Sprite kinPromotedSprite = promotedSprites[(int)Piece.PieceId.Kin];

        for (int i = 0; i < 2; i++)
        {
            int x = startX[i];

            GameObject sentePiece = Instantiate(piece, new Vector2(x, 1), Quaternion.identity);
            sentePiece.tag = "Sente";
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();

            sentePieceScript.defaultSprite = kinDefaultSprite;
            sentePieceScript.promotedSprite = kinPromotedSprite;
            sentePieceScript.SetPieceType(Piece.PieceId.Kin);

            GameObject gotePiece = Instantiate(piece, new Vector2(x, 9), Quaternion.identity);
            gotePiece.tag = "Gote";
            Piece gotePieceScript = gotePiece.GetComponent<Piece>();

            gotePieceScript.defaultSprite = kinDefaultSprite;
            gotePieceScript.promotedSprite = kinPromotedSprite;
            gotePieceScript.SetPieceType(Piece.PieceId.Kin);

            sentePiece.name = "先手:金将." + (i + 1);
            gotePiece.name = "後手:金将." + (i + 1);
        }
    }

    void GyokuCreateInstance()
    {
        Sprite GyokuDefaultSprite = defaultSprites[(int)Piece.PieceId.Gyoku];
        Sprite GyokuPromotedSprite = promotedSprites[(int)Piece.PieceId.Gyoku];

        GameObject sentePiece = Instantiate(piece, new Vector2(5, 1), Quaternion.identity);
        sentePiece.tag = "Sente";
        Piece sentePieceScript = sentePiece.GetComponent<Piece>();

        sentePieceScript.defaultSprite = GyokuDefaultSprite;
        sentePieceScript.promotedSprite = GyokuPromotedSprite;
        sentePieceScript.SetPieceType(Piece.PieceId.Gyoku);

        GameObject gotePiece = Instantiate(piece, new Vector2(5, 9), Quaternion.identity);
        gotePiece.tag = "Gote";
        Piece gotePieceScript = gotePiece.GetComponent<Piece>();

        gotePieceScript.defaultSprite = GyokuDefaultSprite;
        gotePieceScript.promotedSprite = GyokuPromotedSprite;
        gotePieceScript.SetPieceType(Piece.PieceId.Gyoku);

        sentePiece.name = "先手:王将.";
        gotePiece.name = "後手:王将.";
    }

    void KyoshaCreateInstance()
    {
        int[] startX = { 1, 9 };
        Sprite kyoshaDefaultSprite = defaultSprites[(int)Piece.PieceId.Kyosha];
        Sprite kyoshaPromotedSprite = promotedSprites[(int)Piece.PieceId.Kyosha];

        for (int i = 0; i < 2; i++)
        {
            int x = startX[i];

            GameObject sentePiece = Instantiate(piece, new Vector2(x, 1), Quaternion.identity);
            sentePiece.tag = "Sente";
            Piece sentePieceScript = sentePiece.GetComponent<Piece>();

            sentePieceScript.defaultSprite = kyoshaDefaultSprite;
            ;
            sentePieceScript.promotedSprite = kyoshaPromotedSprite;
            sentePieceScript.SetPieceType(Piece.PieceId.Kyosha);

            GameObject gotePiece = Instantiate(piece, new Vector2(x, 9), Quaternion.identity);
            gotePiece.tag = "Gote";
            Piece gotePieceScript = gotePiece.GetComponent<Piece>();

            gotePieceScript.defaultSprite = kyoshaDefaultSprite;
            gotePieceScript.promotedSprite = kyoshaPromotedSprite;
            gotePieceScript.SetPieceType(Piece.PieceId.Kyosha);

            sentePiece.name = "先手:香車." + (i + 1);
            gotePiece.name = "後手:香車." + (i + 1);
        }
    }

    void HishaCreateInstance()
    {
        Sprite HishaDefaultSprite = defaultSprites[(int)Piece.PieceId.Hisha];
        Sprite HishaPromotedSprite = promotedSprites[(int)Piece.PieceId.Hisha];

        GameObject sentePiece = Instantiate(piece, new Vector2(8, 2), Quaternion.identity);
        sentePiece.tag = "Sente";
        Piece sentePieceScript = sentePiece.GetComponent<Piece>();

        sentePieceScript.defaultSprite = HishaDefaultSprite;
        sentePieceScript.promotedSprite = HishaPromotedSprite;
        sentePieceScript.SetPieceType(Piece.PieceId.Hisha);
        sentePiece.name = "先手:飛車";

        // 後手 飛車
        GameObject gotePiece = Instantiate(piece, new Vector2(2, 8), Quaternion.identity);
        gotePiece.tag = "Gote";
        Piece gotePieceScript = gotePiece.GetComponent<Piece>();
        gotePieceScript.defaultSprite = HishaDefaultSprite;

        gotePieceScript.promotedSprite = HishaPromotedSprite;
        gotePieceScript.SetPieceType(Piece.PieceId.Hisha);
        gotePiece.name = "後手:飛車";
    }

    void KakuCreateInstance()
    {
        Sprite KakuDefaultSprite = defaultSprites[(int)Piece.PieceId.Kaku];
        Sprite KakuPromotedSprite = promotedSprites[(int)Piece.PieceId.Kaku];

        GameObject sentePiece = Instantiate(piece, new Vector2(2, 2), Quaternion.identity);
        sentePiece.tag = "Sente";
        Piece sentePieceScript = sentePiece.GetComponent<Piece>();

        sentePieceScript.defaultSprite = KakuDefaultSprite;
        sentePieceScript.promotedSprite = KakuPromotedSprite;   
        sentePieceScript.SetPieceType(Piece.PieceId.Kaku);
        sentePiece.name = "先手:角";

        // 後手 飛車
        GameObject gotePiece = Instantiate(piece, new Vector2(8, 8), Quaternion.identity);
        gotePiece.tag = "Gote";
        Piece gotePieceScript = gotePiece.GetComponent<Piece>();
        
        gotePieceScript.defaultSprite = KakuDefaultSprite;
        gotePieceScript.promotedSprite = KakuPromotedSprite;
        gotePieceScript.SetPieceType(Piece.PieceId.Kaku);
        gotePiece.name = "後手:角";
    }
}