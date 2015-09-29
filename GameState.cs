using System;
using System.Linq;
using System.Text;

namespace TeleBotTicTacToe
{
    public class GameState
    {
        public string RedUser { get; set; }
        public string BlueUser { get; set; }

        public CurrentUser CurrentUser { get; set; }

        public State[,] BoardState { get; } = new State[3, 3];

        public bool HasUser(params string[] usernames)
        {
            return usernames.Any(username =>
                string.Equals(RedUser, username, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(BlueUser, username, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsTurnUser(string username)
        {
            switch (CurrentUser)
            {
                case CurrentUser.Red:
                    return string.Equals(RedUser, username, StringComparison.OrdinalIgnoreCase);

                case CurrentUser.Blue:
                    return string.Equals(BlueUser, username, StringComparison.OrdinalIgnoreCase);

                default:
                    return false;
            }
        }

        public void SwitchTurn()
        {
            switch (CurrentUser)
            {
                case CurrentUser.Red:
                    CurrentUser = CurrentUser.Blue;
                    break;

                case CurrentUser.Blue:
                    CurrentUser = CurrentUser.Red;
                    break;
            }
        }

        public override string ToString()
        {
            var data = new StringBuilder();
            data.AppendLine($"{Program.RedField} = {RedUser}");
            data.AppendLine($"{Program.BlueField} = {BlueUser}");

            for (var i = 0; i < 3; i++)
            {
                data.AppendLine();
                for (var j = 0; j < 3; j++)
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
