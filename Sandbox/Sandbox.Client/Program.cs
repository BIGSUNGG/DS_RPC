using Sandbox.Client;
using Sandbox.Contracts;

const string ConnectionKey = "sandbox-key";

using var hub = await GameClientHub.ConnectAsync("127.0.0.1", 9050, ConnectionKey);
Console.WriteLine("[client] connected");

// 1) 기본 ReliableOrdered 호출
Console.WriteLine($"[client] Add(2, 3) -> {await hub.AddAsync(2, 3)}");

// 2) 메시지 타입 매개변수·반환
PlayerJoined joined = await hub.JoinAsync(new Player { Id = 7, Name = "Hong" });
Console.WriteLine($"[client] Join -> playerId={joined.PlayerId} roomId={joined.RoomId}");

// 3) 전송 방식 오버라이드(Sequenced) — 속성 하나로 호출 방식이 바뀐다
await hub.SetPositionAsync(7, 1.25f, -3.5f);
Console.WriteLine("[client] SetPosition(Sequenced) sent");

// 4) OneWay — 응답 없이 전달
await hub.LogChatAsync("hello from sandbox");
Console.WriteLine("[client] LogChat(OneWay) sent");

// 5) 그룹 다형성 — 파생 타입을 루트 타입 계약으로 보낸다
await hub.ChatMessageAsync(new ShoutChatLine { Text = "gg" });
Console.WriteLine("[client] ChatMessage(OneWay, 실제 타입 ShoutChatLine) sent");

// 6) 피어가 보낸 one-way 을 수신 구현이 처리했는지 확인하려면 잠시 기다린다.
await Task.Delay(500);

hub.Disconnect();
Console.WriteLine("[client] disconnected");
