using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace StockWarden
{
    class Program
    {
        private static string WebSocketsURL = "wss://wss.tradernet.ru";
        static List<string> TickersToWatchChanges { get; set; }
        static Dictionary<string, string> LastKnownTimes { get; set; } = new Dictionary<string, string>();
        static Dictionary<string, string> LastKnownPrices { get; set; } = new Dictionary<string, string>();
        static Dictionary<string, string> LastKnownSizes { get; set; } = new Dictionary<string, string>();
        static Dictionary<string, string> LastKnownPercentChanges { get; set; } = new Dictionary<string, string>();
        static Dictionary<string, string> LastKnownPointChanges { get; set; } = new Dictionary<string, string>();

        private static async Task ConnectWebSocketAsync(CancellationToken cancellationToken)
        {
            using (ClientWebSocket webSocket = new ClientWebSocket())
            {
                Uri serverUri = new Uri(WebSocketsURL);
                try
                {
                    await webSocket.ConnectAsync(serverUri, cancellationToken).ConfigureAwait(false);
                    Console.WriteLine("Connected to the WebSocket server.");

                    var receiveTask = Task.Run(() => ReceiveMessages(webSocket, cancellationToken));

                    await SubscribeToQuotes(webSocket, cancellationToken).ConfigureAwait(false);

                    await Task.WhenAny(receiveTask, Task.Run(() => Console.ReadLine(), cancellationToken)).ConfigureAwait(false);
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        private static async Task SubscribeToQuotes(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            string message = JsonSerializer.Serialize(new object[] { "quotes", TickersToWatchChanges });
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            Console.WriteLine("Subscribed to quotes.");
        }

        private static async Task ReceiveMessages(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received message: {message}");
                        HandleMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("WebSocket closed.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocket error while receiving: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while receiving: {ex.Message}");
                    break;
                }
            }
        }

        private static void HandleMessage(string message)
        {
            try
            {
                var parsedMessage = JsonSerializer.Deserialize<object[]>(message);
                var eventType = parsedMessage?[0]?.ToString();
                var data = parsedMessage?[1];

                if (eventType == "q")
                {
                    Console.WriteLine("Handling 'q' event...");
                    UpdateWatcher(data);
                }
                else if (eventType == "userData")
                {
                    // Если понадобится
                }
                else
                {
                    Console.WriteLine("Unknown event type received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
            }
        }

        private static void UpdateWatcher(object data)
        {
            try
            {
                var quotesData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data?.ToString());
                Console.WriteLine("Updating watcher with new data...");

                Console.Clear();

                DisplayQuotesData(quotesData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating watcher: {ex.Message}");
            }
        }

        private static void DisplayQuotesData(Dictionary<string, JsonElement> quotesData)
        {
            Console.WriteLine("Displaying quotes data...");

            PrintIfExists(quotesData, "name", "Название бумаги");
            PrintIfExists(quotesData, "name2", "Латинское название бумаги");
            PrintIfExists(quotesData, "bbp", "Лучший бид");
            PrintIfExists(quotesData, "bbc", "Обозначение изменения лучшего бида");
            PrintIfExists(quotesData, "bbs", "Количество (сайз) лучшего бида");
            PrintIfExists(quotesData, "bbf", "Объем лучшего бида");
            PrintIfExists(quotesData, "bap", "Лучшее предложение");
            PrintIfExists(quotesData, "bac", "Обозначение изменения лучшего предложения");
            PrintIfExists(quotesData, "bas", "Количество (сайз) лучшего предложения");
            PrintIfExists(quotesData, "baf", "Объем лучшего предложения");
            PrintIfExists(quotesData, "ltc", "Обозначение изменения цены последней сделки");
            PrintIfExists(quotesData, "vol", "Объём торгов за день в штуках");
            PrintIfExists(quotesData, "vlt", "Объём торгов за день в валюте");
            PrintIfExists(quotesData, "yld", "Доходность к погашению");
            PrintIfExists(quotesData, "acd", "Накопленный купонный доход (НКД)");
            PrintIfExists(quotesData, "fv", "Номинал");
            PrintIfExists(quotesData, "trades", "Количество сделок");
            PrintIfExists(quotesData, "mtd", "Дата погашения");
            PrintIfExists(quotesData, "cpn", "Купон в валюте");
            PrintIfExists(quotesData, "cpp", "Купонный период (в днях)");
            PrintIfExists(quotesData, "ncd", "Дата следующего купона");
            PrintIfExists(quotesData, "ncp", "Дата последнего купона");
            PrintIfExists(quotesData, "dpd", "ГО покупки");
            PrintIfExists(quotesData, "dps", "ГО продажи");
            PrintIfExists(quotesData, "min_step", "Минимальный шаг цены");
            PrintIfExists(quotesData, "step_price", "Шаг цены");
            PrintIfExists(quotesData, "ltr", "Биржа последней сделки");
            PrintIfExists(quotesData, "op", "Цена открытия в текущей торговой сессии");
            PrintIfExists(quotesData, "pp", "Цена предыдущего закрытия");
            PrintIfExists(quotesData, "mintp", "Минимальная цена сделки за день");
            PrintIfExists(quotesData, "maxtp", "Максимальная цена сделки за день");
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------");
            PrintLastPrice(quotesData, "c", "Тикер", "ltp", "Цена", LastKnownPrices);
            PrintLast(quotesData, "c", "Тикер", "lts", "Количество (сайз)", LastKnownSizes);
            PrintLast(quotesData, "c", "Тикер", "pcp", "Изменение в процентах", LastKnownPercentChanges);
            PrintLast(quotesData, "c", "Тикер", "chg", "Изменение в пунктах относительно цены закрытия предыдущей торговой сессии", LastKnownPointChanges);
            PrintLast(quotesData, "c", "Тикер", "ltt", "Время", LastKnownTimes);
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine();
        }

        private static void PrintIfExists(Dictionary<string, JsonElement> data, string key, string label)
        {
            if (data.ContainsKey(key))
            {
                try
                {
                    var value = data[key];
                    Console.WriteLine($"{key} {label}: {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error printing {key}: {ex.Message}");
                }
            }
        }

        private static void PrintLast(Dictionary<string, JsonElement> data, string tickerKey, string tickerLabel, string valueKey, string valueLabel, Dictionary<string, string> fallbackDictionary)
        {
            string ticker = data.ContainsKey(tickerKey) ? data[tickerKey].ToString() : "Unknown";
            string value = data.ContainsKey(valueKey) ? data[valueKey].ToString() : null;

            if (!string.IsNullOrEmpty(value))
            {
                fallbackDictionary[ticker] = value;
            }
            else if (fallbackDictionary.ContainsKey(ticker))
            {
                value = fallbackDictionary[ticker];
            }
            else
            {
                value = "N/A";
            }
            Console.WriteLine($"{valueKey} {valueLabel}: {value}");
        }

        private static void PrintLastPrice(Dictionary<string, JsonElement> data, string tickerKey, string tickerLabel, string valueKey, string valueLabel, Dictionary<string, string> fallbackDictionary)
        {
            string ticker = data.ContainsKey(tickerKey) ? data[tickerKey].ToString() : "Unknown";
            string value = data.ContainsKey(valueKey) ? data[valueKey].ToString() : null;

            if (!string.IsNullOrEmpty(value))
            {
                fallbackDictionary[ticker] = value;
            }
            else if (fallbackDictionary.ContainsKey(ticker))
            {
                value = fallbackDictionary[ticker];
            }
            else
            {
                value = "N/A";
            }
            Console.WriteLine($"{tickerKey}   {tickerLabel}: {ticker}");
            Console.WriteLine($"{valueKey} {valueLabel}: {value}");
        }

        static async Task Main(string[] args)
        {
            Program.TickersToWatchChanges = new List<string>();

            Console.WriteLine("Введите индексы нажимая enter после каждого, как только закончите, ничего не вводите и нажмите enter:");
            string Index;
            while (true)
            {
                Index = Console.ReadLine();
                if (string.IsNullOrEmpty(Index))
                {
                    break;
                }
                else
                {
                    if (Index.Contains("500") == true)
                    {
                        Program.TickersToWatchChanges.Add("SP500.IDX");
                    }
                    else
                    {
                        Program.TickersToWatchChanges.Add(Index);
                    }
                }
            }
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                await ConnectWebSocketAsync(cts.Token).ConfigureAwait(false);
            }
        }
    }
}
