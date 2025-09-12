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
        int fromX = 9 - int.Parse(moveString[0].ToString()) + 1;
        char fromYChar = moveString[1];
        // 移動先の座標
        int toX = 9 - int.Parse(moveString[2].ToString()) + 1;
        char toYChar = moveString[3];
        
        // int型に変換
        int fromY = 'a' + 9 - fromYChar;
        int toY = 'a' + 9 - toYChar;
        
        // 成駒のチェック
        bool isFastPromote = (moveString.Length == 5 && moveString[4].ToString() == "+");
        
        // 成駒でない場合
        return (fromX, fromY, toX, toY, isFastPromote);
    }

    /// <summary>
    /// 送られた持ち駒の指し手の文字列を分解
    /// </summary>
    /// <param name="moveString"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static (PieceType type, int toX, int toY) ParseDropString(string moveString)
    {
        // 持ち駒の処理
        if (moveString.Length < 4 || moveString[1] != '*')
        {
            Debug.LogWarning($"フォーマットが異なります: {moveString}");
        }
        
        char pieceChar = moveString[0]; // 駒の種類
        int toX = 9 - int.Parse(moveString[2].ToString()) + 1;
        char toYChar = moveString[3];
        int toY = 'a' + 9 - toYChar;

        PieceType pieceType = PieceCharToType(pieceChar);
        
        return (pieceType, toX, toY);
    }
    
    /// <summary>
    /// 指し手を記譜法に変換
    /// </summary>
    public static string ToUsiMove(Vector2Int fromPos, Vector2Int toPos)
    {
        int fromX = 9 - (fromPos.x - 1);
        char fromYChar = (char)('a' + 9 - fromPos.y);
        int toX = 9 - (toPos.x - 1);
        char toYChar = (char)('a' + 9 - toPos.y);
        
        string notation = $"{fromX}{fromYChar}{toX}{toYChar}";
        
        return notation;
    }
    
    /// <summary>
    /// 成りの記号を指し手に追加
    /// </summary>
    /// <param name="moveNotation"></param>
    /// <returns></returns>
    public static string AddPromote(string moveNotation)
    {
        return moveNotation + "+";
    }

    /// <summary>
    /// 持ち駒の指し手を記譜法に変換
    /// </summary>
    public static string ToUsiDrop(PieceType pieceType, Vector2Int toPos)
    {
        char toYChar = (char)('a' + toPos.y - 1);
        string pieceChar = PieceTypeToChar(pieceType);
        string notation = $"{pieceChar}*{toPos.x}{toYChar}";
        return notation;
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
            'K' => PieceType.玉将, // 玉将
            'R' => PieceType.飛車, // 飛車
            'B' => PieceType.角行, // 角行
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
            PieceType.玉将 => "K", // 玉将
            PieceType.飛車 => "R", // 飛車
            PieceType.角行 => "B", // 角行
            _ => throw new ArgumentException("不明な持ち駒: " + pieceType)
        };
        return pieceChar;
    }
}
