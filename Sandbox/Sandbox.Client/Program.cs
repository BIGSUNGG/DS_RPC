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

// 6) 제네릭 ① — 반환 전용: 명시 타입 인자. 허용 집합은 [GenericProcedure] 선언(int/string).
Console.WriteLine($"[client] GetConfig<int>() -> {await hub.GetConfigAsync<int>()}");
Console.WriteLine($"[client] GetConfig<string>() -> {await hub.GetConfigAsync<string>()}");

// 7) 제네릭 ② — 매개변수: 타입 인자 없이 일반 호출처럼(T 추론).
Console.WriteLine($"[client] Describe(7) -> {await hub.DescribeAsync(7)}");
Console.WriteLine($"[client] Describe(\"gg\") -> {await hub.DescribeAsync("gg")}");

// 8) 제네릭 ③ — 복합 다중 슬롯(데카르트 곱 조합).
Console.WriteLine($"[client] Blend<int, float, Player>(1.5f, player) -> {await hub.BlendAsync<int, float, Player>(1.5f, new Player { Id = 1, Name = "Hong" })}");
Console.WriteLine($"[client] Blend<string, double, ChatLine>(2.5, chat) -> {await hub.BlendAsync<string, double, ChatLine>(2.5, new ShoutChatLine { Text = "hi" })}");

// 9) 제네릭 ④ — [GenericMessage] 파라미터: T 허용 집합을 GiftBox 구성 선언에서 상속.
await hub.UnwrapAsync(new GiftBox<ChatLine> { Gift = new ShoutChatLine { Text = "boxed" } });
await hub.UnwrapAsync(new GiftBox<Token> { Gift = new Token { Value = 7 } });
Console.WriteLine("[client] Unwrap(GiftBox<ChatLine>/GiftBox<Token>) sent");

// 10) 피어가 보낸 one-way 을 수신 구현이 처리했는지 확인하려면 잠시 기다린다.
await Task.Delay(500);

hub.Disconnect();
Console.WriteLine("[client] disconnected");
