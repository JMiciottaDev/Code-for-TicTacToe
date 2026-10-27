
using System.Collections.Generic;
using UnityEngine;

public class TicTacToeManager : MonoBehaviour
{
    [Header("Board")]
    public BoardSlot[] slots = new BoardSlot[9];
    public float moveSnapDistance = 0.35f;

    [Header("AI")]
    public TicTacToePiece aiPiecePrefab;
    public float aiThinkTime = 0.8f;

    private TicTacToePiece.PieceType?[] board = new TicTacToePiece.PieceType?[9];

    private TicTacToePiece[] boardPieces = new TicTacToePiece[9];

    private TicTacToePiece.PieceType currentPlayer = TicTacToePiece.PieceType.X;

    private bool gameOver;
    private bool aiThinking;

    private struct Move
    {
        public int from;
        public int to;

        public Move(int from, int to)
        {
            this.from = from;
            this.to = to;
        }
    }

    private void Start()
    {
        if (slots == null || slots.Length != 9)
        {
            enabled = false;
            return;
        }

        foreach (BoardSlot slot in slots)
        {
            if (slot == null || slot.index <+ 0 || slot.index >= 9)
            {
                enabled = false;
                return;
            }
        }
    }

    public bool TryPlacePiece(TicTacToePiece piece, BoardSlot slot)
    {
        if (gameOver || aiThinking || currentPlayer !=
            TicTacToePiece.PieceType.X)
            return false;

        if (piece == null || slot == null)
            return false;

        int target = slot.index;

        if (target < 0 || target >= 9 || slots[target] != slot)
            return false;

        if (board[target].HasValue)
            return false;

        if (piece.pieceType != TicTacToePiece.PieceType.X)
            return false;

        int origin = piece.moveOriginIndex;

        if (origin >= 0)
        {
            if (origin >= 9 || !IsAdjacent(origin, target))
                return false;
        }
        else if (CountPieces(TicTacToePiece.PieceType.X) >= 3)
        {
            Debug.Log("You must move one of your existing pieces.");
            return false;
        }

        board[target] = piece.pieceType;
        boardPieces[target] = piece;
        piece.moveOriginIndex = -1;

        SnapPiece(piece, slots[target]);
        Debug.Log("X placed or moved to square " + target);

        FinishTurn();
        return true;
    }


    public bool PreparePieceForMove(TicTacToePiece piece)
    {
        if (gameOver || aiThinking ||
            currentPlayer != TicTacToePiece.PieceType.X ||
            piece == null ||
            piece.pieceType != TicTacToePiece.PieceType.X ||
            CountPieces(TicTacToePiece.PieceType.X) < 3)
            return false;

        int origin = FindPieceIndex(piece);

        if (origin < 0)
            return false;

        board[origin] = null;
        boardPieces[origin] = null;
        piece.moveOriginIndex = origin;

        piece.transform.SetParent(null, true);

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in piece.GetComponentsInChildren<Collider>())
            col.enabled = true;

        TicTacToeProjectile projectile =
            piece.GetComponent<TicTacToeProjectile>();

        if (projectile != null)
            projectile.ResetForLaunch();

        return true;
    }

    private void FinishTurn()
    {
        if (CheckWin(currentPlayer))
        {
            gameOver = true;
            Debug.Log(currentPlayer + " wins!");
            return;
        }

        currentPlayer = currentPlayer ==
            TicTacToePiece.PieceType.X
            ? TicTacToePiece.PieceType.O
            : TicTacToePiece.PieceType.X;

        Debug.Log("Next turn: " + currentPlayer);

        if (currentPlayer == TicTacToePiece.PieceType.O)
        {
            aiThinking = true;
            Invoke(nameof(PerformAIMove), aiThinkTime);
        }
    }

    private void PerformAIMove()
    {
        if (gameOver)
            return;

        List<Move> moves = GetLegalMoves(
            board, TicTacToePiece.PieceType.O);

        if (moves.Count == 0)
        {
            aiThinking = false;
            gameOver = true;
            Debug.Log("No legal moves remain.");
            return;
        }

        Move bestMove = ChooseAIMove(moves);

        TicTacToePiece piece;

        if (bestMove.from >= 0)
        {
            piece = boardPieces[bestMove.from];

            board[bestMove.from] = null;
            boardPieces[bestMove.from] = null;
        }
        else
        {
            if (aiPiecePrefab == null)
            {
                aiThinking = false;
                return;
            }

            piece = Instantiate(aiPiecePrefab);
            piece.pieceType = TicTacToePiece.PieceType.O;
        }

        // Capture an X piece if the AI moves onto its square.
        if (board[bestMove.to] == TicTacToePiece.PieceType.X)
        {
            TicTacToePiece captured = boardPieces[bestMove.to];

            if (captured != null)
                Destroy(captured.gameObject);

            Debug.Log("AI captured a piece!");
        }

        board[bestMove.to] = TicTacToePiece.PieceType.O;
        boardPieces[bestMove.to] = piece;
        piece.moveOriginIndex = -1;

        SnapPiece(piece, slots[bestMove.to]);

        Debug.Log("AI moved to square " + bestMove.to);

        aiThinking = false;
        FinishTurn();
    }

    private Move ChooseAIMove(List<Move> moves)
    {
        int bestScore = int.MinValue;
        List<Move> bestMoves = new List<Move>();

        foreach (Move move in moves)
        {
            TicTacToePiece.PieceType?[] simulated =
                (TicTacToePiece.PieceType?[])board.Clone();

            if (move.from >= 0)
                simulated[move.from] = null;

            simulated[move.to] = TicTacToePiece.PieceType.O;

            int score = EvaluateBoard(simulated);

            if (CheckWin(simulated, TicTacToePiece.PieceType.O))
                score += 1000;

            // Strongly discourage moves that leave an immediate
            // winning move available to the human.
            if (HasImmediateWin(simulated, TicTacToePiece.PieceType.X))
                score -= 250;

            // Small randomness makes noncritical choices less predictable.
            score += Random.Range(0, 5);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves[Random.Range(0, bestMoves.Count)];
    }

    private int EvaluateBoard(TicTacToePiece.PieceType?[] position)
    {
        int score = 0;

        int[,] lines =
        {
            {0,1,2}, {3,4,5}, {6,7,8},
            {0,3,6}, {1,4,7}, {2,5,8},
            {0,4,8}, {2,4,6}
        };

        for (int i = 0; i < 8; i++)
        {
            int aiCount = 0;
            int humanCount = 0;

            for (int j = 0; j < 3; j++)
            {
                var value = position[lines[i, j]];

                if (value == TicTacToePiece.PieceType.O)
                    aiCount++;
                else if (value == TicTacToePiece.PieceType.X)
                    humanCount++;
            }

            if (humanCount == 0)
            {
                if (aiCount == 2) score += 35;
                else if (aiCount == 1) score += 4;
            }

            if (aiCount == 0)
            {
                if (humanCount == 2) score -= 30;
                else if (humanCount == 1) score -= 3;
            }
        }

        // Prefer the center and corners when other factors are equal.
        if (position[4] == TicTacToePiece.PieceType.O)
            score += 4;

        int[] corners = { 0, 2, 6, 8 };

        foreach (int corner in corners)
        {
            if (position[corner] == TicTacToePiece.PieceType.O)
                score += 2;
        }

        return score;
    }

    private bool HasImmediateWin(
        TicTacToePiece.PieceType?[] position,
        TicTacToePiece.PieceType player)
    {
        List<Move> moves = GetLegalMoves(position, player);

        foreach (Move move in moves)
        {
            TicTacToePiece.PieceType?[] simulated =
                (TicTacToePiece.PieceType?[])position.Clone();

            if (move.from >= 0)
                simulated[move.from] = null;

            simulated[move.to] = player;

            if (CheckWin(simulated, player))
                return true;
        }

        return false;
    }

    private List<Move> GetLegalMoves(
        TicTacToePiece.PieceType?[] position,
        TicTacToePiece.PieceType player)
    {
        List<Move> moves = new List<Move>();
        int count = 0;

        for (int i = 0; i < 9; i++)
        {
            if (position[i] == player)
                count++;
        }

        if (count < 3)
        {
            for (int to = 0; to < 9; to++)
            {
                if (!position[to].HasValue)
                    moves.Add(new Move(-1, to));
            }

            return moves;
        }

        for (int from = 0; from < 9; from++)
        {
            if (position[from] != player)
                continue;

            foreach (BoardSlot neighbor in slots[from].adjacentSlots)
            {
                if (neighbor == null)
                    continue;

                int to = neighbor.index;

                if (to >= 0 && to < 9 && position[to] != player)
                {
                    moves.Add(new Move(from, to));
                }
            }
        }

        return moves;
    }

    private int CountPieces(TicTacToePiece.PieceType player)
    {
        int count = 0;

        foreach (TicTacToePiece piece in boardPieces)
        {
            if (piece != null && piece.pieceType == player)
                count++;
        }

        return count;
    }

    private int FindPieceIndex(TicTacToePiece piece)
    {
        for (int i = 0; i < boardPieces.Length; i++)
        {
            if (boardPieces[i] == piece)
                return i;
        }

        return -1;
    }

    private bool IsAdjacent(int from, int to)
    {
        if (from < 0 || from >= 9 || to < 0 || to >= 9)
            return false;

        foreach (BoardSlot neighbor in slots[from].adjacentSlots)
        {
            if (neighbor != null && neighbor.index == to)
                return true;
        }

        return false;
    }

    private bool CheckWin(TicTacToePiece.PieceType player)
    {
        return CheckWin(board, player);
    }

    private bool CheckWin(
        TicTacToePiece.PieceType?[] position,
        TicTacToePiece.PieceType player)
    {
        int[,] lines =
        {
            {0,1,2}, {3,4,5}, {6,7,8},
            {0,3,6}, {1,4,7}, {2,5,8},
            {0,4,8}, {2,4,6}
        };

        for (int i = 0; i < 8; i++)
        {
            if (position[lines[i, 0]] == player &&
                position[lines[i, 1]] == player &&
                position[lines[i, 2]] == player)
                return true;
        }

        return false;
    }

    private void SnapPiece(TicTacToePiece piece, BoardSlot slot)
    {
        Transform target = slot.placementPoint != null ? slot.placementPoint : slot.transform;

        Rigidbody rb = piece.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        piece.transform.SetPositionAndRotation(
            target.position, target.rotation);

        piece.transform.SetParent(target, true);

        TicTacToeProjectile projectile =
            piece.GetComponent<TicTacToeProjectile>();

        if (projectile != null)
            projectile.MarkLanded();


    }

    public bool BeginHumanPieceMove(TicTacToePiece piece)
    {
        if (gameOver || aiThinking ||
            currentPlayer != TicTacToePiece.PieceType.X ||
            piece == null ||
            piece.pieceType != TicTacToePiece.PieceType.X ||
            CountPieces(TicTacToePiece.PieceType.X) < 3)
        {
            return false;
        }

        int origin = FindPieceIndex(piece);

        if (origin < 0)
            return false;

        piece.moveOriginIndex = origin;
        return true;
    }

    public void EndHumanPieceMove(
        TicTacToePiece piece, Vector3 releasePosition)
    {
        if (piece == null || piece.moveOriginIndex < 0)
            return;

        int origin = piece.moveOriginIndex;
        int destination = -1;
        float nearestDistance = float.MaxValue;

        foreach (BoardSlot slot in slots)
        {
            if (slot == null)
                continue;

            Transform target = slot.placementPoint != null
                ? slot.placementPoint
                : slot.transform;

            float distance = Vector3.Distance(
                releasePosition, target.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                destination = slot.index;
            }
        }

        if (nearestDistance > moveSnapDistance ||
            destination < 0 || destination >= 9 ||
            !IsAdjacent(origin, destination) ||
            board[destination] == TicTacToePiece.PieceType.X)
        {
            piece.moveOriginIndex = -1;
            SnapPiece(piece, slots[origin]);
            return;
        }

        if (board[destination] == TicTacToePiece.PieceType.O)
        {
            TicTacToePiece captured = boardPieces[destination];

            if (captured != null)
                Destroy(captured.gameObject);

            board[destination] = null;
            boardPieces[destination] = null;

            Debug.Log("X captured an O piece!");
        }

        board[origin] = null;
        boardPieces[origin] = null;

        board[destination] = piece.pieceType;
        boardPieces[destination] = piece;

        piece.moveOriginIndex = -1;
        SnapPiece(piece, slots[destination]);

        Debug.Log("X moved from " + origin + " to " + destination);

        FinishTurn();
    }
}