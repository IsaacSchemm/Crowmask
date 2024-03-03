var client = new HttpClient();
var tokens = await Crowmask.ATProto.Auth.createSessionAsync(client, "bsky.social", "", "");

var n1 = await Crowmask.ATProto.Notifications.listNotificationsAsync(client, "bsky.social", tokens);
Console.WriteLine(n1);

var n2 = await Crowmask.ATProto.Notifications.listNotificationsAsync(client, "bsky.social", tokens);
Console.WriteLine(n2);
