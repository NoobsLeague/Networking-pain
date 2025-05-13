using shared;
using UnityEngine;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    // Keep track of how many times a player clicked the board
    private int player1MoveCount = 0;
    private int player2MoveCount = 0;
    
    // Player names
    private string player1Name = "Player 1";
    private string player2Name = "Player 2";
    
    // Current player ID (1 or 2)
    private int currentPlayerId = 1; // Player 1 starts by default
    
    // My player ID (determined by matching names)
    private int myPlayerId = 0;
    
    // Total moves made in the game
    private int totalMoves = 0;

    public override void EnterState()
    {
        base.EnterState();
        
        view.gameBoard.OnCellClicked += _onCellClicked;
        view.QuitGameButton.onClick.AddListener(QuitGame);
        
        // Reset game state
        player1MoveCount = 0;
        player2MoveCount = 0;
        currentPlayerId = 1; // Player 1 starts by default
        totalMoves = 0;
        
        Debug.Log("Game state entered. Waiting for player names...");
    }

    private void _onCellClicked(int pCellIndex)
    {
        Debug.Log($"Cell clicked: {pCellIndex}, myPlayerId: {myPlayerId}, currentPlayerId: {currentPlayerId}, totalMoves: {totalMoves}");
        
        // Special case for the very first move (only player 1 can make it)
        if (totalMoves == 0 && currentPlayerId == 1)
        {
            Debug.Log("First move of the game attempted...");
            // If we're definitely player 2, don't allow this move
            if (myPlayerId == 2)
            {
                Debug.Log("Not your turn! You're player 2, can't make first move.");
                return;
            }
            
            // Otherwise allow the move (either we're player 1 or we don't know yet)
            MakeMoveRequest makeMoveRequest = new MakeMoveRequest();
            makeMoveRequest.move = pCellIndex;
            fsm.channel.SendMessage(makeMoveRequest);
            return;
        }
        
        // For the second move, if myPlayerId is still 0, assume we're player 2
        if (totalMoves == 1 && myPlayerId == 0 && currentPlayerId == 2)
        {
            Debug.Log("Second move of the game - we must be player 2");
            myPlayerId = 2;
        }
        
        // For subsequent moves, check if it's my turn
        if (myPlayerId != currentPlayerId)
        {
            Debug.Log($"Not your turn! Your ID: {myPlayerId}, Current turn: {currentPlayerId}");
            return;
        }
        
        MakeMoveRequest request = new MakeMoveRequest();
        request.move = pCellIndex;
        fsm.channel.SendMessage(request);
    }

    private void QuitGame()
    {
        // Send concede request to the server
        fsm.channel.SendMessage(new ConcedeRequest());
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
        view.QuitGameButton.onClick.RemoveListener(QuitGame);
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
        
        // Update UI to indicate whose turn it is
        UpdateTurnIndicator();
    }
    
    private void UpdateTurnIndicator()
    {
        // Add visual indicator for whose turn it is
        if (view.playerLabel1 != null && view.playerLabel2 != null)
        {
            string player1Text = $"{player1Name} (Moves: {player1MoveCount})";
            string player2Text = $"{player2Name} (Moves: {player2MoveCount})";
            
            // Add turn indicator
            if (currentPlayerId == 1)
            {
                player1Text = "→ " + player1Text + " ←";
            }
            else if (currentPlayerId == 2)
            {
                player2Text = "→ " + player2Text + " ←";
            }
            
            // Add "YOU" indicator to show which player you are
            if (myPlayerId == 1)
            {
                player1Text += " (YOU)";
            }
            else if (myPlayerId == 2)
            {
                player2Text += " (YOU)";
            }
            
            view.playerLabel1.text = player1Text;
            view.playerLabel2.text = player2Text;
        }
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is MakeMoveResult makeMoveResult)
        {
            handleMakeMoveResult(makeMoveResult);
        }
        else if (pMessage is PlayerNames playerNames)
        {
            handlePlayerNames(playerNames);
        }
        else if (pMessage is GameFinished gameFinished)
        {
            handleGameFinished(gameFinished);
        }
    }

    private void handlePlayerNames(PlayerNames playerNames)
    {
        player1Name = playerNames.player1Name;
        player2Name = playerNames.player2Name;
        
        Debug.Log($"Player 1: {player1Name}, Player 2: {player2Name}");
        
        UpdateTurnIndicator();
    }


private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
{
    Debug.Log("Received MakeMoveResult: " + pMakeMoveResult);
    
    // Validate board data
    if (pMakeMoveResult.boardData == null)
    {
        Debug.LogError("MakeMoveResult has null board data!");
        return;
    }
    
    // Log the board data for debugging
    Debug.Log("Board data received: " + pMakeMoveResult.boardData.ToString());
    Debug.Log("Board array: " + (pMakeMoveResult.boardData.board != null ? 
                                string.Join(",", pMakeMoveResult.boardData.board) : "NULL"));
    
    // Update the board visual representation
    if (view != null && view.gameBoard != null)
    {
        Debug.Log("Calling SetBoardData on the gameBoard");
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);
    }
    else
    {
        Debug.LogError("GameView or GameBoard is null! view: " + (view != null) + 
                       ", gameBoard: " + (view != null && view.gameBoard != null));
    }
    
    // Update turn tracking
    currentPlayerId = pMakeMoveResult.boardData.currentTurn;
    
    Debug.Log($"Move result received. Who made move: {pMakeMoveResult.whoMadeTheMove}, " +
             $"Next turn: {currentPlayerId}");
    
    // Increment move counter for the player who moved
    if (pMakeMoveResult.whoMadeTheMove == 1)
    {
        player1MoveCount++;
    }
    else if (pMakeMoveResult.whoMadeTheMove == 2)
    {
        player2MoveCount++;
    }
    
    UpdateTurnIndicator();
}

    private void handleGameFinished(GameFinished gameFinished)
    {
        string resultMessage;
        
        if (gameFinished.IsDraw)
        {
            resultMessage = "Game ended in a draw!";
            Debug.Log(resultMessage);
        }
        else
        {
            resultMessage = gameFinished.YesDadImWinnin ? "You won!" : "You lost!";
            Debug.Log("Game finished! Result: " + resultMessage);
        }
        
        // Switch to Results state
        Results resultsState = FindResultsState();
        if (resultsState != null)
        {
            resultsState.InitializeEnd(gameFinished.boardData, gameFinished.YesDadImWinnin, gameFinished.IsDraw);
            fsm.ChangeState<Results>();
        }
        else
        {
            Debug.LogError("Results state not found!");
        }
    }
    
    private Results FindResultsState()
    {
        // First try to find Results as a direct component of the FSM
        Results resultsState = fsm.GetComponent<Results>();
        
        // If not found, try to find it from the children of the FSM GameObject
        if (resultsState == null)
        {
            foreach (Transform child in fsm.transform)
            {
                resultsState = child.GetComponent<Results>();
                if (resultsState != null) break;
            }
        }
        
        return resultsState;
    }
}