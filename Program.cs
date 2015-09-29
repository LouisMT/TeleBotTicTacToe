using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TeleBotDotNet;
using TeleBotDotNet.Requests.Methods;
using TeleBotDotNet.Responses.Types;

namespace TeleBotTicTacToe
{
    class Program
    {
        public const string EmptyField = "⚪️";
        public const string RedField = "🔴";
        public const string BlueField = "🔵";

        private const int UpdateTimeoutInSeconds = 30;
        private const string ApiKey = "YOUR_API_KEY_HERE";

        private static TeleBot _bot;
        private static TeleBot Bot => _bot ?? (_bot = new TeleBot(ApiKey));

        private static IEnumerable<Command> Commands { get; } = new List<Command>
        {
            new Command
            {
                Trigger = new Regex(@"^(?:\/newgame)\s+(?:@(?<BlueUser>[a-z0-9]+))\s*$", RegexOptions.IgnoreCase),
                CallBack = NewGame
            },
            new Command
            {
                Trigger = new Regex(@"^(?:\/play)\s+(?<Row>[1-3])\s+(?<Column>[1-3])\s*$", RegexOptions.IgnoreCase),
                CallBack = Play
            },
            new Command
            {
                Trigger = new Regex(@"^(?:\/endgame)\s*$", RegexOptions.IgnoreCase),
                CallBack = EndGame
            }
        };
        private static List<GameState> GameStates { get; } = new List<GameState>();

        static void Main()
        {
            Console.Title = "TeleBotTicTacToe";
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            var lastUpdateId = 0;

            while (true)
            {
                WriteLog($"Getting updates (offset {lastUpdateId + 1})...");

                var updates = Bot.GetUpdates(new GetUpdatesRequest
                {
                    Offset = lastUpdateId + 1,
                    Timeout = UpdateTimeoutInSeconds
                });

                if (updates.Result.Count < 0)
                    continue;

                foreach (var update in updates.Result)
                {
                    try
                    {
                        ProcessUpdate(update);
                        lastUpdateId = update.UpdateId;
                    }
                    catch (Exception e)
                    {
                        WriteLog(e.Message);
                    }
                }
            }
        }

        private static void ProcessUpdate(UpdateResponse updateResponse)
        {
            if (string.IsNullOrEmpty(updateResponse?.Message?.Text))
                return;

            WriteLog("Processing message...");

            var senderName = updateResponse.Message.From.UserName;
            var chatId = updateResponse.Message?.UserChat?.Id ?? updateResponse.Message.GroupChat.Id;

            var command = Commands.FirstOrDefault(c => c.Check(updateResponse.Message.Text));
            command?.CallBack(senderName, command.Matches, chatId, updateResponse);

            WriteLog("Message processed!");
        }

        private static void NewGame(string senderName, Match newGame, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c =>
                c.HasUser(senderName, newGame.Groups["BlueUser"].Value));

            if (currentGame != null)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "You or your opponent is already playing a game!"
                });
            }

            var newGameState = new GameState
            {
                RedUser = senderName,
                BlueUser = newGame.Groups["BlueUser"].Value
            };

            GameStates.Add(newGameState);

            Bot.SendMessage(new SendMessageRequest
            {
                ChatId = chatId,
                Text = newGameState.ToString()
            });

            WriteLog("User started new game!");
        }

        private static void Play(string senderName, Match play, int chatId, UpdateResponse updateResponse)
        {
            var row = Convert.ToInt32(play.Groups["Row"].Value) - 1;
            var column = Convert.ToInt32(play.Groups["Column"].Value) - 1;
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderName));

            if (currentGame != null &&
                currentGame.BoardState[row, column] == State.None &&
                currentGame.IsTurnUser(senderName))
            {
                if (string.Equals(currentGame.RedUser, senderName, StringComparison.OrdinalIgnoreCase))
                {
                    currentGame.BoardState[row, column] = State.Red;
                }
                else if (string.Equals(currentGame.BlueUser, senderName, StringComparison.OrdinalIgnoreCase))
                {
                    currentGame.BoardState[row, column] = State.Blue;
                }

                currentGame.SwitchTurn();

                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = currentGame.ToString()
                });
            }
            else if (currentGame == null)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "You're not in a game!"
                });
            }
            else if (currentGame.IsTurnUser(senderName))
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "It's not your turn!"
                });
            }
            else if (currentGame.BoardState[row, column] != State.None)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "This position is already in use!"
                });
            }

            WriteLog("User played!");
        }

        private static void EndGame(string senderName, Match endGame, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderName));

            if (currentGame != null)
            {
                GameStates.RemoveAt(GameStates.IndexOf(currentGame));

                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "Game has ben ended!"
                });
            }
            else
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    ReplyToMessageId = updateResponse.Message.MessageId,
                    Text = "You're not in a game!"
                });
            }
        }

        private static void WriteLog(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
