using shared;
using System;
using System.Reflection;

namespace server
{
	/**
	 * This room runs a single Game (at a time). 
	 * 
	 * The 'Game' is very simple at the moment:
	 *	- all client moves are broadcasted to all clients
	 *	
	 * The game has no end yet (that is up to you), in other words:
	 * all players that are added to this room, stay in here indefinitely.
	 */
	class GameRoom : Room
	{
		public bool IsGameInPlay { get; private set; }

        //keep track of the member count, to make sure its not empty
        public int MemberCount => base.memberCount;
        //get the room id
        public int RoomId { get; }

		//wraps the board to play on...
		private TicTacToeBoard _board;

		//to keep track of who is in a game
        private readonly List<TcpMessageChannel> _players = new List<TcpMessageChannel>();

        public GameRoom(TCPGameServer pOwner, int id) : base(pOwner)
		{
			//pass the room id
			RoomId = id;
		}

		public void StartGame (TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
		{
			//if (IsGameInPlay) throw new Exception("Programmer error duuuude.");		no

			IsGameInPlay = true;
			//initialize the board here so it can be repeated (not only 1 game)
			_board = new TicTacToeBoard();
			//clear to make sure
			_players.Clear();
			//add players yey
			_players.Add(pPlayer1);
			_players.Add(pPlayer2);
			addMember(pPlayer1);
			addMember(pPlayer2);

			//names
			var p1 = _server.GetPlayerInfo(pPlayer1).name;
			var p2 = _server.GetPlayerInfo(pPlayer2).name;
			
			sendToAll(new PlayerNames { player1Name = p1, player2Name = p2 });
		}


		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);

			//notify client he has joined a game room 
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
			pMember.SendMessage(roomJoinedEvent);
		}

		public override void Update()
		{
			//demo of how we can tell people have left the game...
			int oldMemberCount = memberCount;
			base.Update();
			int newMemberCount = memberCount;

			if (oldMemberCount != newMemberCount)
			{
				Log.LogInfo("People left the game...", this);
			}
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is MakeMoveRequest)
			{
				handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
			}

            else if (pMessage is ConcedeRequest)
            {
				//just send the win to whoever not send the message
                var winner = _players[0] == pSender ? _players[1] : _players[0];
                gameFinished(winner);
            }

            else if (pMessage is ReturnToLobby lobby)
            {
                Log.LogInfo($"[Server] got ReturnToLobbyRequest from {pSender.GetRemoteEndPoint()}", this);
                removeMember(pSender);
                _server.GetLobbyRoom().backToTheLobby(pSender);
                var chat = new ChatMessage
                {
                    message = lobby.areYaWinninSon
                ? "fuck yea i won"
                : "nah im a disapointment"
                };
                pSender.SendMessage(chat);
                return;
            }

        }



		private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
		{
			//we have two players, so index of sender is 0 or 1, which means playerID becomes 1 or 2
			int playerID = indexOfMember(pSender) + 1;
			//make the requested move (0-8) on the board for the player
			_board.MakeMove(pMessage.move, playerID);

			//and send the result of the boardstate back to all clients
			MakeMoveResult makeMoveResult = new MakeMoveResult();
			makeMoveResult.whoMadeTheMove = playerID;
			makeMoveResult.boardData = _board.GetBoardData();
			sendToAll(makeMoveResult);

            var boardData = _board.GetBoardData();
            int winnerId = boardData.WhoHasWon();
            if (winnerId != 0)
            {
                TcpMessageChannel winner = _players[winnerId - 1];
                gameFinished(winner);
            }
        }

		private void gameFinished(TcpMessageChannel winner)
		{
            var loser = _players[0] == winner ? _players[1] : _players[0];
            var data = _board.GetBoardData();
            winner.SendMessage(new GameFinished { boardData = data, YesDadImWinnin = true });
            loser.SendMessage(new GameFinished { boardData = data, YesDadImWinnin = false });
            IsGameInPlay = false;
        }

	}
}
