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

        public const int DefaultGridSize = 3;

        private const int UpdateTimeoutInSeconds = 30;
        private const string BotUserName = "YOUR_BOT_NAME";
        private const string ApiKey = "YOUR_BOT_API_KEY";

        private static TeleBot _bot;
        private static TeleBot Bot => _bot ?? (_bot = new TeleBot(ApiKey));

        private static IEnumerable<Command> Commands { get; } = new List<Command>
        {
            new Command
            {
                Trigger = new Regex(@"^(?:\/newgame)\s+(?:@(?<BlueUserName>[a-z0-9_]{5,32}))(?:\s+(?<GridSize>[1-9]))?\s*$", RegexOptions.IgnoreCase),
                CallBack = NewGame
            },
            new Command
            {
                Trigger = new Regex(@"^(?:\/play)\s+(?<Row>[1-9][0-9]*)\s+(?<Column>[1-9][0-9]*)\s*$", RegexOptions.IgnoreCase),
                CallBack = Play
            },
            new Command
            {
                Trigger = new Regex(@"^(?:\/endgame)\s*$", RegexOptions.IgnoreCase),
                CallBack = EndGame
            },
            new Command
            {
                Trigger = new Regex(@"^(?:\/gamestate)\s*$"),
                CallBack = GameState
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

                try
                {
                    var updates = Bot.GetUpdates(new GetUpdatesRequest
                    {
                        Offset = lastUpdateId + 1,
                        Timeout = UpdateTimeoutInSeconds
                    });

                    if (updates.Result.Count < 0)
                        continue;

                    foreach (var update in updates.Result)
                    {
                        ProcessUpdate(update);
                        lastUpdateId = update.UpdateId;
                    }
                }
                catch (Exception e)
                {
                    WriteLog("Exception occured!");
                    WriteLog(e.Message);
                }
            }
        }

        private static void ProcessUpdate(UpdateResponse updateResponse)
        {
            if (string.IsNullOrEmpty(updateResponse?.Message?.Text))
                return;

            WriteLog("Processing message...");

            var senderUserName = updateResponse.Message.From.UserName;
            var chatId = updateResponse.Message.Chat.Id;

            var command = Commands.FirstOrDefault(c => c.Check(updateResponse.Message.Text));
            command?.CallBack(senderUserName, command.Matches, chatId, updateResponse);

            WriteLog("Message processed!");
        }

        private static void NewGame(string senderUserName, Match newGame, int chatId, UpdateResponse updateResponse)
        {
            var blueUserName = newGame.Groups["BlueUserName"].Value;
            var boardSize = newGame.Groups["GridSize"].Success
                ? Convert.ToInt32(newGame.Groups["GridSize"].Value) : DefaultGridSize;

            if (blueUserName == senderUserName)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "You can't play with yourself! ( ͡° ͜ʖ ͡°)"
                });

                return;
            }
            else if (blueUserName == BotUserName)
            {
                var winningGameState = new GameState
                {
                    RedUserName = senderUserName,
                    BlueUserName = blueUserName,
                    BoardSize = boardSize
                };

                for (var i = 0; i < boardSize; i++)
                {
                    winningGameState.BoardState[i, i] = State.Blue;
                }

                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = winningGameState.ToString("I win! 😈")
                });

                return;
            }

            var currentGame = GameStates.FirstOrDefault(c =>
                c.HasUser(senderUserName, blueUserName));

            if (currentGame != null)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = $"@{senderUserName} or @{blueUserName} is already playing a game!"
                });

                return;
            }

            var newGameState = new GameState
            {
                RedUserName = senderUserName,
                BlueUserName = blueUserName,
                BoardSize = boardSize
            };

            GameStates.Add(newGameState);

            Bot.SendMessage(new SendMessageRequest
            {
                ChatId = chatId,
                Text = newGameState.ToString()
            });

            WriteLog("User started new game!");
        }

        private static void Play(string senderUserName, Match play, int chatId, UpdateResponse updateResponse)
        {
            var row = Convert.ToInt32(play.Groups["Row"].Value) - 1;
            var column = Convert.ToInt32(play.Groups["Column"].Value) - 1;
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderUserName) && row <= c.BoardSize && column <= c.BoardSize);

            if (currentGame != null &&
                currentGame.BoardState[row, column] == State.None &&
                currentGame.IsTurnUser(senderUserName))
            {
                var playerState = PlayerState.Neutral;

                if (string.Equals(currentGame.RedUserName, senderUserName, StringComparison.OrdinalIgnoreCase))
                {
                    playerState = currentGame.Play(row, column, State.Red);
                }
                else if (string.Equals(currentGame.BlueUserName, senderUserName, StringComparison.OrdinalIgnoreCase))
                {
                    playerState = currentGame.Play(row, column, State.Blue);
                }

                if (playerState == PlayerState.Win || playerState == PlayerState.Draw)
                {
                    switch (playerState)
                    {
                        case PlayerState.Win:
                            Bot.SendMessage(new SendMessageRequest
                            {
                                ChatId = chatId,
                                Text = currentGame.ToString($"@{senderUserName} has won!")
                            });
                            break;
                        case PlayerState.Draw:
                            Bot.SendMessage(new SendMessageRequest
                            {
                                ChatId = chatId,
                                Text = currentGame.ToString("Draw!")
                            });
                            break;
                    }

                    GameStates.RemoveAt(GameStates.IndexOf(currentGame));

                    return;
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
                    Text = $"@{senderUserName} is not in a game!"
                });
            }
            else if (!currentGame.IsTurnUser(senderUserName))
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "It's not your turn!"
                });
            }
            else if (currentGame.BoardState[row, column] != State.None)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "This position is already in use!"
                });
            }

            WriteLog("User played!");
        }

        private static void EndGame(string senderUserName, Match endGame, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderUserName));

            if (currentGame != null)
            {
                GameStates.RemoveAt(GameStates.IndexOf(currentGame));

                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "Game has ben ended!"
                });
            }
            else
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "You're not in a game!"
                });
            }
        }

        private static void GameState(string senderUserName, Match gameState, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderUserName));

            if (currentGame != null)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = currentGame.ToString()
                });
            }
            else
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "You're not in a game!"
                });
            }
        }

        private static void WriteLog(string message)
        {
            foreach (var line in message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
            }
        }
    }
}
