﻿using shared;
using System.Collections.Generic;

namespace server
{
    /**
	 * The LobbyRoom is a little bit more extensive than the LoginRoom.
	 * In this room clients change their 'ready status'.
	 * If enough people are ready, they are automatically moved to the GameRoom to play a Game (assuming a game is not already in play).
	 */
    class LobbyRoom : SimpleRoom
    {
        //this list keeps tracks of which players are ready to play a game, this is a subset of the people in this room
        private List<TcpMessageChannel> _readyMembers = new List<TcpMessageChannel>();

        public LobbyRoom(TCPGameServer pOwner) : base(pOwner)
        {
        }

        protected override void addMember(TcpMessageChannel pMember)
        {
            base.addMember(pMember);

            //tell the member it has joined the lobby
            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
            roomJoinedEvent.room = RoomJoinedEvent.Room.LOBBY_ROOM;
            pMember.SendMessage(roomJoinedEvent);

            //finally im not a john doe
            string name = _server.GetPlayerInfo(pMember).name;
            var Announcer = new ChatMessage { message = $"Client {name} has joined the lobby!" };
            sendToAll(Announcer);

            //send information to all clients that the lobby count has changed
            sendLobbyUpdateCount();
        }

        public void backToTheLobby(TcpMessageChannel player)
        {
            base.addMember(player);
            player.SendMessage(new RoomJoinedEvent { room = RoomJoinedEvent.Room.LOBBY_ROOM });
            sendLobbyUpdateCount();
        }

        /**
		 * Override removeMember so that our ready count and lobby count is updated (and sent to all clients)
		 * anytime we remove a member.
		 */
        protected override void removeMember(TcpMessageChannel pMember)
        {
            base.removeMember(pMember);
            _readyMembers.Remove(pMember);

            sendLobbyUpdateCount();
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            if (pMessage is ChangeReadyStatusRequest) handleReadyNotification(pMessage as ChangeReadyStatusRequest, pSender);
            else if (pMessage is ChatMessage) handleChatMessage(pMessage as ChatMessage, pSender);
        }

        private void handleChatMessage(ChatMessage pMessage, TcpMessageChannel pSender)
        {
            // Get the player name
            string playerName = _server.GetPlayerInfo(pSender).name;

            // Modify the message to include the sender's name
            pMessage.message = $"{playerName}: {pMessage.message}";

            // Broadcast the message to all clients in the room
            sendToAll(pMessage);
        }

        private void handleReadyNotification(ChangeReadyStatusRequest pReadyNotification, TcpMessageChannel pSender)
        {
            //if the given client was not marked as ready yet, mark the client as ready
            if (pReadyNotification.ready)
            {
                if (!_readyMembers.Contains(pSender)) _readyMembers.Add(pSender);
            }
            else //if the client is no longer ready, unmark it as ready
            {
                _readyMembers.Remove(pSender);
            }

            //do we have enough people for a game and is there no game running yet?
            if (_readyMembers.Count >= 2)
            {
                TcpMessageChannel player1 = _readyMembers[0];
                TcpMessageChannel player2 = _readyMembers[1];
                removeMember(player1);
                removeMember(player2);
                _server.StartGame(player1, player2);
            }

            //(un)ready-ing / starting a game changes the lobby/ready count so send out an update
            //to all clients still in the lobby
            sendLobbyUpdateCount();
        }

        private void sendLobbyUpdateCount()
        {
            LobbyInfoUpdate lobbyInfoMessage = new LobbyInfoUpdate();
            lobbyInfoMessage.memberCount = memberCount;
            lobbyInfoMessage.readyCount = _readyMembers.Count;
            sendToAll(lobbyInfoMessage);
        }
    }
}