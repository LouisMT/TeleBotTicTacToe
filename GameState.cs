using System;
using System.Linq;
using System.Text;

namespace TeleBotTicTacToe
{
    public class GameState
    {
        public string RedUserName { get; set; }
        public string BlueUserName { get; set; }
        public Player CurrentPlayerTurn { get; set; }

        private int MoveCount { get; set; }
        public int BoardSize { get; set; }

        private State[,] _boardState;
        public State[,] BoardState => _boardState ?? (_boardState = new State[BoardSize, BoardSize]);

        public bool HasUser(params string[] userNames)
        {
            return userNames.Any(userName =>
                string.Equals(RedUserName, userName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(BlueUserName, userName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsTurnUser(string userName)
        {
            switch (CurrentPlayerTurn)
            {
                case Player.Red:
                    return string.Equals(RedUserName, userName, StringComparison.OrdinalIgnoreCase);

                case Player.Blue:
                    return string.Equals(BlueUserName, userName, StringComparison.OrdinalIgnoreCase);

                default:
                    return false;
            }
        }

        public void SwitchTurn()
        {
            switch (CurrentPlayerTurn)
            {
                case Player.Red:
                    CurrentPlayerTurn = Player.Blue;
                    break;

                case Player.Blue:
                    CurrentPlayerTurn = Player.Red;
                    break;
            }
        }

        public PlayerState Play(int x, int y, State state)
        {
            // Make the move
            BoardState[x, y] = state;
            MoveCount++;

            // Assigning (self OR check) so whenever one of the
            // items is set to true it won't change back to false
            var fails = new bool[4];

            for (var i = 0; i < BoardSize; i++)
            {
                // Check current row
                fails[0] = fails[0] || BoardState[x, i] != state;

                // Check current column
                fails[1] = fails[1] || BoardState[i, y] != state;

                // Move is in diagonal, check current diagonal
                fails[2] = fails[2] || x != y || BoardState[i, i] != state;

                // Check current anti diagonal
                fails[3] = fails[3] || BoardState[i, (BoardSize - 1) - i] != state;

                // All checks failed, so no winning combination
                if (fails.All(f => f))
                    break;

                if (i == BoardSize - 1)
                    return PlayerState.Win;
            }

            return MoveCount == BoardSize * BoardSize ?
                PlayerState.Draw : PlayerState.Neutral;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(string additionalText)
        {
            var data = new StringBuilder();
            data.AppendLine($"{Program.RedField} = @{RedUserName}");
            data.AppendLine($"{Program.BlueField} = @{BlueUserName}");

            if (!string.IsNullOrEmpty(additionalText))
            {
                data.AppendLine();
                data.AppendLine(additionalText);
            }

            for (var i = 0; i < BoardSize; i++)
            {
                data.AppendLine();
                for (var j = 0; j < BoardSize; j++)
                {
                    switch (BoardState[i, j])
                    {
                        case State.None:
                            data.Append(Program.EmptyField);
                            break;
                        case State.Red:
                            data.Append(Program.RedField);
                            break;
                        case State.Blue:
                            data.Append(Program.BlueField);
                            break;
                    }
                }
            }

            return data.ToString();
        }
    }
}
