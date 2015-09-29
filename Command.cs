using System;
using System.Text.RegularExpressions;
using TeleBotDotNet.Responses.Types;

namespace TeleBotTicTacToe
{
    public class Command
    {
        public Regex Trigger { get; set; }
        public Action<string, Match, int, UpdateResponse> CallBack { get; set; }
        public Match Matches { get; set; }

        public bool Check(string message)
        {
            var match = Trigger.Match(message);
            if (match.Success)
            {
                Matches = match;
            }

            return match.Success;
        }
    }
}
