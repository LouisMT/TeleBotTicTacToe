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
        private const string ApiKey = "109148704:AAFAS64QRgfev6iWoGkvhpLrmMTjuyi7X5g";

        private static TeleBot _bot;
        private static TeleBot Bot => _bot ?? (_bot = new TeleBot(ApiKey));

        private static IEnumerable<Command> Commands { get; } = new List<Command>
        {
            new Command
            {
                Trigger = new Regex(@"^(?:\/newgame)\s+(?:@(?<BlueUser>[a-z0-9]+))(?:\s+(?<GridSize>[1-9]))?\s*$", RegexOptions.IgnoreCase),
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
            var x = new GameState
            {
                BoardSize = 9
            };
            x.BoardState[0, 0] = State.Red;
            x.BoardState[1, 0] = State.Red;
            x.BoardState[1, 1] = State.Red;
            x.BoardState[1, 2] = State.Red;
            x.BoardState[2, 2] = State.Red;
            x.BoardState[3, 3] = State.Red;
            x.BoardState[4, 4] = State.Red;
            x.BoardState[5, 5] = State.Red;
            x.BoardState[6, 6] = State.Red;
            x.BoardState[6, 2] = State.Blue;
            x.BoardState[6, 4] = State.Blue;
            x.BoardState[7, 0] = State.Blue;
            x.BoardState[0, 7] = State.Blue;
            x.BoardState[1, 7] = State.Blue;
            x.BoardState[2, 7] = State.Blue;
            x.BoardState[3, 7] = State.Blue;
            x.BoardState[4, 7] = State.Blue;
            x.BoardState[6, 7] = State.Blue;
            x.BoardState[7, 7] = State.Blue;
            x.BoardState[8, 7] = State.Blue;
            x.Play(5, 7, State.Blue);

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
            var blueUser = newGame.Groups["BlueUser"].Value;

            if (blueUser == senderName)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "You can't play with youself! ( ͡° ͜ʖ ͡°)"
                });

                return;
            }

            var currentGame = GameStates.FirstOrDefault(c =>
                c.HasUser(senderName, blueUser));

            if (currentGame != null)
            {
                Bot.SendMessage(new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = $"@{senderName} or @{blueUser} is already playing a game!"
                });
            }

            var newGameState = new GameState
            {
                RedUsername = senderName,
                BlueUsername = blueUser,
                BoardSize = newGame.Groups["GridSize"].Success ?
                    Convert.ToInt32(newGame.Groups["GridSize"].Value) : DefaultGridSize
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
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderName) && row <= c.BoardSize && column <= c.BoardSize);

            if (currentGame != null &&
                currentGame.BoardState[row, column] == State.None &&
                currentGame.IsTurnUser(senderName))
            {
                var playerState = PlayerState.Neutral;

                if (string.Equals(currentGame.RedUsername, senderName, StringComparison.OrdinalIgnoreCase))
                {
                    playerState = currentGame.Play(row, column, State.Red);
                }
                else if (string.Equals(currentGame.BlueUsername, senderName, StringComparison.OrdinalIgnoreCase))
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
                                Text = currentGame.ToString($"@{senderName} has won!")
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
                    Text = $"@{senderName} is not in a game!"
                });
            }
            else if (!currentGame.IsTurnUser(senderName))
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

        private static void EndGame(string senderName, Match endGame, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderName));

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

        private static void GameState(string senderName, Match gameState, int chatId, UpdateResponse updateResponse)
        {
            var currentGame = GameStates.FirstOrDefault(c => c.HasUser(senderName));

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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
