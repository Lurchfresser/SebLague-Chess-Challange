using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    Board board;

    int[] values = { 0, 100, 300, 320, 500, 900, 0 };
    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        Move[] moves = board.GetLegalMoves();
        int highestEval = int.MinValue;
        Random rng = new();
        List<Move> bestMoves = new List<Move>();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -recursiveLookUp(3, int.MinValue + 1, int.MaxValue - 1);
            if (eval > highestEval)
            {
                highestEval = eval;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (eval == highestEval)
            {
                bestMoves.Add(move);
            }
            board.UndoMove(move);
        }
        return bestMoves[rng.Next(bestMoves.Count)];
    }
    int recursiveLookUp(int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate())
        {
            return int.MinValue + 1;
        }
        else if (board.IsDraw())
        {
            return 0;
        }

        if (depth == 0)
        {

            PieceList[] pieceLists = board.GetAllPieceLists();
            int blackScore = 0;
            int whiteScore = 0;
            for (int i = 0; i < 12; i++)
            {
                if (i > 5)
                    blackScore += pieceLists[i].Count * values[(int)pieceLists[i].TypeOfPieceInList];
                else
                    whiteScore += pieceLists[i].Count * values[(int)pieceLists[i].TypeOfPieceInList];

            }
            int whiteScoreUp = whiteScore - blackScore;
            return board.IsWhiteToMove ? whiteScoreUp : -whiteScoreUp;
        }


        Move[] moves = board.GetLegalMoves();
        int eval;
        foreach (Move move in moves)
        {   

            board.MakeMove(move);
            eval = -recursiveLookUp(depth - 1, -beta, -alpha);
            board.UndoMove(move);
            if (eval >= beta) {
                
                return beta;
            }
            alpha = Math.Max(eval, alpha);

            
        }
        return alpha;
    }
}