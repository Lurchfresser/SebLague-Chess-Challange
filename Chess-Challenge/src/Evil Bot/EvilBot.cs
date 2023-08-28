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

        int getPieceValue(PieceType pieceType)
        {
            return values[(int)pieceType];
        }


        struct transposition
        {
            public ulong zobristHash;
            public sbyte depth;
            public int eval;
            public bool initialized;
        }

        List<transposition>[] transpositiontable = new List<transposition>[256];

        LinkedList<byte> indexList = new LinkedList<byte>();

        public EvilBot()
        {
            transpositiontable = Enumerable.Range(0, 256).Select(_ => new List<transposition>()).ToArray();
        }

        void setTransposition(transposition transposition)
        {
            int index = (int)transposition.zobristHash & 255;


            transposition matchingTranspo = transpositiontable[index].Find(x => x.zobristHash == transposition.zobristHash);

            if (!matchingTranspo.initialized)
            {
                indexList.AddLast((byte)index);
                transpositiontable[index].Add(transposition);
            }
        }

        transposition getTransposition(ulong zobristHash, int depth)
        {
            int index = (int)zobristHash & 255;


            transposition matchingTranspo = transpositiontable[index].Find(x => x.zobristHash == zobristHash);

            if (matchingTranspo.initialized && depth >= matchingTranspo.depth)
            {
                return matchingTranspo;
            }

            return new transposition();
        }







        public Move Think(Board board, Timer timer)
        {
            this.board = board;
            Move[] moves = board.GetLegalMoves();
            moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ToArray();
            int highestEval = int.MinValue + 10;
            Random rng = new();
            List<Move> bestMoves = new List<Move>();
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = -recursiveLookUp(4, highestEval, int.MaxValue);
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
                return int.MinValue + 10;
            }
            else if (board.IsDraw())
            {
                return 0;
            }
            if (board.IsInCheck())
            {
                depth++;
            }

            if (depth == 0)
            {
                return qSearch(alpha, beta);
            }


            Move[] moves = board.GetLegalMoves();
            moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ThenBy(m => (int)m.MovePieceType).ToArray();
            int eval;
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

        int qSearch(int alpha, int beta)
        {
            if (board.IsInCheckmate())
            {
                return int.MinValue + 10;
            }
            else if (board.IsDraw())
            {
                return 0;
            }
            PieceList[] pieceLists = board.GetAllPieceLists();
            int blackScore = 0;
            int whiteScore = 0;
            for (int i = 0; i < 12; i++)
            {
                if (i > 5)
                    blackScore += pieceLists[i].Count * getPieceValue(pieceLists[i].TypeOfPieceInList);
                else
                    whiteScore += pieceLists[i].Count * getPieceValue(pieceLists[i].TypeOfPieceInList);

            }
            int whiteScoreUp = whiteScore - blackScore;

            int stand_pat = board.IsWhiteToMove ? whiteScoreUp : -whiteScoreUp;

            if (stand_pat >= beta)
                return beta;

            alpha = Math.Max(stand_pat, alpha);

            Move[] moves = board.GetLegalMoves(true);
            if (moves.Length != 0)
            {
                moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ThenBy(m => (int)m.MovePieceType).ToArray();
                foreach (Move move in moves)
                {
                    /*if (getPieceValue(move.CapturePieceType) + 200 + stand_pat < alpha)
                    {
                        continue;
                    }*/

                    board.MakeMove(move);

                    int eval = -qSearch(-beta, -alpha);
                    board.UndoMove(move);
                    if (eval >= beta)
                    {

                        return beta;
                    }
                    alpha = Math.Max(eval, alpha);

                }
                return alpha;
            }

            return alpha;
        }
    }
}