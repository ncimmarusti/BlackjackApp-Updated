using UnityEngine;
using UnityEngine.UI;  
using System.Collections.Generic;
using System;  

public class CardDealer : MonoBehaviour
{
    public GameObject cardPrefab;  // Reference the prefab
    public Transform playerHandTransform;  // Player's first hand area
    public Transform dealerHandTransform;  // Dealer's hand area

    public Text runningCountText;  // UI Text for running count 
    public Text trueCountText;     // UI Text for true count
    public Text strategySuggestionText;  // Reference to the UI Text for strategy suggestions
    public Text decksRemainingText;  // UI Text for displaying decks remaining
    public Text accuracyText;        // UI Text for displaying player's accuracy
    public Text bankrollText;         // UI Text for displaying bankroll
    public Text currentBetText;       // UI Text for displaying current bet
    public Text notificationText; // For regular Text UI
    public InputField bankrollInput;  // Input field for setting initial bankroll


    public Button nextRoundButton;  // Reference to the Next Round button
    public Button splitButton;      // Reference to the Split button
    public Button dealButton;         // Button to deal cards after placing a bet
    public Button bet5Button;         // Button for $5 bet
    public Button bet10Button;        // Button for $10 bet
    public Button bet25Button;        // Button for $25 bet
    public Button bet100Button;       // Button for $100 bet
    public Button countToggleButton; // Reference to the toggle button
    public Button resetBetButton; // Reference to the Reset Bet button



    private int runningCount = 0;
    private float cardSpacing = 60f;  // Adjust this value to make cards in each hand overlap slightly
    private float handSpacing = 350f;  // Increase or Decrease this value to create more space between Hand 1 and Hand 2
    private int dealerCardCount = 0;  // Track how many cards the dealer has
    private int dealerTotal = 0;  // Store the dealer's total card value
    private int bankroll = 1000;      // Default bankroll value
    private int currentBet = 0;  // Current bet amount
    private string dealerHiddenCardName;  // Store the name of the dealer's hidden card

    private List<string> deck;
    private bool roundActive = false;  // Flag to manage if the round is ongoing
    private bool playerTurnOver = false;  // Ensure the player�s turn finishes before dealer plays
    private bool canPlaceBet = true;  // Flag to manage betting state
    private bool isCountVisible = true; // Track visibility state



    // Track player hands dynamically
    private List<List<GameObject>> playerHands = new List<List<GameObject>>();  // List of hands, each hand is a list of cards
    private List<Transform> playerHandTransforms = new List<Transform>();  // Track all player hand transforms
    private int currentHandIndex = 0;  // Track which hand the player is currently playing
    private int maxSplits = 3;  // Limit player to 3 splits, resulting in 4 total hands



    public Button startGameButton;  // Reference to the "Start Game" button

    // This method is called when the "Start Game" button is clicked
    public void OnStartGameButtonClick()
    {
        // Set bankroll from input field or use default
        if (!string.IsNullOrEmpty(bankrollInput.text))
        {
            if (int.TryParse(bankrollInput.text, out int initialBankroll))
            {
                bankroll = initialBankroll;
            }
            else
            {
                Debug.LogError("Invalid bankroll input. Using default value.");
            }
        }

        UpdateBankrollUI();

        if (selectedDeckCount > 0)  // Ensure a valid deck count is selected
        {
            UnityEngine.Debug.Log("Starting game with " + selectedDeckCount + " decks.");
            InitializeDeck();  // Initialize the deck with the selected number of decks
        }
        else
        {
            UnityEngine.Debug.LogError("Please select the number of decks before starting the game.");
        }
    }

    public Dropdown deckDropdown;  // Reference to the dropdown in the Unity UI
    private int selectedDeckCount = 1;  // Default to 1 deck if nothing is selected

    // This method will be called when the dropdown value changes
    public void OnDeckSelectionChange()
    {
        int selectedIndex = deckDropdown.value;  // Get the selected option's index
        string selectedOption = deckDropdown.options[selectedIndex].text;

        // Set the selectedDeckCount based on the option selected
        switch (selectedOption)
        {
            case "1 Deck":
                selectedDeckCount = 1;
                break;
            case "2 Decks":
                selectedDeckCount = 2;
                break;
            case "6 Decks":
                selectedDeckCount = 6;
                break;
            case "8 Decks":
                selectedDeckCount = 8;
                break;
            default:
                selectedDeckCount = 1;  // Default to 1 deck if something goes wrong
                break;
        }

        UnityEngine.Debug.Log("Player selected " + selectedDeckCount + " decks.");
    }

    void Start()
    {
        UpdateBankrollUI();
        UpdateCurrentBetUI();
        nextRoundButton.gameObject.SetActive(false); // Hide the "Next Round" button
        splitButton.interactable = false; // Hide the "Split" button initially

        // Setup bet button listeners
        bet5Button.onClick.AddListener(() => PlaceBet(5));
        bet10Button.onClick.AddListener(() => PlaceBet(10));
        bet25Button.onClick.AddListener(() => PlaceBet(25));
        bet100Button.onClick.AddListener(() => PlaceBet(100));
        dealButton.onClick.AddListener(OnDealButtonClick);

        // Set up the Reset Bet button
        resetBetButton.onClick.AddListener(ResetCurrentBet);
        resetBetButton.interactable = true; // Enable it initially

        dealButton.interactable = false;  // Disable Deal button initially

        countToggleButton.onClick.AddListener(ToggleCountVisibility);
        UpdateCountVisibility(); // Initialize visibility

    }

    private void UpdateBankrollUI()
    {
        bankrollText.text = "Bankroll: $" + bankroll;
    }

    private void PlaceBet(int amount)
    {
        if (canPlaceBet && bankroll >= amount)
        {
            currentBet += amount;
            bankroll -= amount;
            UpdateBankrollUI();
            UpdateCurrentBetUI();

            dealButton.interactable = true;  // Enable Deal button once a bet is placed

            // Ensure the bet for the first hand reflects the cumulative total
            if (handBets.Count == 0)
            {
                handBets.Add(currentBet);  // Initialize with the first bet amount
            }
            else
            {
                handBets[0] = currentBet;  // Update the first hand bet to the cumulative amount
            }
        }
        else if (bankroll < amount)
        {
            Debug.LogError("Insufficient bankroll to place this bet.");
        }
    }

    private void OnDealButtonClick()
    {
        if (currentBet > 0)
        {
            canPlaceBet = false;  // Disable further betting after dealing
            dealButton.interactable = false;  // Disable Deal button during the round
            resetBetButton.interactable = false; // Disable Reset Bet button after dealing
            StartNewRound();  // Deal cards and start the round
        }
        else
        {
            Debug.LogError("Place a bet before dealing.");
        }
    }

    private void ResetBet()
    {
        currentBet = 0;
        handBets.Clear();  // Clear the list of bets for the new round 
        UpdateCurrentBetUI();
    }

    private void UpdateCurrentBetUI()
    {
        currentBetText.text = "Current Bet: $" + currentBet;
    }

    private void StartNewRound()
    {
        UnityEngine.Debug.Log("Starting a new round...");

        // Reset round state
        dealerCardCount = 0;
        dealerTotal = 0;
        playerTurnOver = false; // Reset player turn flag
        currentHandIndex = 0;  // Start with the first hand

        // Clear the player's and dealer's hands
        ClearHands();

        // Only reshuffle the deck if needed, not every time
        CheckAndReshuffleDeck();

        // Deal new cards for the next round
        DealInitialCards();

        // Check if player has blackjack
        bool playerHasBlackjack = CheckPlayerBlackjack();
        bool dealerHasBlackjack = CheckDealerBlackjack();

        if (playerHasBlackjack && dealerHasBlackjack)
        {
            // Both player and dealer have blackjack; it's a tie
            HandleTieOnBlackjack();
        }
        else if (playerHasBlackjack)
        {
            // Player has blackjack and wins 3:2
            HandlePlayerBlackjack();
        }
        else if (dealerHasBlackjack)
        {
            // Dealer has blackjack and wins
            HandleDealerBlackjack();
        }
        else
        {
            // No blackjack; proceed with the round as normal
            UnityEngine.Debug.Log("No dealer or player blackjack. Continue with player actions.");
            roundActive = true;
            nextRoundButton.gameObject.SetActive(false);

            // Enable the split button if splitting is possible
            splitButton.interactable = CanSplit();
        }
    }

    // Function to check if the dealer has blackjack
    private bool CheckDealerBlackjack()
    {
        if (dealerHandTransform.childCount >= 2)
        {
            string firstCardName = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;
            string secondCardName = dealerHiddenCardName;  

            int firstCardValue = GetCardValue(firstCardName);
            int secondCardValue = GetCardValue(secondCardName);

            return (firstCardValue == 11 && secondCardValue == 10) || (firstCardValue == 10 && secondCardValue == 11);
        }
        return false;
    }

    // Function to handle dealer blackjack scenario
    private void HandleDealerBlackjack()
    {
        RevealDealerCard();  // Reveal the hidden dealer card
        int playerTotal = CalculateHandTotal(playerHands[0]);  // Assuming only one hand initially

        // Check if the player also has blackjack
        if (playerTotal == 21)
        {
            UnityEngine.Debug.Log("Both dealer and player have blackjack. It's a tie.");
            notificationText.text = "Both dealer and player have blackjack. It's a tie.";
            bankroll += currentBet;  // Return the bet to the player
        }
        else
        {
            UnityEngine.Debug.Log("Dealer has blackjack. Player loses.");
            notificationText.text = "Dealer has blackjack. Player loses.";
        }

        UpdateBankrollUI();  // Update the UI with the latest bankroll
        EndRound("Dealer has blackjack.");
    }

    private bool CheckPlayerBlackjack()
    {
        // Ensure player has exactly 2 cards
        if (playerHands[0].Count == 2)
        {
            int playerTotal = CalculateHandTotal(playerHands[0]);
            return playerTotal == 21;
        }
        return false;
    }

    private void HandlePlayerBlackjack()
    {
        // Payout is 1.5 times the current bet
        int payout = (int)(currentBet * 1.5f);
        bankroll += currentBet + payout;  // Return the bet plus payout
        UnityEngine.Debug.Log("Player has blackjack! Payout: $" + payout);
        notificationText.text = $"Player has blackjack! Payout: ${payout}";

        UpdateBankrollUI();
        EndRound("Player has blackjack! Round over.");
    }

    private void HandleTieOnBlackjack()
    {
        // Return the player's bet as it's a tie
        bankroll += currentBet;  // Return the initial bet without any additional payout
        UnityEngine.Debug.Log("Both dealer and player have blackjack. It's a tie.");
        notificationText.text = "Both dealer and player have blackjack. Round over.";

        UpdateBankrollUI();
        EndRound("Both dealer and player have blackjack. Round over.");
    }

    // Function to clear all hands
    private void ClearHands()
    {
        foreach (Transform handTransform in playerHandTransforms)
        {
            foreach (Transform child in handTransform)
            {
                Destroy(child.gameObject);  // Destroy each card in each hand
            }
        }

        // Clear the hands and transforms
        playerHands.Clear();
        playerHandTransforms.Clear();

        // Clear dealer's hand
        foreach (Transform child in dealerHandTransform)
        {
            Destroy(child.gameObject);
        }

        

        // Reset the first hand to use the original playerHandTransform
        playerHandTransforms.Add(playerHandTransform);  // Use the original player hand transform for the first hand
        playerHands.Add(new List<GameObject>());  // Create a new list to store cards for this hand
    }

    // Function to deal initial two cards to player and dealer
    private void DealInitialCards()
    {
        // Deal two cards to the player
        for (int i = 0; i < 2; i++)
        {
            DealCardToPlayer(currentHandIndex);  // Deal cards to the first hand
        }

        // Deal two cards to the dealer
        for (int i = 0; i < 2; i++)
        {
            DealCardToDealer();
        }
    }

    // Create a new hand for the player (split logic)
    private void CreateNewPlayerHand()
    {
        // Create a new empty hand transform as a child of playerHandTransform
        GameObject handGO = new GameObject("PlayerHand" + playerHands.Count);
        handGO.transform.SetParent(playerHandTransform);  // Make it a child of the existing playerHandTransform

        // Use increased handSpacing to create more space between hands
        RectTransform handRect = handGO.AddComponent<RectTransform>();
        handRect.localPosition = new Vector2(playerHands.Count * handSpacing, 0);  // Spread out hands with more space
        handRect.sizeDelta = new Vector2(0, 0);  // It doesn't need a size since it's just a container

        // Add the new hand to the list
        playerHands.Add(new List<GameObject>());
        playerHandTransforms.Add(handGO.transform);
    }

    // Function to deal a card to the specified hand (by index)
    private void DealCardToPlayer(int handIndex)
    {
        if (deck.Count > 0)
        {
            string dealtCard = DealCard();
            GameObject cardGO = Instantiate(cardPrefab, playerHandTransforms[handIndex]); // Instantiate the new "Card" prefab for the player

            // Adjust position relative to the specific hand's transform
            RectTransform cardRectTransform = cardGO.GetComponent<RectTransform>();
            int cardIndex = playerHands[handIndex].Count;
            cardRectTransform.localPosition = new Vector2(cardIndex * cardSpacing, 0);  // Reduce the spacing between cards

            // Ensure the card has the correct size
            cardRectTransform.sizeDelta = new Vector2(100f, 150f);  // Adjust the size of the card (if necessary)

            SetCardImage(cardGO, dealtCard);
            UpdateRunningCount(dealtCard);

            // Add the card to the specified hand's list
            playerHands[handIndex].Add(cardGO);

            UpdateDecksRemainingUI();  // Update the decks remaining after dealing
        }
    }

    public void DealCardToDealer()
    {
        if (deck.Count > 0)
        {
            string dealtCard = DealCard();
            GameObject cardGO = Instantiate(cardPrefab, dealerHandTransform); // Instantiate the new "Card" prefab for the dealer

            // Adjust position relative to dealerHandTransform's position
            RectTransform cardRectTransform = cardGO.GetComponent<RectTransform>();
            cardRectTransform.localPosition = new Vector2(dealerCardCount * cardSpacing, 0);  // Spread cards horizontally

            // Ensure the card has the correct size
            cardRectTransform.sizeDelta = new Vector2(100f, 150f);  // Adjust the size of the card (if necessary)

            // If it's the dealer's second card, hide it (use the card-back sprite)
            if (dealerCardCount == 1)  // Dealer's second card
            {
                dealerHiddenCardName = dealtCard;  // Store the hidden card name
                SetCardBackImage(cardGO);  // Hide the second card
            }
            else
            {
                SetCardImage(cardGO, dealtCard);
            }

            UpdateRunningCount(dealtCard);
            dealerCardCount++;  // Increment dealer card count to space the next card

            UpdateDecksRemainingUI();  // Update the decks remaining after dealing
        }
    }

    private List<int> handBets = new List<int>();  // Track the bet for each hand

    // Function to handle splitting
    public void PlayerSplit()
    {
        if (roundActive && CanSplit() && playerHands.Count <= maxSplits)
        {
            if (bankroll >= currentBet)
            {
                // Deduct the bet for the new hand from the bankroll
                bankroll -= currentBet;
                UpdateBankrollUI();

                // Track the bet for the new hand
                handBets.Add(currentBet);  // Add bet for the new hand

                // Log to verify synchronization
                UnityEngine.Debug.Log($"After split: playerHands count = {playerHands.Count}, handBets count = {handBets.Count}");

                // Existing split logic
                string dealerUpCard = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;
                string recommendedAction = BasicStrategyChecker(playerHands[currentHandIndex], dealerUpCard);
                TrackPlayerAccuracy("Split", recommendedAction);

                strategySuggestionText.text = recommendedAction != "Split" ?
                    $"You chose to Split, but the recommended action was: {recommendedAction}" :
                    "You followed the basic strategy by splitting.";

                splitButton.interactable = CanSplit();
                GameObject splitCard = playerHands[currentHandIndex][1];
                playerHands[currentHandIndex].RemoveAt(1);

                CreateNewPlayerHand();
                splitCard.transform.SetParent(playerHandTransforms[playerHands.Count - 1]);
                splitCard.transform.localPosition = Vector2.zero;

                playerHands[playerHands.Count - 1].Add(splitCard);

                // Deal additional cards
                DealCardToPlayer(currentHandIndex);
                DealCardToPlayer(playerHands.Count - 1);

                UnityEngine.Debug.Log("Cards split. Dealing one additional card to each split hand.");
            }
            else
            {
                UnityEngine.Debug.LogError("Insufficient bankroll to split.");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Cannot split.");
        }
    }

    // Check if the player's first two cards are the same value (for splitting)
    private bool CanSplit()
    {
        if (playerHands[currentHandIndex].Count == 2)  // Ensure the player has exactly two cards in the current hand
        {
            string card1 = playerHands[currentHandIndex][0].GetComponent<Image>().sprite.name;
            string card2 = playerHands[currentHandIndex][1].GetComponent<Image>().sprite.name;

            return GetCardValue(card1) == GetCardValue(card2);
        }
        return false;
    }

    // Function to handle player hitting
    public void PlayerHit()
    {
        if (roundActive && !playerTurnOver)
        {
            // Check if hitting was the correct action
            string dealerUpCard = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;
            string recommendedAction = BasicStrategyChecker(playerHands[currentHandIndex], dealerUpCard);

            TrackPlayerAccuracy("Hit", recommendedAction);  // Track accuracy


            if (recommendedAction != "Hit")
            {
                string message = "You chose to Hit, but the recommended action was: " + recommendedAction;
                UnityEngine.Debug.Log(message);
                strategySuggestionText.text = message;  // Display on UI
            }
            else
            {
                strategySuggestionText.text = "You followed the basic strategy by hitting.";
            }

            // Deal the card and check hand status
            DealCardToPlayer(currentHandIndex);
            CheckHandStatus(playerHands[currentHandIndex]);
        }
    }

    // Function to handle player standing
    public void PlayerStand()
    {
        if (roundActive && !playerTurnOver)
        {
            // Check if standing was the correct action
            string dealerUpCard = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;
            string recommendedAction = BasicStrategyChecker(playerHands[currentHandIndex], dealerUpCard);

            TrackPlayerAccuracy("Stand", recommendedAction);  // Track accuracy


            if (recommendedAction != "Stand")
            {
                string message = "You chose to Stand, but the recommended action was: " + recommendedAction;
                UnityEngine.Debug.Log(message);
                strategySuggestionText.text = message;  // Display on UI
            }
            else
            {
                strategySuggestionText.text = "You followed the basic strategy by standing.";
            }

            UnityEngine.Debug.Log("Player stands on hand " + (currentHandIndex + 1));

            // Move to the next hand, if there are any remaining
            if (currentHandIndex < playerHands.Count - 1)
            {
                currentHandIndex++;
                UnityEngine.Debug.Log("Now playing hand " + (currentHandIndex + 1));
            }
            else
            {
                UnityEngine.Debug.Log("Player finished all hands. Dealer's turn.");
                playerTurnOver = true;  // End player's turn after all hands
                DealerPlay();
            }
        }
    }

    public void PlayerDouble()
    {
        if (roundActive && !playerTurnOver && playerHands[currentHandIndex].Count == 2)
        {
            if (currentHandIndex >= handBets.Count)
            {
                UnityEngine.Debug.LogError($"Invalid currentHandIndex: {currentHandIndex}, handBets count: {handBets.Count}");
                return;
            }

            int currentHandBet = handBets[currentHandIndex];  // Get the bet for the current hand

            if (bankroll >= currentHandBet)
            {
                // Deduct the additional bet from the bankroll
                bankroll -= currentHandBet;

                // Double the bet for this hand
                handBets[currentHandIndex] = currentHandBet * 2;

                UnityEngine.Debug.Log($"Player doubled down on hand {currentHandIndex + 1}. New bet: {handBets[currentHandIndex]}, Bankroll: {bankroll}");

                UpdateBankrollUI();
                UpdateCurrentBetUI();

                string dealerUpCard = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;
                string recommendedAction = BasicStrategyChecker(playerHands[currentHandIndex], dealerUpCard);
                TrackPlayerAccuracy("Double", recommendedAction);

                strategySuggestionText.text = recommendedAction != "Double" ?
                    $"You chose to Double, but the recommended action was: {recommendedAction}" :
                    "You followed the basic strategy by doubling down.";

                DealCardToPlayer(currentHandIndex);

                int playerTotal = CalculateHandTotal(playerHands[currentHandIndex]);

                if (playerTotal > 21)
                {
                    UnityEngine.Debug.Log($"Player busted after doubling down on hand {currentHandIndex + 1}.");
                    CheckHandStatus(playerHands[currentHandIndex]);
                }
                else
                {
                    // Move to the next hand, if there are any remaining
                    if (currentHandIndex < playerHands.Count - 1)
                    {
                        currentHandIndex++;
                        UnityEngine.Debug.Log("Now playing hand " + (currentHandIndex + 1));
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Player finished all hands. Dealer's turn.");
                        playerTurnOver = true;  // End player's turn after all hands
                        DealerPlay();
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Insufficient bankroll to double down.");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Double down is only allowed with exactly 2 cards.");
        }
    }

    private int playerHandsBusted = 0; // Track the number of busted hands

    // Function to check the status of a hand after a hit
    private void CheckHandStatus(List<GameObject> hand)
    {
        int handTotal = CalculateHandTotal(hand);

        if (handTotal > 21)
        {
            UnityEngine.Debug.Log("Hand " + (currentHandIndex + 1) + " busted!");

            // Move to the next hand automatically
            if (currentHandIndex < playerHands.Count - 1)
            {
                currentHandIndex++;
                UnityEngine.Debug.Log("Now playing hand " + (currentHandIndex + 1));
            }
            else
            {
                UnityEngine.Debug.Log("Player finished all hands. Dealer's turn.");
                playerTurnOver = true;
                DealerPlay();
            }
        }
    }

    private int CalculateHandTotal(List<GameObject> hand)
    {
        int total = 0;

        foreach (GameObject cardGO in hand)
        {
            string cardName = cardGO.GetComponent<Image>().sprite.name;
            total += GetCardValue(cardName);
        }

        // Handle Aces
        int aceCount = CountAces(hand);
        while (total > 21 && aceCount > 0)
        {
            total -= 10; // Convert Ace from 11 to 1
            aceCount--;
        }

        UnityEngine.Debug.Log($"[CalculateHandTotal] Hand Total: {total}, Ace Count: {aceCount}");
        return total;
    }

    private int CountAces(List<GameObject> hand)
    {
        int count = 0;
        foreach (GameObject cardGO in hand)
        {
            string cardName = cardGO.GetComponent<Image>().sprite.name;
            if (cardName.StartsWith("1_of")) 
            {
                count++;
            }
        }
        return count;
    }

    private int GetCardValue(string cardName)
    {
        string[] parts = cardName.Split('_');  // "10_of_clubs" becomes ["10", "of", "clubs"]
        int value = int.Parse(parts[0]);  // Get the numeric value of the card

        if (value > 10) return 10;  // Face cards (Jack, Queen, King) are worth 10
        if (value == 1) return 11;  // Aces are worth 11 (handled later if needed)

        return value;
    }

    private void SetCardImage(GameObject cardGO, string cardName)
    {
        Image cardImage = cardGO.GetComponent<Image>();
        Sprite cardSprite = Resources.Load<Sprite>(cardName);

        if (cardSprite != null)
        {
            cardImage.sprite = cardSprite;
            UnityEngine.Debug.Log("Successfully set sprite for: " + cardName);  // Confirm sprite assignment
        }
        else
        {
            UnityEngine.Debug.LogError("Sprite not found for: " + cardName);  // Catch any errors
        }
    }

    private void SetCardBackImage(GameObject cardGO)
    {
        Image cardImage = cardGO.GetComponent<Image>();
        Sprite cardBackSprite = Resources.Load<Sprite>("card-back");  

        if (cardBackSprite != null)
        {
            cardImage.sprite = cardBackSprite;  // Set the card back image
        }
        else
        {
            UnityEngine.Debug.LogError("Card-back sprite not found.");
        }
    }

    private void UpdateRunningCount(string cardName)
    {
        // Basic Hi-Lo card counting system
        if (cardName.StartsWith("2_") || cardName.StartsWith("3_") || cardName.StartsWith("4_") ||
            cardName.StartsWith("5_") || cardName.StartsWith("6_"))
        {
            runningCount += 1;  // Low cards increase running count
        }
        else if (cardName.StartsWith("10_") || cardName.StartsWith("11_") || cardName.StartsWith("12_") ||
                 cardName.StartsWith("13_") || cardName.StartsWith("1_of")) // '1_of' represents Ace
        {
            runningCount -= 1;  // High cards (10, J, Q, K, Ace) decrease running count
        }

        UpdateRunningCountUI();
    }

    private void UpdateRunningCountUI()
    {
        // Display the running count
        runningCountText.text = "Running Count: " + runningCount;

        // Calculate the number of decks remaining
        float decksRemaining = (float)deck.Count / 52.0f;

        // Prevent division by zero
        if (decksRemaining < 1) decksRemaining = 1;

        // Calculate the true count
        float trueCount = runningCount / decksRemaining;

        // Display the true count rounded to 1 decimal place
        trueCountText.text = "True Count: " + trueCount.ToString("F1");
    }

    private void InitializeDeck()
    {
        deck = new List<string>();  // Initialize a new deck
        string[] suits = { "clubs", "diamonds", "hearts", "spades" };

        // Loop through each deck based on the selected deck count
        for (int d = 0; d < selectedDeckCount; d++)
        {
            for (int i = 1; i <= 13; i++)  // Cards 1 (Ace) to 13 (King)
            {
                foreach (string suit in suits)
                {
                    deck.Add(i + "_of_" + suit);  // Add cards
                }
            }
        }

        ShuffleDeck();  // Shuffle the deck after initialization

        UnityEngine.Debug.Log("Deck initialized with " + deck.Count + " cards.");
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1); string temp = deck[i];
            deck[i] = deck[rnd];
            deck[rnd] = temp;
        }
    }

    private string DealCard()
    {
        if (deck.Count == 0)
        {
            UnityEngine.Debug.LogError("The deck is empty!");
            return null;
        }

        string dealtCard = deck[0];
        deck.RemoveAt(0);  // Remove the dealt card from the deck
        return dealtCard;
    }

    private void CheckAndReshuffleDeck()
    {
        UnityEngine.Debug.Log("Cards remaining in deck: " + deck.Count);  // Log the number of remaining cards

        if (deck.Count < 13)  // If fewer than 13 cards remain, reshuffle the deck
        {
            UnityEngine.Debug.Log("Reshuffling deck as fewer than 13 cards remain.");
            InitializeDeck();  // Reinitialize and reshuffle the deck

            // Reset the running count and true count after reshuffle
            runningCount = 0;
            UpdateRunningCountUI();  // Update the UI to reflect the reset running count

            UpdateDecksRemainingUI();  // Update the decks remaining after reshuffle
        }
    }

    // Call this method after each round ends
    public void OnNextRoundButtonClick()
    {
        ClearHands();  // Clear cards for the new round
        CheckAndReshuffleDeck();  // Check if the deck needs to be reshuffled
        ResetBet();
        canPlaceBet = true;
        dealButton.interactable = false;  // Deal button disabled until a bet is placed
        nextRoundButton.gameObject.SetActive(false);
        resetBetButton.interactable = true;  // Re-enable Reset Bet button after round end



        // Clear the basic strategy suggestion text
        strategySuggestionText.text = "";  // Reset strategy suggestion text for the new round
        notificationText.text = "";  // Reset the notification text for the new round

    }

    // Function to handle dealer's play
    private void DealerPlay()
    {
        RevealDealerCard();  // Reveal the hidden dealer card
        dealerTotal = CalculateDealerTotal();

        UnityEngine.Debug.Log("Dealer's total: " + dealerTotal);

        // Dealer hits until reaching 17 or higher
        while (dealerTotal < 17)
        {
            DealCardToDealer();
            dealerTotal = CalculateDealerTotal();
            UnityEngine.Debug.Log("Dealer hits. New total: " + dealerTotal);
        }

        UnityEngine.Debug.Log("Dealer stands with total: " + dealerTotal);
        CheckWinConditions();  // Check who won

        roundActive = false;  // Set the round to inactive
        DisplayRoundResults();

        // After dealer plays, check and reshuffle if needed
        CheckAndReshuffleDeck();  // Add reshuffle check here
    }

    private void RevealDealerCard()
    {
        // Reveal the dealer's hidden card by setting the correct sprite
        if (dealerHandTransform.childCount > 1) // Ensure there is a second card to reveal
        {
            GameObject hiddenCard = dealerHandTransform.GetChild(1).gameObject;
            SetCardImage(hiddenCard, dealerHiddenCardName);
        }
        else
        {
            UnityEngine.Debug.LogError("No second dealer card to reveal.");
        }
    }

    private int CalculateDealerTotal()
    {
        int total = 0;

        // Loop through the dealer's cards and calculate the total
        for (int i = 0; i < dealerHandTransform.childCount; i++)
        {
            string cardName = dealerHandTransform.GetChild(i).GetComponent<UnityEngine.UI.Image>().sprite.name;
            total += GetCardValue(cardName);  
        }

        // Handle Aces: Aces can be either 1 or 11
        int aceCount = CountAces(dealerHandTransform);
        while (total > 21 && aceCount > 0)
        {
            total -= 10;  // Convert Ace from 11 to 1
            aceCount--;
        }

        return total;
    }

    // Helper method to count Aces in the dealer's hand
    private int CountAces(Transform handTransform)
    {
        int count = 0;
        for (int i = 0; i < handTransform.childCount; i++)
        {
            string cardName = handTransform.GetChild(i).GetComponent<UnityEngine.UI.Image>().sprite.name;
            if (cardName.StartsWith("1_of"))  
            {
                count++;
            }
        }
        return count;
    }

    // Function to check win conditions after dealer plays
    private void CheckWinConditions()
    {
        int dealerTotalFinal = CalculateDealerTotal();

        // Compare totals to determine the winner for each hand
        for (int i = 0; i < playerHands.Count; i++)
        {
            // Safeguard: Ensure the index is within bounds
            if (i >= handBets.Count)
            {
                UnityEngine.Debug.LogError($"Mismatch in hand and bet counts. playerHands.Count: {playerHands.Count}, handBets.Count: {handBets.Count}");
                break;  // Exit the loop to prevent accessing out-of-range index
            }

            int handTotal = CalculateHandTotal(playerHands[i]);
            int currentBet = handBets[i];  // Use the specific bet for each hand

            UnityEngine.Debug.Log($"Checking win conditions for Hand {i + 1}: Player Total = {handTotal}, Dealer Total = {dealerTotalFinal}, Bet = {currentBet}");

            if (handTotal > 21)
            {
                UnityEngine.Debug.Log($"Hand {i + 1} busted!");
                notificationText.text = $"Hand {i + 1} busted!";
                // No further bankroll change needed, bet already deducted
            }
            else if (dealerTotalFinal > 21)
            {
                UnityEngine.Debug.Log($"Dealer busted! Player wins Hand {i + 1}. Payout: {currentBet * 2}");
                notificationText.text = $"Dealer busted! Player wins Hand {i + 1}. Payout: {currentBet * 2}";
                bankroll += currentBet * 2;  // Correct payout: double the bet
            }
            else if (handTotal > dealerTotalFinal)
            {
                UnityEngine.Debug.Log($"Player wins Hand {i + 1}. Payout: {currentBet * 2}");
                notificationText.text = $"Player wins Hand {i + 1}. Payout: {currentBet * 2}";
                bankroll += currentBet * 2;  // Correct payout: double the bet
            }
            else if (dealerTotalFinal > handTotal)
            {
                UnityEngine.Debug.Log($"Dealer wins Hand {i + 1}. Player loses bet of {currentBet}.");
                notificationText.text = $"Dealer wins Hand {i + 1}. Player loses bet of {currentBet}.";
                // No further bankroll change needed, bet already deducted
            }
            else
            {
                UnityEngine.Debug.Log($"Hand {i + 1} is a tie. Returning bet: {currentBet}");
                notificationText.text = $"Hand {i + 1} is a tie. Returning bet: {currentBet}";
                bankroll += currentBet;  // Correct return of the bet on a tie
            }

            UnityEngine.Debug.Log($"After Hand {i + 1}, Bankroll: {bankroll}");
            UpdateBankrollUI();  // Update bankroll after each hand
        }

        EndRound("Round over.");
    }

    private void EndRound(string resultMessage)
    {
        UnityEngine.Debug.Log(resultMessage);

        // Mark the round as inactive to block further player actions
        roundActive = false;


        // Show the "Next Round" button to let the player start the next round
        nextRoundButton.gameObject.SetActive(true);
    }







    // Method to determine the best action based on basic strategy chart
    public string BasicStrategyChecker(List<GameObject> playerHand, string dealerUpCard)
    {
        int dealerValue = GetCardValue(dealerUpCard);  // Get the value of the dealer's up-card
        int playerTotal = CalculateHandTotal(playerHand);

        bool isSoft = IsSoftHand(playerHand);  // Check if the player has a soft hand (contains Ace valued as 11)
        bool isPair = IsPair(playerHand);      // Check if the player has a pair

        UnityEngine.Debug.Log("Player Total: " + playerTotal);
        UnityEngine.Debug.Log("Is Soft Hand? " + isSoft);
        UnityEngine.Debug.Log("Dealer's Card: " + dealerUpCard);

        // Pairs
        if (isPair)
        {
            int cardValue = GetCardValue(playerHand[0].GetComponent<Image>().sprite.name);  // Get the value of the pair

            switch (cardValue)
            {
                case 1:  // Aces
                    return "Split";  // Always split Aces
                case 8:
                    return "Split";  // Always split 8s
                case 9:
                    if (dealerValue == 7 || dealerValue >= 10 || dealerValue == 1)
                        return "Stand";  // Stand on pair of 9s if dealer shows 7, 10, or Ace
                    return "Split";  // Split 9s in all other cases
                case 7:
                    if (dealerValue <= 7)
                        return "Split";  // Split 7s if dealer shows 2-7
                    return "Hit";  // Hit otherwise
                case 6:
                    if (dealerValue <= 6)
                        return "Split";  // Split 6s if dealer shows 2-6
                    return "Hit";  // Hit otherwise
                case 5:
                    if (dealerValue <= 9)
                        return "Double";  // Double on 5s if dealer shows 2-9
                    return "Hit";  // Hit otherwise
                case 4:
                    if (dealerValue == 5 || dealerValue == 6)
                        return "Split";  // Split 4s if dealer shows 5 or 6
                    return "Hit";  // Hit otherwise
                case 3:
                case 2:
                    if (dealerValue <= 7)
                        return "Split";  // Split 2s and 3s if dealer shows 2-7
                    return "Hit";  // Hit otherwise
                case 10:
                    return "Stand";  // Stand on pair of 10s (10s, Jacks, Queens, Kings)
                default:
                    return "Hit";  // Fallback
            }
        }

        // Soft hands (contains Ace counted as 11)
        if (isSoft)
        {
            switch (playerTotal)
            {
                case 21:  // Soft 21 (A10 or A-King)
                    return "Stand";  // Always stand on soft 21
                case 20:  // Soft 20 (A9)
                    return "Stand";  // Always stand on soft 20
                case 19:  // Soft 19 (A8)
                    if (dealerValue == 6)
                        return "Double";  // Double if dealer shows 6
                    return "Stand";  // Stand otherwise
                case 18:  // Soft 18 (A7)
                    if (dealerValue >= 2 && dealerValue <= 6)
                        return "Double";  // Double if dealer shows 3-6
                    if (dealerValue == 7 || dealerValue == 8)
                        return "Stand";  // Stand if dealer shows 2, 7, or 8
                    return "Hit";  // Hit otherwise
                case 17:  // Soft 17 (A6)
                    if (dealerValue >= 3 && dealerValue <= 6)
                        return "Double";  // Double if dealer shows 3-6
                    return "Hit";  // Hit otherwise
                case 16:  // Soft 16 (A5)
                case 15:  // Soft 15 (A4)
                    if (dealerValue >= 4 && dealerValue <= 6)
                        return "Double";  // Double if dealer shows 4-6
                    return "Hit";  // Hit otherwise
                case 14:  // Soft 14 (A3)
                case 13:  // Soft 13 (A2)
                    if (dealerValue >= 5 && dealerValue <= 6)
                        return "Double";  // Double if dealer shows 5-6
                    return "Hit";  // Hit otherwise
                default:
                    return "Hit";  // Fallback for soft hands below 13
            }
        }

        // Hard hands (no Ace counted as 11)
        switch (playerTotal)
        {
            case 21:  // Hard 21
                return "Stand";  // Always stand on hard 21
            case 20:  // Hard 20 (10 and 10 or face cards)
                return "Stand";  // Always stand on 20
            case 19:  // Hard 19
                return "Stand";  // Always stand on hard 19
            case 18:  // Hard 18
                return "Stand";  // Always stand on hard 18
            case 17:  // Hard 17
                return "Stand";  // Always stand on hard 17
            case 16:  // Hard 16
                if (dealerValue >= 2 && dealerValue <= 6)
                    return "Stand";  // Stand if dealer shows 2-6
                return "Hit";  // Hit if dealer shows 7 or higher
            case 15:  // Hard 15
                if (dealerValue >= 2 && dealerValue <= 6)
                    return "Stand";  // Stand if dealer shows 2-6
                return "Hit";  // Hit if dealer shows 7 or higher
            case 14:  // Hard 14
                if (dealerValue >= 2 && dealerValue <= 6)
                    return "Stand";  // Stand if dealer shows 2-6
                return "Hit";  // Hit if dealer shows 7 or higher
            case 13:  // Hard 13
                if (dealerValue >= 2 && dealerValue <= 6)
                    return "Stand";  // Stand if dealer shows 2-6
                return "Hit";  // Hit if dealer shows 7 or higher
            case 12:  // Hard 12
                if (dealerValue >= 4 && dealerValue <= 6)
                    return "Stand";  // Stand if dealer shows 4-6
                return "Hit";  // Hit if dealer shows 2-3 or 7 or higher
            case 11:  // Hard 11
                return "Double";  // Always double on 11
            case 10:  // Hard 10
                if (dealerValue >= 2 && dealerValue <= 9)
                    return "Double";  // Double if dealer shows 2-9
                return "Hit";  // Hit if dealer shows 10 or Ace
            case 9:  // Hard 9
                if (dealerValue >= 3 && dealerValue <= 6)
                    return "Double";  // Double if dealer shows 3-6
                return "Hit";  // Hit if dealer shows 2, or 7 and higher
            case 8:  // Hard 8
                return "Hit";  // Always hit on hard 8
            case 7:  // Hard 7
                return "Hit";  // Always hit on hard 7
            case 6:  // Hard 6
                return "Hit";  // Always hit on hard 6
            case 5:  // Hard 5
                return "Hit";  // Always hit on hard 5
            case 4:  // Hard 4
                return "Hit";  // Always hit on hard 4
            default:
                return "Hit";  // Fallback for unrecognized hands
        }
    }

    // Check if the hand is a soft hand (contains an Ace valued as 11)
    private bool IsSoftHand(List<GameObject> hand)
    {
        int total = 0;
        int aceCount = 0;

        // Calculate hand total and count Aces
        foreach (GameObject cardGO in hand)
        {
            string cardName = cardGO.GetComponent<Image>().sprite.name;
            int cardValue = GetCardValue(cardName);
            total += cardValue;
            if (cardValue == 11) aceCount++;  // Count Aces valued as 11
        }

        // Adjust Aces from 11 to 1 if total exceeds 21
        while (total > 21 && aceCount > 0)
        {
            total -= 10;  // Convert Ace from 11 to 1
            aceCount--;   // Reduce Ace count because one was adjusted
        }

        // If there are any Aces still valued as 11 (after adjustments), it is a soft hand
        return aceCount > 0 && total <= 21;
    }

    // Check if the player has a pair 
    private bool IsPair(List<GameObject> hand)
    {
        if (hand.Count == 2)
        {
            string card1 = hand[0].GetComponent<Image>().sprite.name;
            string card2 = hand[1].GetComponent<Image>().sprite.name;
            return GetCardValue(card1) == GetCardValue(card2);
        }
        return false;
    }

    public void DisplayBasicStrategy()
    {
        string dealerUpCard = dealerHandTransform.GetChild(0).GetComponent<Image>().sprite.name;  // Dealer's up-card
        string suggestedAction = BasicStrategyChecker(playerHands[currentHandIndex], dealerUpCard);

        UnityEngine.Debug.Log("Suggested Action: " + suggestedAction);  // Log the suggested action

        // Display this to the player via UI Text element
        strategySuggestionText.text = "Suggested Action: " + suggestedAction;
    }


    private int totalActions = 0;  // Total actions made by the player
    private int correctActions = 0;  // Actions that matched basic strategy

    private void UpdateDecksRemainingUI()
    {
        float decksRemaining = (float)deck.Count / 52.0f;

        // Round up to the nearest 0.5 deck
        float roundedDecksRemaining = Mathf.Ceil(decksRemaining * 2) / 2.0f;

        decksRemainingText.text = "Decks Remaining: " + roundedDecksRemaining.ToString("F1");
    }

    private void TrackPlayerAccuracy(string playerAction, string recommendedAction)
    {
        totalActions++;  // Increment total actions

        if (playerAction == recommendedAction)
        {
            correctActions++;  // Increment correct actions if player followed the suggestion
        }

        UpdateAccuracyUI();  // Update accuracy display
    }

    private void UpdateAccuracyUI()
    {
        if (totalActions > 0)
        {
            float accuracy = ((float)correctActions / totalActions) * 100f;
            accuracyText.text = "Accuracy: " + accuracy.ToString("F1") + "%";
        }
        else
        {
            accuracyText.text = "Accuracy: N/A";  
        }
    }

    private void ToggleCountVisibility()
    {
        isCountVisible = !isCountVisible; // Toggle visibility state
        UpdateCountVisibility();          // Apply visibility change
    }

    private void UpdateCountVisibility()
    {
        runningCountText.gameObject.SetActive(isCountVisible); // Show/hide running count
        trueCountText.gameObject.SetActive(isCountVisible);    // Show/hide true count
    }

    private void DisplayAllHandResults()
    {
    string resultMessage = ""; // Initialize an empty string to hold all results

    int dealerTotal = CalculateDealerTotal();  // Calculate dealer�s total once
    bool dealerBusted = dealerTotal > 21;

    for (int i = 0; i < playerHands.Count; i++)
    {
        int handTotal = CalculateHandTotal(playerHands[i]);
        
        // Ensure there�s a bet associated with each hand, defaulting to 0 if missing
        int currentBet = i < handBets.Count ? handBets[i] : 0;
        int payout = currentBet * 2;  // Calculate the payout here for clarity

        // Formulate the result text for the current hand
        if (handTotal > 21)
        {
            resultMessage += $"Hand {i + 1} busted! ";
        }
        else if (dealerBusted)
        {
            resultMessage += $"Dealer busted! Player wins Hand {i + 1}. Payout: {payout} ";
        }
        else if (handTotal > dealerTotal)
        {
            resultMessage += $"Player wins Hand {i + 1}. Payout: {payout} ";
        }
        else if (handTotal == dealerTotal)
        {
            resultMessage += $"Hand {i + 1} is a tie. Returning bet: {currentBet} ";
        }
        else
        {
            resultMessage += $"Dealer wins Hand {i + 1}. Player loses bet of {currentBet}. ";
        }
    }

    // Display the full message in the UI
    notificationText.text = resultMessage;  // Update the notificationText to display all results in one line
    }

    private void DisplayRoundResults()
    {
        if (!roundActive) // Ensure the round is inactive before displaying results
        {
            DisplayAllHandResults();  // Only display all hand results at the end of a regular round
        }
    }

    private void ResetCurrentBet()
    {
        if (canPlaceBet)
        {
            int refundAmount = currentBet;  // Store the current bet amount before resetting
            currentBet = 0;  // Reset current bet to 0

            bankroll += refundAmount;  // Add the refunded amount back to bankroll
            UpdateBankrollUI();  // Update the bankroll UI to reflect the refund
            UpdateCurrentBetUI();  // Update the current bet UI to reflect the reset

            dealButton.interactable = false;  // Keep Deal button disabled until a new bet is placed
        }
    }

}