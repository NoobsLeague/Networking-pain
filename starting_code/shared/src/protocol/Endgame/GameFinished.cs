using System;
using System.Collections.Generic;
using System.Text;
using shared;

public class GameFinished : ASerializable
{
    //this to serialize the board data and the player status (win or not), to later send them to their client
    public TicTacToeBoardData boardData;
    public bool YesDadImWinnin;
    public override void Serialize(Packet p)
    {
        p.Write(boardData);
        p.Write(YesDadImWinnin);
    }
    public override void Deserialize(Packet p)
    {
        boardData = p.Read<TicTacToeBoardData>();
        YesDadImWinnin = p.ReadBool();
    }
}
