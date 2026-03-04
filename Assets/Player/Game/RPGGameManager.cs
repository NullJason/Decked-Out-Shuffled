// // RPGGameManager.cs - Updated with complete item effects
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;

// public class RPGGameManager : BaseCardGameManager
// {
//     [Header("RPG Settings")]
//     public int playerTowerHP = 100;
//     public int opponentTowerHP = 100;
//     public int maxEntityCards = 10;
    
//     [Header("Game Areas")]
//     public Transform playerEntityArea;
//     public Transform opponentEntityArea;
//     public Transform playerItemArea;
//     public Transform opponentItemArea;
    
//     [Header("Cards")]
//     public List<RPGCardData> entityCardsData = new List<RPGCardData>();
//     public List<RPGCardData> itemCardsData = new List<RPGCardData>();
    
//     private List<GameObject> playerEntities = new List<GameObject>();
//     private List<GameObject> opponentEntities = new List<GameObject>();
//     private List<GameObject> playerItems = new List<GameObject>();
//     private List<GameObject> opponentItems = new List<GameObject>();
    
//     [Header("Buffs")]
//     private Dictionary<string, Buff> playerBuffs = new Dictionary<string, Buff>();
//     private Dictionary<string, Buff> opponentBuffs = new Dictionary<string, Buff>();
    
//     private RPGEntityCard selectedEntity;
//     private RPGItemCard selectedItem;
//     private bool extraTurn = false;
//     private int reconstructTurns = 0;
    
//     public override void StartGame()
//     {
//         // Initialize tower HP
//         playerTowerHP = 100;
//         opponentTowerHP = 100;
        
//         // Clear buffs
//         playerBuffs.Clear();
//         opponentBuffs.Clear();
        
//         // Draw starting cards
//         for (int i = 0; i < 3; i++)
//         {
//             DrawItemCard(true);
//         }
        
//         // NPC draws
//         for (int i = 0; i < 3; i++)
//         {
//             DrawItemCard(false);
//         }
//     }
    
//     private void ExecuteItemEffect(RPGCardData itemData, bool isPlayer)
//     {
//         switch (itemData.cardName)
//         {
//             case "Draw":
//                 DrawEntityCard(isPlayer);
//                 break;
                
//             case "Strategize":
//                 extraTurn = true;
//                 Debug.Log($"{GetPlayerName(isPlayer)} gains an extra turn!");
//                 break;
                
//             case "Sword":
//                 ApplyBuff(isPlayer, "Damage", 0.5f, 1);
//                 Debug.Log($"{GetPlayerName(isPlayer)} gains 50% damage bonus!");
//                 break;
                
//             case "Shield":
//                 ApplyShield(isPlayer, 1);
//                 Debug.Log($"{GetPlayerName(isPlayer)} gains 1 shield!");
//                 break;
                
//             case "Loot":
//                 StealItemCard(isPlayer);
//                 break;
                
//             case "Bless":
//                 HealAll(isPlayer, 0.3f);
//                 Debug.Log($"{GetPlayerName(isPlayer)} heals all entities by 30%!");
//                 break;
                
//             case "Cannon":
//                 RemoveShield(!isPlayer);
//                 Debug.Log($"{GetPlayerName(isPlayer)} removes all shields from opponent!");
//                 break;
                
//             case "Spear":
//                 ApplyBuff(isPlayer, "Spear", 0, 1);
//                 Debug.Log($"{GetPlayerName(isPlayer)} gains spear attack bonus!");
//                 break;
                
//             case "Swap":
//                 SwapCards(isPlayer);
//                 Debug.Log($"{GetPlayerName(isPlayer)} swaps cards with opponent!");
//                 break;
                
//             case "Teleport":
//                 ApplyBuff(isPlayer, "Teleport", 0, 1);
//                 Debug.Log($"{GetPlayerName(isPlayer)} gains teleport effect!");
//                 break;
                
//             case "Reconstruct":
//                 StartCoroutine(ReconstructTower(isPlayer));
//                 Debug.Log($"{GetPlayerName(isPlayer)} starts tower reconstruction!");
//                 break;
//         }
//     }
    
//     private string GetPlayerName(bool isPlayer)
//     {
//         return isPlayer ? "Player" : "Opponent";
//     }
    
//     private void ApplyBuff(bool isPlayer, string buffType, float value, int duration)
//     {
//         Dictionary<string, Buff> buffs = isPlayer ? playerBuffs : opponentBuffs;
//         buffs[buffType] = new Buff
//         {
//             type = buffType,
//             value = value,
//             duration = duration
//         };
//     }
    
//     private void ApplyShield(bool isPlayer, int amount)
//     {
//         List<GameObject> entities = isPlayer ? playerEntities : opponentEntities;
        
//         foreach (GameObject entityObj in entities)
//         {
//             RPGEntityCard entity = entityObj.GetComponent<RPGEntityCard>();
//             if (entity != null)
//             {
//                 entity.CurrentShield += amount;
//             }
//         }
//     }
    
//     private void StealItemCard(bool isPlayer)
//     {
//         List<GameObject> sourceItems = isPlayer ? opponentItems : playerItems;
//         List<GameObject> targetItems = isPlayer ? playerItems : opponentItems;
        
//         if (sourceItems.Count == 0)
//         {
//             Debug.Log("No items to steal!");
//             return;
//         }
        
//         GameObject stolenCard = sourceItems[Random.Range(0, sourceItems.Count)];
//         sourceItems.Remove(stolenCard);
//         targetItems.Add(stolenCard);
        
//         // Animate card movement
//         CardAnimate anim = stolenCard.GetComponent<CardAnimate>();
//         if (anim != null)
//         {
//             Transform targetArea = isPlayer ? playerItemArea : opponentItemArea;
//             anim.Animate(targetArea.position, true, false);
//         }
        
//         Debug.Log($"{GetPlayerName(isPlayer)} stole an item card!");
//     }
    
//     private void HealAll(bool isPlayer, float percentage)
//     {
//         List<GameObject> entities = isPlayer ? playerEntities : opponentEntities;
        
//         foreach (GameObject entityObj in entities)
//         {
//             RPGEntityCard entity = entityObj.GetComponent<RPGEntityCard>();
//             if (entity != null)
//             {
//                 int healAmount = Mathf.RoundToInt(entity.cardData.baseHP * percentage);
//                 entity.CurrentHP = Mathf.Min(entity.CurrentHP + healAmount, entity.cardData.baseHP);
//             }
//         }
//     }
    
//     private void RemoveShield(bool isOpponent)
//     {
//         List<GameObject> entities = isOpponent ? opponentEntities : playerEntities;
        
//         if (entities.Count == 0)
//         {
//             Debug.Log("No entities to remove shield from!");
//             return;
//         }
        
//         // In a real implementation, you would show UI to select which entity
//         // For now, remove from first entity
//         if (entities.Count > 0)
//         {
//             RPGEntityCard entity = entities[0].GetComponent<RPGEntityCard>();
//             if (entity != null)
//             {
//                 entity.CurrentShield = 0;
//                 Debug.Log($"Removed all shields from {entity.cardData.cardName}!");
//             }
//         }
//     }
    
//     private void SwapCards(bool isPlayer)
//     {
//         // Swap entities
//         List<GameObject> tempEntities = new List<GameObject>(playerEntities);
//         playerEntities = new List<GameObject>(opponentEntities);
//         opponentEntities = tempEntities;
        
//         // Swap items
//         List<GameObject> tempItems = new List<GameObject>(playerItems);
//         playerItems = new List<GameObject>(opponentItems);
//         opponentItems = tempItems;
        
//         // Reposition all cards
//         StartCoroutine(RepositionAllCards());
//     }
    
//     private IEnumerator RepositionAllCards()
//     {
//         RepositionEntities(true);
//         RepositionEntities(false);
//         RepositionItems(true);
//         RepositionItems(false);
//         yield return null;
//     }
    
//     private IEnumerator ReconstructTower(bool isPlayer)
//     {
//         reconstructTurns = 3;
        
//         while (reconstructTurns > 0)
//         {
//             yield return new WaitUntil(() => isPlayerTurn == isPlayer);
            
//             if (isPlayer)
//             {
//                 playerTowerHP += 5;
//                 playerTowerHP = Mathf.Min(playerTowerHP, 100);
//             }
//             else
//             {
//                 opponentTowerHP += 5;
//                 opponentTowerHP = Mathf.Min(opponentTowerHP, 100);
//             }
            
//             UpdateTowerHP();
//             reconstructTurns--;
            
//             if (reconstructTurns <= 0) break;
            
//             yield return new WaitUntil(() => isPlayerTurn != isPlayer);
//         }
//     }
    
//     public void RepositionEntities(bool isPlayer)
//     {
//         List<GameObject> entities = isPlayer ? playerEntities : opponentEntities;
//         Transform area = isPlayer ? playerEntityArea : opponentEntityArea;
        
//         float spacing = 2f;
//         for (int i = 0; i < entities.Count; i++)
//         {
//             Vector3 targetPos = area.position + new Vector3(
//                 i * spacing - (entities.Count * spacing / 2),
//                 0,
//                 0
//             );
            
//             CardAnimate anim = entities[i].GetComponent<CardAnimate>();
//             if (anim != null)
//             {
//                 anim.Animate(targetPos, true, false);
//             }
//             else
//             {
//                 entities[i].transform.position = targetPos;
//             }
//         }
//     }
    
//     public void RepositionItems(bool isPlayer)
//     {
//         List<GameObject> items = isPlayer ? playerItems : opponentItems;
//         Transform area = isPlayer ? playerItemArea : opponentItemArea;
        
//         float spacing = 1.5f;
//         float yOffset = -1f; // Position below entities
        
//         for (int i = 0; i < items.Count; i++)
//         {
//             Vector3 targetPos = area.position + new Vector3(
//                 i * spacing - (items.Count * spacing / 2),
//                 yOffset,
//                 0
//             );
            
//             CardAnimate anim = items[i].GetComponent<CardAnimate>();
//             if (anim != null)
//             {
//                 anim.Animate(targetPos, true, false);
//             }
//             else
//             {
//                 items[i].transform.position = targetPos;
//             }
//         }
//     }
    
//     private void DrawEntityCard(bool isPlayer)
//     {
//         if (entityCardsData.Count == 0) return;
        
//         RPGCardData randomEntity = entityCardsData[Random.Range(0, entityCardsData.Count)];
//         GameObject entityObj = CreateEntityCard(randomEntity, isPlayer);
        
//         if (isPlayer)
//         {
//             playerEntities.Add(entityObj);
//             RepositionEntities(true);
//         }
//         else
//         {
//             opponentEntities.Add(entityObj);
//             RepositionEntities(false);
//         }
//     }
    
//     private void DrawItemCard(bool isPlayer)
//     {
//         if (itemCardsData.Count == 0) return;
        
//         RPGCardData randomItem = itemCardsData[Random.Range(0, itemCardsData.Count)];
//         GameObject itemObj = CreateItemCard(randomItem, isPlayer);
        
//         if (isPlayer)
//         {
//             playerItems.Add(itemObj);
//             RepositionItems(true);
//         }
//         else
//         {
//             opponentItems.Add(itemObj);
//             RepositionItems(false);
//         }
//     }
    
//     private GameObject CreateEntityCard(RPGCardData data, bool isPlayer)
//     {
//         GameObject cardObj = Instantiate(cardPrefab, 
//             isPlayer ? playerEntityArea : opponentEntityArea,
//             false);
            
//         RPGEntityCard entityCard = cardObj.AddComponent<RPGEntityCard>();
//         entityCard.Initialize(data, isPlayer);
        
//         return cardObj;
//     }
    
//     private GameObject CreateItemCard(RPGCardData data, bool isPlayer)
//     {
//         GameObject cardObj = Instantiate(cardPrefab,
//             isPlayer ? playerItemArea : opponentItemArea,
//             false);
            
//         RPGItemCard itemCard = cardObj.AddComponent<RPGItemCard>();
//         itemCard.Initialize(data, isPlayer);
        
//         return cardObj;
//     }
    
//     private float CalculateTowerAttackValue(RPGEntityCard entity)
//     {
//         float value = entity.CurrentAttack;
        
//         // Apply damage bonus buff if active
//         if (entity.IsPlayer && playerBuffs.ContainsKey("Damage"))
//         {
//             value *= (1 + playerBuffs["Damage"].value);
//         }
//         else if (!entity.IsPlayer && opponentBuffs.ContainsKey("Damage"))
//         {
//             value *= (1 + opponentBuffs["Damage"].value);
//         }
        
//         // Tower attacks are valuable
//         return value * 2f;
//     }
    
//     private float CalculateItemValue(RPGItemCard item)
//     {
//         float value = 0f;
        
//         switch (item.cardData.cardName)
//         {
//             case "Draw":
//                 value = 8f; // High value for card advantage
//                 break;
//             case "Strategize":
//                 value = 10f; // Extra turn is very valuable
//                 break;
//             case "Sword":
//                 value = 6f; // Damage boost
//                 break;
//             case "Shield":
//                 value = 5f; // Defense
//                 break;
//             case "Loot":
//                 value = 7f; // Card advantage
//                 break;
//             case "Bless":
//                 value = 8f; // Healing
//                 break;
//             case "Cannon":
//                 value = 6f; // Shield removal
//                 break;
//             case "Spear":
//                 value = 7f; // Area damage
//                 break;
//             case "Swap":
//                 value = 9f; // Can turn the tide
//                 break;
//             case "Teleport":
//                 value = 8f; // Direct tower damage
//                 break;
//             case "Reconstruct":
//                 value = 7f; // Sustained healing
//                 break;
//         }
        
//         // Adjust based on game state
//         if (item.IsPlayer)
//         {
//             if (playerTowerHP < 50) value += 2f;
//             if (playerEntities.Count == 0) value += 3f;
//         }
//         else
//         {
//             if (opponentTowerHP < 50) value += 2f;
//             if (opponentEntities.Count == 0) value += 3f;
//         }
        
//         return value;
//     }
    
//     private void UpdateTowerHP()
//     {
//         // Update UI for tower HP
//         Debug.Log($"Player Tower: {playerTowerHP}, Opponent Tower: {opponentTowerHP}");
//     }
    
//     public override void EndTurn()
//     {
//         if (extraTurn)
//         {
//             extraTurn = false;
//             Debug.Log("Extra turn! Same player goes again.");
//             return;
//         }
        
//         // Update buff durations
//         UpdateBuffs(playerBuffs);
//         UpdateBuffs(opponentBuffs);
        
//         isPlayerTurn = !isPlayerTurn;
        
//         if (!isPlayerTurn)
//         {
//             StartCoroutine(NPCTurn());
//         }
//     }
    
//     private void UpdateBuffs(Dictionary<string, Buff> buffs)
//     {
//         List<string> toRemove = new List<string>();
        
//         foreach (var kvp in buffs)
//         {
//             kvp.Value.duration--;
//             if (kvp.Value.duration <= 0)
//             {
//                 toRemove.Add(kvp.Key);
//             }
//         }
        
//         foreach (string key in toRemove)
//         {
//             buffs.Remove(key);
//         }
//     }
    
//     private IEnumerator NPCTurn()
//     {
//         yield return new WaitForSeconds(1f);
        
//         // NPC decision making
//         List<NPCDecision> decisions = new List<NPCDecision>();
        
//         // Evaluate attacks
//         foreach (GameObject entityObj in opponentEntities)
//         {
//             RPGEntityCard entity = entityObj.GetComponent<RPGEntityCard>();
//             if (entity != null && entity.CanAttack)
//             {
//                 // Attack player entities
//                 foreach (GameObject targetObj in playerEntities)
//                 {
//                     RPGEntityCard target = targetObj.GetComponent<RPGEntityCard>();
//                     if (target != null)
//                     {
//                         NPCDecision decision = new NPCDecision
//                         {
//                             type = DecisionType.Attack,
//                             attacker = entity,
//                             target = target,
//                             value = CalculateAttackValue(entity, target)
//                         };
//                         decisions.Add(decision);
//                     }
//                 }
                
//                 // Attack tower if no entities
//                 if (playerEntities.Count == 0)
//                 {
//                     NPCDecision decision = new NPCDecision
//                     {
//                         type = DecisionType.AttackTower,
//                         attacker = entity,
//                         value = CalculateTowerAttackValue(entity)
//                     };
//                     decisions.Add(decision);
//                 }
//             }
//         }
        
//         // Evaluate item usage
//         foreach (GameObject itemObj in opponentItems)
//         {
//             RPGItemCard item = itemObj.GetComponent<RPGItemCard>();
//             if (item != null)
//             {
//                 NPCDecision decision = new NPCDecision
//                 {
//                     type = DecisionType.UseItem,
//                     item = item,
//                     value = CalculateItemValue(item)
//                 };
//                 decisions.Add(decision);
//             }
//         }
        
//         // Evaluate discarding
//         if (opponentEntities.Count > 0 && opponentEntities.Count + opponentItems.Count > 5)
//         {
//             NPCDecision decision = new NPCDecision
//             {
//                 type = DecisionType.Discard,
//                 value = -3f
//             };
//             decisions.Add(decision);
//         }
        
//         // Sort by value and use weighted random
//         decisions.Sort((a, b) => b.value.CompareTo(a.value));
        
//         if (decisions.Count > 0)
//         {
//             float[] weights = { 0.55f, 0.25f, 0.15f, 0.05f };
//             float random = Random.value;
//             float cumulative = 0f;
            
//             NPCDecision chosenDecision = decisions[0];
            
//             for (int i = 0; i < Mathf.Min(decisions.Count, weights.Length); i++)
//             {
//                 cumulative += weights[i];
//                 if (random <= cumulative)
//                 {
//                     chosenDecision = decisions[i];
//                     break;
//                 }
//             }
            
//             yield return StartCoroutine(ExecuteNPCDecision(chosenDecision));
//         }
        
//         EndTurn();
//     }
    
//     private float CalculateAttackValue(RPGEntityCard attacker, RPGEntityCard target)
//     {
//         float value = attacker.CurrentAttack;
        
//         // Bonus for killing blow
//         if (attacker.CurrentAttack >= target.CurrentHP)
//             value += 5f;
            
//         // Bonus for high-value target
//         value += target.cardData.baseAttack * 0.5f;
        
//         // Penalty if target has shield
//         if (target.CurrentShield > 0)
//             value -= 2f;
            
//         return value;
//     }
    
//     private IEnumerator ExecuteNPCDecision(NPCDecision decision)
//     {
//         switch (decision.type)
//         {
//             case DecisionType.Attack:
//                 decision.attacker.Attack(decision.target);
//                 yield return new WaitForSeconds(1f);
//                 break;
                
//             case DecisionType.AttackTower:
//                 int damage = decision.attacker.CurrentAttack;
                
//                 // Apply buffs
//                 if (opponentBuffs.ContainsKey("Damage"))
//                 {
//                     damage = Mathf.RoundToInt(damage * (1 + opponentBuffs["Damage"].value));
//                 }
                
//                 playerTowerHP -= damage;
//                 UpdateTowerHP();
//                 Debug.Log($"Opponent attacks tower for {damage} damage!");
//                 yield return new WaitForSeconds(0.5f);
//                 break;
                
//             case DecisionType.UseItem:
//                 ExecuteItemEffect(decision.item.cardData, false);
//                 opponentItems.Remove(decision.item.gameObject);
//                 Destroy(decision.item.gameObject);
//                 RepositionItems(false);
//                 yield return new WaitForSeconds(0.5f);
//                 break;
                
//             case DecisionType.Discard:
//                 if (opponentEntities.Count > 0)
//                 {
//                     GameObject toDiscard = opponentEntities[Random.Range(0, opponentEntities.Count)];
//                     opponentEntities.Remove(toDiscard);
//                     MoveCardToDiscard(toDiscard);
//                     RepositionEntities(false);
//                 }
//                 yield return new WaitForSeconds(0.5f);
//                 break;
//         }
//     }
    
//     public override void CheckWinCondition()
//     {
//         if (playerTowerHP <= 0)
//         {
//             GameOver(false);
//         }
//         else if (opponentTowerHP <= 0)
//         {
//             GameOver(true);
//         }
//     }
    
//     private void GameOver(bool playerWon)
//     {
//         isGameActive = false;
        
//         if (playerWon)
//         {
//             AwardPlayer();
//             if (actionManager != null)
//             {
//                 actionManager.ExecuteAction("RPGWin");
//             }
//         }
//         else
//         {
//             if (actionManager != null)
//             {
//                 actionManager.ExecuteAction("RPGLose");
//             }
//         }
//     }
// }

// // Buff.cs
// [System.Serializable]
// public class Buff
// {
//     public string type;
//     public float value;
//     public int duration;
// }