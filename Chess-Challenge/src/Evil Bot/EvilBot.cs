using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        Board board;

        int[] values = { 0, 100, 300, 320, 500, 900, 0 };
        public Move Think(Board board, Timer timer)
        {
            this.board = board;
            Move[] moves = board.GetLegalMoves();
            moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ToArray();
            double highestEval = -10;
            Random rng = new();
            List<Move> bestMoves = new List<Move>();
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                double eval = -recursiveLookUp(5, -10f, 10f);
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
        double recursiveLookUp(int depth, double alpha, double beta)
        {
            if (board.IsInCheckmate())
            {
                return -10;
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
                double whiteScoreUp = whiteScore - blackScore;
                double score = 10d * Math.Tanh(whiteScoreUp / 32f);
                return board.IsWhiteToMove ? score : -score;
            }


            Move[] moves = board.GetLegalMoves();
            moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ThenBy(m => (int)m.MovePieceType).ToArray();
            double eval;
            foreach (Move move in moves)
            {

                board.MakeMove(move);
                eval = -recursiveLookUp(depth - 1, -beta, -alpha);
                board.UndoMove(move);
                if (eval >= beta)
                {

                    return beta;
                }
                alpha = Math.Max(eval, alpha);

            }
            return alpha;
        }
    }
}