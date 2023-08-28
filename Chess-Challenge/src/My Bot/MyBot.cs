using ChessChallenge.API;
//using ChessChallenge.Chess;
//using ChessChallenge.Chess;
using System;
using System.Linq;

public class MyBot : IChessBot
{


    Board board;

    int[] values = { 0, 100, 300, 320, 500, 900, 0 };

    int getPieceValue(PieceType pieceType)
    {
        return values[(int)pieceType];
    }


    struct Transposition
    {
        public ulong zobristHash;
        public sbyte depth;
        public int eval;
        public byte flag;
    }

    Transposition[] transpositiontable = new Transposition[8388608];

    int noTT;
    int bestTTmatch;
    bool betaCutOff;



    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        Move[] moves = board.GetLegalMoves();
        moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ToArray();
        int highestEval = int.MinValue + 2;
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -recursiveLookUp(4, int.MinValue + 2, -highestEval);
            if (eval > highestEval)
            {
                highestEval = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return bestMove;
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
            int q = qSearch(alpha, beta);
            return q;
        }


        var (moves, TTeval, alphabetaCutoff) = orderAndcheckForTranspos(alpha, beta, depth, board.GetLegalMoves());
        if (alphabetaCutoff)
            return TTeval;
        else
            alpha = Math.Max(TTeval, alpha);
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int eval = -recursiveLookUp(depth - 1, -beta, -alpha);


            ref Transposition transpo = ref transpositiontable[board.ZobristKey & 4095];
            transpo.depth = (sbyte)depth;
            transpo.zobristHash = board.ZobristKey;
            transpo.eval = eval;

            board.UndoMove(move);
            if (eval >= beta)
            {
                transpo.flag = 2;
                return beta;
            }
            else if (eval >= alpha)
            {
                transpo.flag = 3;
                alpha = eval;
            }
            else
            {
                transpo.flag = 1;
            }
        }
        return alpha;
    }

    int qSearch(int alpha, int beta)
    {
        bool isWhiteToMove = board.IsWhiteToMove;


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
                blackScore += pieceEval(pieceLists[i], false);
            else
                whiteScore += pieceEval(pieceLists[i], true);
        }
        int whiteScoreUp = whiteScore - blackScore;

        int stand_pat = isWhiteToMove ? whiteScoreUp : -whiteScoreUp;




        //easy endgames
        if ((isWhiteToMove ? blackScore : whiteScore) < 299)
        {
            int endgameeval = 0;
            int file = board.GetKingSquare(!isWhiteToMove).File;
            int rank = board.GetKingSquare(!isWhiteToMove).Rank;
            endgameeval += Math.Max(3 - rank, rank - 4);
            endgameeval += Math.Max(3 - file, file - 4);

            int endgameeval2 = (14 - Math.Abs((board.GetKingSquare(isWhiteToMove).Rank - board.GetKingSquare(!isWhiteToMove).Rank)) - Math.Abs((board.GetKingSquare(isWhiteToMove).File - board.GetKingSquare(!isWhiteToMove).File)));

            stand_pat += endgameeval * 20 + endgameeval2 * 20;

        }



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



    int pieceEval(PieceList pieceList, bool isWhite)
    {
        int val = 0;
        ulong adjacentKingSquares = BitboardHelper.GetKingAttacks(board.GetKingSquare(!isWhite));
        foreach (Piece piece in pieceList)
        {
            ulong pieceAttacks = BitboardHelper.GetPieceAttacks(pieceList.TypeOfPieceInList, piece.Square, board, isWhite);
            val += BitboardHelper.GetNumberOfSetBits(pieceAttacks);

            if ((pieceAttacks & adjacentKingSquares) != 0)
            {
                val += 15;
            }

            if (pieceList.TypeOfPieceInList == PieceType.Pawn)
            {
                val += (int)Math.Pow((isWhite ? piece.Square.Rank : 7 - piece.Square.Rank), 3) / 20;
            }

        }
        return pieceList.Count * getPieceValue(pieceList.TypeOfPieceInList) + val * 4;
    }

    (Move[], int, bool) orderAndcheckForTranspos(int alpha, int beta, int depth, Move[] moves)
    {
        noTT = 0;
        bestTTmatch = int.MinValue + 10;
        betaCutOff = false;

        Move[] moves2 = moves.OrderByDescending(
            move =>
            {
                if (betaCutOff)
                    return -1;

                board.MakeMove(move);

                ref Transposition transposition = ref transpositiontable[board.ZobristKey & 8388607];

                if (transposition.zobristHash == board.ZobristKey)
                {
                    if (transposition.depth >= depth)
                    {
                        if (transposition.flag == 1)
                        {
                            bestTTmatch = Math.Max(bestTTmatch, transposition.eval);
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                        else if (transposition.flag == 2 && transposition.eval >= beta)
                        {
                            bestTTmatch = transposition.eval;
                            betaCutOff = true;
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                        else if (transposition.flag == 3 && transposition.eval <= alpha)
                        {
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                    }
                    else
                    {

                        noTT++;
                        board.UndoMove(move);
                        return transposition.eval + (int)move.CapturePieceType * 10 - (int)move.MovePieceType;
                    }
                }
                noTT++;
                board.UndoMove(move);
                //move ordering hier improoven
                return (int)move.CapturePieceType * 10 - (int)move.MovePieceType;


            }).ToArray();


        return (moves2.Take(noTT).ToArray(), bestTTmatch, betaCutOff);
    }
}




