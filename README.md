# Stock Warden
### Отслеживание биржевых котировок в реальном времени на C# .NET 7 с использованием WebSocket в асинхронности и мультипоточности.
##### На основе открытого WebSocket сервера от tradernet.ru

###### Используемые зависимости (установки не требуют):
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.WebSockets;
