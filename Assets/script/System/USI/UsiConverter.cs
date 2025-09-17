using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public static class UsiConverter
{
    /// <summary>
    /// 送られた指し手の文字列を分解
    /// </summary>
    public static 
        (int fromX, int fromY, int toX, int toY, bool isPromote) ParseMoveString(string moveString)
    {
        //　文字列チェック
        if (moveString.Length < 4)
        {
            Debug.LogWarning($"フォーマットが異なります: {moveString}");
        }
        
        // 移動元の座標
        int fromX = int.Parse(moveString[0].ToString());
        char fromYChar = moveString[1];
        // 移動先の座標
        int toX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        
        // int型に変換
        int fromY = fromYChar - 'a' + 1;
        int toY = toYChar - 'a' + 1;
        
        // 成駒のチェック
        bool isFastPromote = (moveString.Length == 5 && moveString[4].ToString() == "+");
        
        // 成駒でない場合
        return (fromX, fromY, toX, toY, isFastPromote);
    }

    /// <summary>
    /// 送られた持ち駒の指し手の文字列を分解
    /// </summary>
    public static (PieceType type, int toX, int toY) ParseDropString(string moveString)
    {
        // 持ち駒の処理
        if (moveString.Length < 4 || moveString[1] != '*')
        {
            Debug.LogWarning($"フォーマットが異なります: {moveString}");
        }
        
        char pieceChar = moveString[0]; // 駒の種類
        int toX = int.Parse(moveString[2].ToString());
        char toYChar = moveString[3];
        int toY = toYChar - 'a' + 1;

        PieceType pieceType = PieceCharToType(pieceChar);
        return (pieceType, toX, toY);
    }
    
    /// <summary>
    /// 指し手を記譜法に変換
    /// </summary>
    public static string ToUsiMove(Vector2Int fromPos, Vector2Int toPos)
    {
        int fromX = fromPos.x;
        char fromYChar = (char)('a' + fromPos.y - 1);
        int toX = toPos.x;
        char toYChar = (char)('a' + toPos.y - 1);
        
        return $"{fromX}{fromYChar}{toX}{toYChar}";
    }
    
    /// <summary>
    /// 成りの記号を指し手に追加
    /// </summary>
    public static string AddPromote(string moveNotation)
    {
        return moveNotation + "+";
    }

    /// <summary>
    /// 持ち駒の指し手を記譜法に変換
    /// </summary>
    public static string ToUsiDrop(PieceType pieceType, Vector2Int toPos)
    {
        int toX = toPos.x;
        char toYChar = (char)('a' + toPos.y - 1);
        string pieceChar = PieceTypeToChar(pieceType);
        
        return $"{pieceChar}*{toX}{toYChar}";
    }

    /// <summary>
    /// 盤面の状態をSFEN形式に変換
    /// </summary>
    public static string ConvertBoardToSfen(PieceType[,] board)
    {
        string sfen = "";
        // 盤面の状態をSFEN形式に変換
        for (int y = 1; y <= 9; y++)
        {
            if (y > 1) sfen += "/";
            for (int x = 9; x >= 1; x--)
            {
                PieceType pieceType = board[x - 1, y - 1];
                if (pieceType == PieceType.None)
                {
                    char delimiterChar = '/'; // 区切り文字
                    if (sfen[^1] == delimiterChar || !int.TryParse(sfen[^1].ToString(), out _))
                    {
                        sfen += "1"; // 連続する空白の数を増やす
                    }
                    else
                    {
                        int lastEmptyCount = int.Parse(sfen[^1].ToString());
                        sfen = sfen.Remove(sfen.Length - 1) + (lastEmptyCount + 1);
                    }
                }
                else
                {
                    Piece piece = ShogiManager.Instance.GetPieceAt(new Vector2Int(x, y));
                    if (piece.isPromoted)
                    {
                        sfen += "+";
                        pieceType = piece.BasePieceType;
                    }
                    
                    string pieceChar = PieceTypeToChar(pieceType);
                    if (piece.PieceTurn == Turn.後手)
                    {
                        pieceChar = pieceChar.ToLower();
                    }
                    sfen += pieceChar;
                }
            }
        }
        return sfen;
    }
    
    /// <summary>
    /// 持ち駒の状態をSFEN形式に変換
    /// </summary>
    public static string ConvertCapturesToSfen(int[] senteCapturedPieces, int[] goteCapturedPieces)
    {
        string sfen = "";
        // 先手の持ち駒
        for (int i = 0; i < senteCapturedPieces.Length; i++)
        {
            int count = senteCapturedPieces[i];
            if (count > 0)
            {
                string pieceChar = PieceTypeToChar((PieceType)i);
                if (senteCapturedPieces[i] > 1)
                {
                    sfen += senteCapturedPieces[i];
                }
                sfen += pieceChar;
            }
        }
        // 後手の持ち駒
        for (int i = 0; i < goteCapturedPieces.Length; i++)
        {
            int count = goteCapturedPieces[i];
            if (count > 0)
            {
                string pieceChar = PieceTypeToChar((PieceType)i);
                if (goteCapturedPieces[i] > 1)
                {
                    sfen += goteCapturedPieces[i];
                }
                sfen += pieceChar.ToLower();
            }
        }
        return sfen == "" ? "-" : sfen;
    }

    /// <summary>
    /// char型の駒の種類をPieceTypeに変換
    /// </summary>
    private static PieceType PieceCharToType(char pieceChar)
    {
        PieceType pieceType = pieceChar switch
        {
            'P' => PieceType.歩兵, // 歩兵
            'L' => PieceType.香車, // 香車
            'N' => PieceType.桂馬, // 桂馬
            'S' => PieceType.銀将, // 銀将
            'G' => PieceType.金将, // 金将
            'B' => PieceType.角行, // 角行
            'R' => PieceType.飛車, // 飛車
            'K' => PieceType.玉将, // 玉将
            _ => throw new ArgumentException("不明な持ち駒: " + pieceChar)
        };
        return pieceType;
    }

    /// <summary>
    /// PieceTypeをchar型の駒の種類に変換
    /// </summary>
    private static string PieceTypeToChar(PieceType pieceType)
    {
        string pieceChar = pieceType switch
        {
            PieceType.歩兵 => "P", // 歩兵
            PieceType.香車 => "L", // 香車
            PieceType.桂馬 => "N", // 桂馬
            PieceType.銀将 => "S", // 銀将
            PieceType.金将 => "G", // 金将
            PieceType.角行 => "B", // 角行
            PieceType.飛車 => "R", // 飛車
            PieceType.玉将 => "K", // 玉将
            _ => throw new ArgumentException("不明な持ち駒: " + pieceType)
        };
        return pieceChar;
    }
}