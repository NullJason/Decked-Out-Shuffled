// // ChessGameManager.cs - Updated with complete implementation
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;

// public enum ChessPieceType { Pawn, Rook, Knight, Bishop, Queen, King }

// public class ChessGameManager : BaseCardGameManager
// {
//     [Header("Chess Settings")]
//     public ChessBoard board;
//     public ChessPiece[,] chessPieces = new ChessPiece[8, 8];
//     public List<ChessPiece> playerPieces = new List<ChessPiece>();
//     public List<ChessPiece> opponentPieces = new List<ChessPiece>();
    
//     [Header("Card Management")]
//     public int maxHandSize = 12;
//     private ChessPiece selectedPiece;
//     private Vector2Int selectedPosition;
    
//     [Header("Turn Management")]
//     public bool hasDrawnCardThisTurn = false;
//     public bool canCheat = true;
//     public bool queenMovementEnabled = false;
//     public bool preventCheckNextTurn = false;
//     public bool immuneToCheck = false;
    
//     [Header("Prefabs")]
//     public GameObject pawnPrefab;
//     public GameObject rookPrefab;
//     public GameObject knightPrefab;
//     public GameObject bishopPrefab;
//     public GameObject queenPrefab;
//     public GameObject kingPrefab;
    
//     private List<CardData> chessCardDeck = new List<CardData>();
//     private List<CardData> mixedDeck = new List<CardData>();
    
//     public override void StartGame()
//     {
//         SetupChessBoard();
//         GenerateChessCards();
//         InitializeMixedDeck();
//         DrawInitialCards();
//         StartFirstTurn();
//     }
    
//     private void StartFirstTurn()
//     {
//         isPlayerTurn = true;
//         actionLocked = false;
//         Debug.Log("Player's turn starts!");
//     }
    
//     private void SetupChessBoard()
//     {
//         // Clear existing pieces
//         for (int x = 0; x < 8; x++)
//         {
//             for (int y = 0; y < 8; y++)
//             {
//                 chessPieces[x, y] = null;
//             }
//         }
        
//         // Setup pawns
//         for (int x = 0; x < 8; x++)
//         {
//             // Player pawns (row 1)
//             CreateChessPiece(ChessPieceType.Pawn, new Vector2Int(x, 1), true);
//             // Opponent pawns (row 6)
//             CreateChessPiece(ChessPieceType.Pawn, new Vector2Int(x, 6), false);
//         }
        
//         // Setup player back row (row 0)
//         CreateChessPiece(ChessPieceType.Rook, new Vector2Int(0, 0), true);
//         CreateChessPiece(ChessPieceType.Knight, new Vector2Int(1, 0), true);
//         CreateChessPiece(ChessPieceType.Bishop, new Vector2Int(2, 0), true);
//         CreateChessPiece(ChessPieceType.Queen, new Vector2Int(3, 0), true);
//         CreateChessPiece(ChessPieceType.King, new Vector2Int(4, 0), true);
//         CreateChessPiece(ChessPieceType.Bishop, new Vector2Int(5, 0), true);
//         CreateChessPiece(ChessPieceType.Knight, new Vector2Int(6, 0), true);
//         CreateChessPiece(ChessPieceType.Rook, new Vector2Int(7, 0), true);
        
//         // Setup opponent back row (row 7)
//         CreateChessPiece(ChessPieceType.Rook, new Vector2Int(0, 7), false);
//         CreateChessPiece(ChessPieceType.Knight, new Vector2Int(1, 7), false);
//         CreateChessPiece(ChessPieceType.Bishop, new Vector2Int(2, 7), false);
//         CreateChessPiece(ChessPieceType.Queen, new Vector2Int(3, 7), false);
//         CreateChessPiece(ChessPieceType.King, new Vector2Int(4, 7), false);
//         CreateChessPiece(ChessPieceType.Bishop, new Vector2Int(5, 7), false);
//         CreateChessPiece(ChessPieceType.Knight, new Vector2Int(6, 7), false);
//         CreateChessPiece(ChessPieceType.Rook, new Vector2Int(7, 7), false);
//     }
    
//     private ChessPiece CreateChessPiece(ChessPieceType type, Vector2Int position, bool isPlayer)
//     {
//         GameObject pieceObj = Instantiate(GetPiecePrefab(type), 
//             board.GetWorldPosition(position.x, position.y), 
//             Quaternion.identity);
            
//         ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
//         if (piece == null)
//             piece = pieceObj.AddComponent<ChessPiece>();
            
//         piece.Initialize(type, position, isPlayer);
        
//         chessPieces[position.x, position.y] = piece;
        
//         if (isPlayer)
//             playerPieces.Add(piece);
//         else
//             opponentPieces.Add(piece);
            
//         return piece;
//     }
    
//     public GameObject GetPiecePrefab(ChessPieceType type)
//     {
//         return type switch
//         {
//             ChessPieceType.Pawn => pawnPrefab,
//             ChessPieceType.Rook => rookPrefab,
//             ChessPieceType.Knight => knightPrefab,
//             ChessPieceType.Bishop => bishopPrefab,
//             ChessPieceType.Queen => queenPrefab,
//             ChessPieceType.King => kingPrefab,
//             _ => pawnPrefab
//         };
//     }
    
//     private void GenerateChessCards()
//     {
//         chessCardDeck.Clear();
        
//         // Generate multiple copies of each chess piece card
//         foreach (ChessPieceType type in System.Enum.GetValues(typeof(ChessPieceType)))
//         {
//             for (int i = 0; i < 4; i++) // 4 copies of each
//             {
//                 CardData card = CreateChessCardData(type);
//                 chessCardDeck.Add(card);
//             }
//         }
//     }
    
//     private CardData CreateChessCardData(ChessPieceType type)
//     {
//         CardData data = ScriptableObject.CreateInstance<CardData>();
//         data.cardName = $"{type} Card";
//         data.cardType = "Chess";
//         data.description = $"Move a {type} piece";
//         data.specialType = type.ToString();
        
//         return data;
//     }
    
//     private void InitializeMixedDeck()
//     {
//         mixedDeck = new List<CardData>(chessCardDeck);
//         // Add other game cards based on player selection
//     }
    
//     private void DrawInitialCards()
//     {
//         // Draw 5 cards for each player
//         for (int i = 0; i < 5; i++)
//         {
//             DrawCardFromDeck(true);
//             DrawCardFromDeck(false);
//         }
//     }
    
//     private void DrawCardFromDeck(bool isPlayer)
//     {
//         if (mixedDeck.Count == 0)
//         {
//             // Reshuffle discard pile if needed
//             return;
//         }
        
//         CardData card = mixedDeck[0];
//         mixedDeck.RemoveAt(0);
        
//         GameObject cardObj = CreateCard(card,
//             isPlayer ? playerHandArea : opponentHandArea,
//             isPlayer);
            
//         if (isPlayer)
//         {
//             playerHand.Add(cardObj);
//         }
//         else
//         {
//             opponentHand.Add(cardObj);
//         }
//     }
    
//     public void SelectChessPiece(ChessPiece piece)
//     {
//         if (actionLocked || !isPlayerTurn) return;
        
//         if (HasCardForPiece(piece.Type))
//         {
//             selectedPiece = piece;
//             selectedPosition = piece.Position;
//             ShowValidMoves(piece);
//         }
//     }
    
//     private bool HasCardForPiece(ChessPieceType pieceType)
//     {
//         return GetCardForPiece(pieceType) != null;
//     }
    
//     public GameObject GetCardForPiece(ChessPieceType pieceType)
//     {
//         foreach (GameObject cardObj in playerHand)
//         {
//             CardDisplay display = cardObj.GetComponent<CardDisplay>();
//             if (display != null && display.cardData != null)
//             {
//                 if (display.cardData.cardType == "Chess" && 
//                     GetPieceTypeFromCard(display.cardData) == pieceType)
//                 {
//                     return cardObj;
//                 }
//                 else if (display.cardData.cardType == "Uno" || 
//                          display.cardData.cardType == "RPG")
//                 {
//                     if (CanUseCardInChess(display.cardData))
//                         return cardObj;
//                 }
//             }
//         }
//         return null;
//     }
    
//     private GameObject GetNPCCardForPiece(ChessPieceType pieceType)
//     {
//         foreach (GameObject cardObj in opponentHand)
//         {
//             CardDisplay display = cardObj.GetComponent<CardDisplay>();
//             if (display != null && display.cardData != null)
//             {
//                 if (display.cardData.cardType == "Chess" && 
//                     GetPieceTypeFromCard(display.cardData) == pieceType)
//                 {
//                     return cardObj;
//                 }
//             }
//         }
//         return null;
//     }
    
//     private ChessPieceType GetPieceTypeFromCard(CardData card)
//     {
//         if (card.specialType == "Pawn") return ChessPieceType.Pawn;
//         if (card.specialType == "Rook") return ChessPieceType.Rook;
//         if (card.specialType == "Knight") return ChessPieceType.Knight;
//         if (card.specialType == "Bishop") return ChessPieceType.Bishop;
//         if (card.specialType == "Queen") return ChessPieceType.Queen;
//         if (card.specialType == "King") return ChessPieceType.King;
//         return ChessPieceType.Pawn;
//     }
    
//     private bool CanUseCardInChess(CardData card)
//     {
//         if (card.cardType == "Uno")
//         {
//             return card.cardValue == 14 || card.cardValue == 12 || card.cardValue == 13;
//         }
//         else if (card.cardType == "RPG")
//         {
//             string[] validRPG = { "Broken Sentinel", "Mage", "Zombie", "Draw", "Sword", "Shield", "Loot", "Spear", "Swap", "Teleport" };
//             return validRPG.Contains(card.cardName);
//         }
//         return false;
//     }
    
//     public void MoveSelectedPiece(Vector2Int targetPosition)
//     {
//         if (selectedPiece == null || actionLocked || !isPlayerTurn) return;
        
//         if (IsValidMove(selectedPiece, targetPosition))
//         {
//             GameObject cardToUse = GetCardForPiece(selectedPiece.Type);
//             if (cardToUse != null)
//             {
//                 // Check for capture
//                 ChessPiece capturedPiece = GetPieceAt(targetPosition);
//                 if (capturedPiece != null && capturedPiece.IsPlayer != selectedPiece.IsPlayer)
//                 {
//                     CapturePiece(capturedPiece);
//                 }
                
//                 // Move piece
//                 chessPieces[selectedPiece.Position.x, selectedPiece.Position.y] = null;
//                 selectedPiece.MoveTo(targetPosition);
//                 chessPieces[targetPosition.x, targetPosition.y] = selectedPiece;
                
//                 // Use card
//                 UseCard(cardToUse);
                
//                 // Check game state
//                 CheckGameState();
                
//                 EndTurn();
//             }
//         }
//     }
    
//     private void UseCard(GameObject card)
//     {
//         CardDisplay display = card.GetComponent<CardDisplay>();
//         if (display == null) return;
        
//         if (playerHand.Contains(card))
//         {
//             playerHand.Remove(card);
//         }
//         else if (opponentHand.Contains(card))
//         {
//             opponentHand.Remove(card);
//         }
        
//         MoveCardToDiscard(card);
        
//         // Check for inter-deck effects
//         if (display.cardData.cardType != "Chess")
//         {
//             ExecuteInterDeckEffect(display.cardData);
//         }
//     }
    
//     private void ExecuteInterDeckEffect(CardData card)
//     {
//         if (card.cardType == "Uno")
//         {
//             switch (card.cardValue)
//             {
//                 case 14: // Joker - Random chess card
//                     GameObject randomCard = GetRandomChessCard();
//                     if (randomCard != null)
//                     {
//                         playerHand.Add(randomCard);
//                     }
//                     break;
                    
//                 case 12: // Queen - Move queen anywhere
//                     EnableQueenMovement();
//                     break;
                    
//                 case 13: // King - Prevent check/checkmate
//                     PreventCheckNextTurn();
//                     break;
//             }
//         }
//         else if (card.cardType == "RPG")
//         {
//             // Implement RPG->Chess effects based on requirements
//         }
//     }
    
//     private GameObject GetRandomChessCard()
//     {
//         if (chessCardDeck.Count == 0) return null;
        
//         CardData randomCardData = chessCardDeck[Random.Range(0, chessCardDeck.Count)];
//         GameObject cardObj = CreateCard(randomCardData, playerHandArea, true);
//         return cardObj;
//     }
    
//     private void EnableQueenMovement()
//     {
//         queenMovementEnabled = true;
//         Debug.Log("Queen movement enabled - can move to any empty square!");
//     }
    
//     private void PreventCheckNextTurn()
//     {
//         immuneToCheck = true;
//         Debug.Log("Immune to check/checkmate next turn!");
//     }
    
//     private void CapturePiece(ChessPiece piece)
//     {
//         // Remove from board
//         chessPieces[piece.Position.x, piece.Position.y] = null;
        
//         // Remove from piece list
//         if (piece.IsPlayer)
//         {
//             playerPieces.Remove(piece);
//             // Player piece captured - draw a card
//             DrawCardForPlayer(true);
//         }
//         else
//         {
//             opponentPieces.Remove(piece);
//             // Opponent piece captured - opponent can cheat
//             if (canCheat)
//             {
//                 StartCoroutine(OpponentCheat());
//             }
//         }
        
//         // Check if king was captured
//         if (piece.Type == ChessPieceType.King)
//         {
//             IsKingKilledByCheating();
//         }
        
//         Destroy(piece.gameObject);
//     }
    
//     private IEnumerator OpponentCheat()
//     {
//         yield return new WaitForSeconds(1f); // Delay before cheating
        
//         if (playerHand.Count > 0)
//         {
//             int randomIndex = Random.Range(0, playerHand.Count);
//             GameObject stolenCard = playerHand[randomIndex];
//             playerHand.RemoveAt(randomIndex);
            
//             // Animate card moving to opponent
//             CardAnimate anim = stolenCard.GetComponent<CardAnimate>();
//             if (anim != null)
//             {
//                 anim.Animate(opponentHandArea.position, true, false);
//                 yield return new WaitForSeconds(0.5f);
//             }
            
//             opponentHand.Add(stolenCard);
//             Debug.Log("Opponent stole a card from you!");
//         }
//     }
    
//     public void DrawCardForPlayer(bool isPlayer)
//     {
//         if (isPlayer && playerHand.Count >= maxHandSize)
//         {
//             // Show replacement UI
//             ShowCardReplacementUI();
//             return;
//         }
        
//         DrawCardFromDeck(isPlayer);
//     }
    
//     private void CheckGameState()
//     {
//         CheckWinCondition();
//     }
    
//     public override void CheckWinCondition()
//     {
//         // Check for checkmate
//         bool playerInCheckmate = IsInCheckmate(true);
//         bool opponentInCheckmate = IsInCheckmate(false);
        
//         if (playerInCheckmate)
//         {
//             GameOver(false);
//         }
//         else if (opponentInCheckmate)
//         {
//             GameOver(true);
//         }
        
//         // Check for king killed by cheating
//         if (IsKingKilledByCheating())
//         {
//             bool playerKingAlive = FindPlayerKing() != null;
//             GameOver(playerKingAlive);
//         }
//     }
    
//     private bool IsKingKilledByCheating()
//     {
//         // Check if any king is missing
//         ChessPiece playerKing = FindPlayerKing();
//         ChessPiece opponentKing = FindOpponentKing();
        
//         return playerKing == null || opponentKing == null;
//     }
    
//     private ChessPiece FindPlayerKing()
//     {
//         foreach (ChessPiece piece in playerPieces)
//         {
//             if (piece.Type == ChessPieceType.King)
//                 return piece;
//         }
//         return null;
//     }
    
//     private ChessPiece FindOpponentKing()
//     {
//         foreach (ChessPiece piece in opponentPieces)
//         {
//             if (piece.Type == ChessPieceType.King)
//                 return piece;
//         }
//         return null;
//     }
    
//     private Vector2Int FindKingPosition(bool isPlayer)
//     {
//         ChessPiece king = isPlayer ? FindPlayerKing() : FindOpponentKing();
//         return king != null ? king.Position : new Vector2Int(-1, -1);
//     }
    
//     private bool IsKingInDanger(bool isPlayer)
//     {
//         Vector2Int kingPos = FindKingPosition(isPlayer);
//         if (kingPos.x == -1) return false;
        
//         // Check if any opponent piece can attack the king
//         List<ChessPiece> attackers = isPlayer ? opponentPieces : playerPieces;
//         foreach (ChessPiece attacker in attackers)
//         {
//             if (IsValidMove(attacker, kingPos))
//                 return true;
//         }
        
//         return false;
//     }
    
//     private bool IsInCheckmate(bool isPlayer)
//     {
//         if (!IsKingInDanger(isPlayer) || immuneToCheck) return false;
        
//         // Get the king
//         ChessPiece king = isPlayer ? FindPlayerKing() : FindOpponentKing();
//         if (king == null) return true;
        
//         // Check if king can move
//         List<Vector2Int> kingMoves = GetPossibleMoves(king);
//         foreach (Vector2Int move in kingMoves)
//         {
//             // Simulate move and check if still in check
//             ChessPiece temp = GetPieceAt(move);
//             Vector2Int originalPos = king.Position;
            
//             chessPieces[originalPos.x, originalPos.y] = null;
//             chessPieces[move.x, move.y] = king;
//             king.Position = move;
            
//             bool stillInCheck = IsKingInDanger(isPlayer);
            
//             // Restore position
//             chessPieces[originalPos.x, originalPos.y] = king;
//             chessPieces[move.x, move.y] = temp;
//             king.Position = originalPos;
            
//             if (!stillInCheck)
//                 return false;
//         }
        
//         // Check if any piece can block or capture attacker
//         List<ChessPiece> allies = isPlayer ? playerPieces : opponentPieces;
//         foreach (ChessPiece ally in allies)
//         {
//             if (ally.Type == ChessPieceType.King) continue;
            
//             List<Vector2Int> allyMoves = GetPossibleMoves(ally);
//             foreach (Vector2Int move in allyMoves)
//             {
//                 // Simulate move
//                 ChessPiece temp = GetPieceAt(move);
//                 Vector2Int originalPos = ally.Position;
                
//                 chessPieces[originalPos.x, originalPos.y] = null;
//                 chessPieces[move.x, move.y] = ally;
//                 ally.Position = move;
                
//                 bool stillInCheck = IsKingInDanger(isPlayer);
                
//                 // Restore
//                 chessPieces[originalPos.x, originalPos.y] = ally;
//                 chessPieces[move.x, move.y] = temp;
//                 ally.Position = originalPos;
                
//                 if (!stillInCheck)
//                     return false;
//             }
//         }
        
//         return true;
//     }
    
//     private List<Vector2Int> GetPossibleMoves(ChessPiece piece)
//     {
//         List<Vector2Int> moves = new List<Vector2Int>();
        
//         for (int x = 0; x < 8; x++)
//         {
//             for (int y = 0; y < 8; y++)
//             {
//                 Vector2Int target = new Vector2Int(x, y);
//                 if (IsValidMove(piece, target))
//                 {
//                     moves.Add(target);
//                 }
//             }
//         }
        
//         return moves;
//     }
    
//     private bool IsValidMove(ChessPiece piece, Vector2Int target)
//     {
//         if (target.x < 0 || target.x >= 8 || target.y < 0 || target.y >= 8)
//             return false;
        
//         // Check if target has own piece
//         ChessPiece targetPiece = GetPieceAt(target);
//         if (targetPiece != null && targetPiece.IsPlayer == piece.IsPlayer)
//             return false;
        
//         // Special queen movement if enabled
//         if (queenMovementEnabled && piece.Type == ChessPieceType.Queen && 
//             piece.IsPlayer && isPlayerTurn)
//         {
//             // Queen can move to any empty square
//             return targetPiece == null;
//         }
        
//         switch (piece.Type)
//         {
//             case ChessPieceType.Pawn:
//                 return IsValidPawnMove(piece, target);
//             case ChessPieceType.Rook:
//                 return IsValidRookMove(piece, target);
//             case ChessPieceType.Knight:
//                 return IsValidKnightMove(piece, target);
//             case ChessPieceType.Bishop:
//                 return IsValidBishopMove(piece, target);
//             case ChessPieceType.Queen:
//                 return IsValidQueenMove(piece, target);
//             case ChessPieceType.King:
//                 return IsValidKingMove(piece, target);
//             default:
//                 return false;
//         }
//     }
    
//     private bool IsValidPawnMove(ChessPiece pawn, Vector2Int target)
//     {
//         int direction = pawn.IsPlayer ? 1 : -1;
//         Vector2Int forward = pawn.Position + new Vector2Int(0, direction);
        
//         // Basic forward move
//         if (target == forward && GetPieceAt(target) == null)
//             return true;
            
//         // First double move
//         int startRow = pawn.IsPlayer ? 1 : 6;
//         if (!pawn.HasMoved && pawn.Position.y == startRow && 
//             target == pawn.Position + new Vector2Int(0, 2 * direction) && 
//             GetPieceAt(target) == null && GetPieceAt(forward) == null)
//             return true;
            
//         // Diagonal capture
//         Vector2Int leftDiag = pawn.Position + new Vector2Int(-1, direction);
//         Vector2Int rightDiag = pawn.Position + new Vector2Int(1, direction);
        
//         if ((target == leftDiag || target == rightDiag) && 
//             GetPieceAt(target) != null && GetPieceAt(target).IsPlayer != pawn.IsPlayer)
//             return true;
            
//         return false;
//     }
    
//     private bool IsValidRookMove(ChessPiece rook, Vector2Int target)
//     {
//         Vector2Int delta = target - rook.Position;
        
//         // Must move straight
//         if (delta.x != 0 && delta.y != 0)
//             return false;
        
//         // Check path
//         int stepX = Mathf.Clamp(delta.x, -1, 1);
//         int stepY = Mathf.Clamp(delta.y, -1, 1);
        
//         Vector2Int checkPos = rook.Position + new Vector2Int(stepX, stepY);
//         while (checkPos != target)
//         {
//             if (GetPieceAt(checkPos) != null)
//                 return false;
//             checkPos += new Vector2Int(stepX, stepY);
//         }
        
//         return true;
//     }
    
//     private bool IsValidKnightMove(ChessPiece knight, Vector2Int target)
//     {
//         Vector2Int delta = target - knight.Position;
//         int absX = Mathf.Abs(delta.x);
//         int absY = Mathf.Abs(delta.y);
        
//         // Knight moves in L shape: (2,1) or (1,2)
//         return (absX == 2 && absY == 1) || (absX == 1 && absY == 2);
//     }
    
//     private bool IsValidBishopMove(ChessPiece bishop, Vector2Int target)
//     {
//         Vector2Int delta = target - bishop.Position;
        
//         // Must move diagonally
//         if (Mathf.Abs(delta.x) != Mathf.Abs(delta.y))
//             return false;
        
//         // Check path
//         int stepX = delta.x > 0 ? 1 : -1;
//         int stepY = delta.y > 0 ? 1 : -1;
        
//         Vector2Int checkPos = bishop.Position + new Vector2Int(stepX, stepY);
//         while (checkPos != target)
//         {
//             if (GetPieceAt(checkPos) != null)
//                 return false;
//             checkPos += new Vector2Int(stepX, stepY);
//         }
        
//         return true;
//     }
    
//     private bool IsValidQueenMove(ChessPiece queen, Vector2Int target)
//     {
//         // Queen moves like rook or bishop
//         return IsValidRookMove(queen, target) || IsValidBishopMove(queen, target);
//     }
    
//     private bool IsValidKingMove(ChessPiece king, Vector2Int target)
//     {
//         Vector2Int delta = target - king.Position;
//         int absX = Mathf.Abs(delta.x);
//         int absY = Mathf.Abs(delta.y);
        
//         // King moves one square in any direction
//         return absX <= 1 && absY <= 1;
//     }
    
//     private ChessPiece GetPieceAt(Vector2Int position)
//     {
//         if (position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8)
//             return chessPieces[position.x, position.y];
//         return null;
//     }
    
//     private void ShowValidMoves(ChessPiece piece)
//     {
//         // Implementation would highlight squares on the board
//         List<Vector2Int> moves = GetPossibleMoves(piece);
//         board.HighlightSquares(moves);
//     }
    
//     public override void EndTurn()
//     {
//         if (preventCheckNextTurn)
//         {
//             immuneToCheck = true;
//             preventCheckNextTurn = false;
//         }
//         else
//         {
//             immuneToCheck = false;
//         }
        
//         queenMovementEnabled = false;
//         hasDrawnCardThisTurn = false;
//         selectedPiece = null;
//         isPlayerTurn = !isPlayerTurn;
        
//         if (!isPlayerTurn)
//         {
//             StartCoroutine(NPCTurn());
//         }
//     }
    
//     private IEnumerator NPCTurn()
//     {
//         yield return new WaitForSeconds(1f);
        
//         // NPC decision making
//         List<ChessMove> possibleMoves = new List<ChessMove>();
        
//         foreach (ChessPiece piece in opponentPieces)
//         {
//             List<Vector2Int> moves = GetPossibleMoves(piece);
//             foreach (Vector2Int move in moves)
//             {
//                 ChessMove chessMove = new ChessMove
//                 {
//                     piece = piece,
//                     targetPosition = move,
//                     value = CalculateMoveValue(piece, move)
//                 };
//                 possibleMoves.Add(chessMove);
//             }
//         }
        
//         // Sort by value
//         possibleMoves.Sort((a, b) => b.value.CompareTo(a.value));
        
//         if (possibleMoves.Count > 0)
//         {
//             // Weighted selection
//             float[] weights = { 0.55f, 0.25f, 0.15f, 0.05f };
//             float random = Random.value;
//             float cumulative = 0f;
            
//             ChessMove chosenMove = possibleMoves[0];
            
//             for (int i = 0; i < Mathf.Min(possibleMoves.Count, weights.Length); i++)
//             {
//                 cumulative += weights[i];
//                 if (random <= cumulative)
//                 {
//                     chosenMove = possibleMoves[i];
//                     break;
//                 }
//             }
            
//             // Check if NPC has card for this piece
//             GameObject npcCard = GetNPCCardForPiece(chosenMove.piece.Type);
//             if (npcCard != null)
//             {
//                 // Execute move
//                 ChessPiece capturedPiece = GetPieceAt(chosenMove.targetPosition);
//                 if (capturedPiece != null && capturedPiece.IsPlayer)
//                 {
//                     CapturePiece(capturedPiece);
//                 }
                
//                 // Move piece
//                 chessPieces[chosenMove.piece.Position.x, chosenMove.piece.Position.y] = null;
//                 chosenMove.piece.MoveTo(chosenMove.targetPosition);
//                 chessPieces[chosenMove.targetPosition.x, chosenMove.targetPosition.y] = chosenMove.piece;
                
//                 // Use card
//                 UseCard(npcCard);
                
//                 yield return new WaitForSeconds(1f);
//             }
//         }
//         else
//         {
//             // No moves available
//             DrawCardFromDeck(false);
//         }
        
//         EndTurn();
//     }
    
//     private float CalculateMoveValue(ChessPiece piece, Vector2Int target)
//     {
//         float value = 0f;
//         ChessPiece targetPiece = GetPieceAt(target);
        
//         if (targetPiece != null)
//         {
//             // Capturing is valuable
//             value += GetPieceValue(targetPiece.Type);
            
//             // Capturing player pieces is especially good (for cheating)
//             if (targetPiece.IsPlayer)
//                 value += 2f;
//         }
        
//         // Moving toward center is good
//         value += 1f - (Vector2.Distance(target, new Vector2(3.5f, 3.5f)) / 5f);
        
//         // Protecting king is important
//         if (IsKingInDanger(piece.IsPlayer))
//         {
//             Vector2Int kingPos = FindKingPosition(piece.IsPlayer);
//             if (Vector2.Distance(target, kingPos) < 2)
//                 value += 3f;
//         }
        
//         return value;
//     }
    
//     private float GetPieceValue(ChessPieceType type)
//     {
//         return type switch
//         {
//             ChessPieceType.Pawn => 1f,
//             ChessPieceType.Knight => 3f,
//             ChessPieceType.Bishop => 3f,
//             ChessPieceType.Rook => 5f,
//             ChessPieceType.Queen => 9f,
//             ChessPieceType.King => 100f,
//             _ => 0f
//         };
//     }
    
//     private void GameOver(bool playerWon)
//     {
//         isGameActive = false;
        
//         if (playerWon)
//         {
//             AwardPlayer();
//             if (actionManager != null)
//             {
//                 actionManager.ExecuteAction("ChessWin");
//             }
//         }
//         else
//         {
//             if (actionManager != null)
//             {
//                 actionManager.ExecuteAction("ChessLose");
//             }
//         }
//     }
    
//     private void ShowCardReplacementUI()
//     {
//         // Implementation for showing UI to replace a card
//         Debug.Log("Hand full! Need to replace a card.");
//     }
// }