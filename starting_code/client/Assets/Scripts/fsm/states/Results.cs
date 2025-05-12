using UnityEngine;
using shared;
using System.Collections.Generic;

public class Results : ApplicationStateWithView<ResultsView>
{
private TicTacToeBoardData _boardData;
    private bool Win;

    // Called by GameState before the switch
    public void InitializeEnd(TicTacToeBoardData boardData, bool win)
    {
        Debug.Log("EndState: received boardData: " + boardData.ToString());
        _boardData = boardData;
        Win = win;
    }


    public override void EnterState()
    {
        base.EnterState();
        view.gameBoard.SetBoardData(_boardData);
        if (Win)
            {
                view.resultText.text = "You win!";
            }
            else
            {
                view.resultText.text = "You lost.";
            }

        view.returnButton.onClick.AddListener(ReturnLobby);
    }

    public override void ExitState()
    {
        base.ExitState();
        view.returnButton.onClick.RemoveListener(ReturnLobby);
    }

    private void ReturnLobby()
    {
        //send the result to the server whether you win or not
        fsm.channel.SendMessage(new ReturnToLobby{areYaWinninSon = Win});
    }


    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }
//checks if the player is sent to the lobby
    protected override void handleNetworkMessage(ASerializable msg)
    {
        if (msg is RoomJoinedEvent rje && rje.room == RoomJoinedEvent.Room.LOBBY_ROOM)
        {
            fsm.ChangeState<LobbyState>();
        }
    }
}
