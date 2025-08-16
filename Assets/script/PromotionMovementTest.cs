using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test script to verify promotion piece movement commonization
/// Attach this to a GameObject and set the pieceDatabase reference
/// </summary>
public class PromotionMovementTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public PieceDatabase pieceDatabase;
    
    [Header("Test Results (Read Only)")]
    public bool allTestsPassed = false;
    public string testResults = "";
    
    void Start()
    {
        if (pieceDatabase == null)
        {
            Debug.LogError("PieceDatabase not assigned!");
            return;
        }
        
        RunAllTests();
    }
    
    void RunAllTests()
    {
        bool test1 = TestPromotionTypeAssignments();
        bool test2 = TestPromotionPieceDataRetrieval();
        bool test3 = TestMoveRangeRetrieval();
        
        allTestsPassed = test1 && test2 && test3;
        testResults = $"Promotion Type: {(test1 ? "PASS" : "FAIL")}, " +
                     $"Data Retrieval: {(test2 ? "PASS" : "FAIL")}, " +
                     $"Move Range: {(test3 ? "PASS" : "FAIL")}";
        
        Debug.Log($"=== TEST SUMMARY ===");
        Debug.Log($"All Tests Passed: {allTestsPassed}");
        Debug.Log($"Details: {testResults}");
    }
    
    /// <summary>
    /// Test that PieceData has correct promotionType assignments
    /// </summary>
    bool TestPromotionTypeAssignments()
    {
        Debug.Log("=== Testing PromotionType Assignments ===");
        
        var expectedMappings = new Dictionary<PieceType, PieceData.PromotionType>
        {
            { PieceType.歩兵, PieceData.PromotionType.Gold },
            { PieceType.香車, PieceData.PromotionType.Gold },
            { PieceType.桂馬, PieceData.PromotionType.Gold },
            { PieceType.銀将, PieceData.PromotionType.Gold },
            { PieceType.飛車, PieceData.PromotionType.龍王 },
            { PieceType.角行, PieceData.PromotionType.龍馬 },
            { PieceType.金将, PieceData.PromotionType.None },
            { PieceType.玉将, PieceData.PromotionType.None }
        };
        
        bool allCorrect = true;
        
        foreach (var mapping in expectedMappings)
        {
            PieceData pieceData = pieceDatabase.GetPieceData(mapping.Key);
            if (pieceData != null)
            {
                bool isCorrect = pieceData.promotionType == mapping.Value;
                Debug.Log($"{mapping.Key}: Expected {mapping.Value}, Got {pieceData.promotionType} - {(isCorrect ? "✓" : "✗")}");
                
                if (!isCorrect) allCorrect = false;
            }
            else
            {
                Debug.LogWarning($"{mapping.Key}: PieceData not found in database");
                allCorrect = false;
            }
        }
        
        return allCorrect;
    }
    
    /// <summary>
    /// Test that promotion piece data can be retrieved correctly
    /// </summary>
    bool TestPromotionPieceDataRetrieval()
    {
        Debug.Log("=== Testing Promotion Piece Data Retrieval ===");
        
        var promotionTypes = new PieceData.PromotionType[]
        {
            PieceData.PromotionType.Gold,
            PieceData.PromotionType.龍馬,
            PieceData.PromotionType.龍王
        };
        
        bool allFound = true;
        
        foreach (var promotionType in promotionTypes)
        {
            PieceData promotionData = pieceDatabase.GetPromotionPieceData(promotionType);
            bool hasData = promotionData != null;
            Debug.Log($"PromotionType.{promotionType}: {(hasData ? "Found data" : "No data")} - {(hasData ? "✓" : "✗")}");
            
            if (hasData)
            {
                Debug.Log($"  - moveRange length: {promotionData.moveRange?.Length ?? 0}");
                Debug.Log($"  - canStraightMove: {promotionData.canStraightMove}");
            }
            else
            {
                allFound = false;
            }
        }
        
        return allFound;
    }
    
    /// <summary>
    /// Test that move range retrieval works for promoted and unpromoted pieces
    /// </summary>
    bool TestMoveRangeRetrieval()
    {
        Debug.Log("=== Testing Move Range Retrieval Logic ===");
        
        // Test that Gold promotion data has the expected number of moves (6 directions)
        PieceData goldPromotion = pieceDatabase.GetPromotionPieceData(PieceData.PromotionType.Gold);
        if (goldPromotion == null)
        {
            Debug.LogError("Gold promotion data not found!");
            return false;
        }
        
        bool goldMoveCountCorrect = goldPromotion.moveRange.Length == 6;
        Debug.Log($"Gold promotion moveRange count: Expected 6, Got {goldPromotion.moveRange.Length} - {(goldMoveCountCorrect ? "✓" : "✗")}");
        
        // Test that 龍王 promotion data has the expected number of moves (8 directions)
        PieceData dragonKingPromotion = pieceDatabase.GetPromotionPieceData(PieceData.PromotionType.龍王);
        if (dragonKingPromotion == null)
        {
            Debug.LogError("Dragon King promotion data not found!");
            return false;
        }
        
        bool dragonKingMoveCountCorrect = dragonKingPromotion.moveRange.Length == 8;
        Debug.Log($"Dragon King promotion moveRange count: Expected 8, Got {dragonKingPromotion.moveRange.Length} - {(dragonKingMoveCountCorrect ? "✓" : "✗")}");
        
        // Test that 龍馬 promotion data has the expected number of moves (8 directions)
        PieceData dragonHorsePromotion = pieceDatabase.GetPromotionPieceData(PieceData.PromotionType.龍馬);
        if (dragonHorsePromotion == null)
        {
            Debug.LogError("Dragon Horse promotion data not found!");
            return false;
        }
        
        bool dragonHorseMoveCountCorrect = dragonHorsePromotion.moveRange.Length == 8;
        Debug.Log($"Dragon Horse promotion moveRange count: Expected 8, Got {dragonHorsePromotion.moveRange.Length} - {(dragonHorseMoveCountCorrect ? "✓" : "✗")}");
        
        return goldMoveCountCorrect && dragonKingMoveCountCorrect && dragonHorseMoveCountCorrect;
    }
}