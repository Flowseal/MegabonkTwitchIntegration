using System.Linq;
using System.Net.Sockets;
using System.IO;
using Thread = Il2CppSystem.Threading.Thread;

namespace TwitchIntegration;

public class Chat
{
    private const string TWITCH_IRC_SERVER = "irc.chat.twitch.tv";
    private const int TWITCH_IRC_PORT = 6667;
    private string TwitchChannel => PluginConfig.TwitchChannel.Value;

    private System.Action<string, string>[] messageCallback = System.Array.Empty<System.Action<string, string>>();
    private readonly System.Random random = new System.Random();

    private TcpClient tcpClient;
    private StreamReader inputStream;
    private StreamWriter outputStream;
    private Thread ircThread;
    private volatile bool isConnected = false;
    private System.DateTime lastConnectionAttempt = System.DateTime.MinValue;
    private const int RECONNECT_DELAY_MS = 5000;

    public void Update()
    {
        if (!isConnected && (System.DateTime.Now - lastConnectionAttempt).TotalMilliseconds > RECONNECT_DELAY_MS)
        {
            StartTwitchConnection();
        }
    }

    public void Reset()
    {
        DisconnectFromTwitch();
    }

    public void RegisterMessageCallback(System.Action<string, string> callback)
    {
        var callbacks = messageCallback.ToList();
        if (!callbacks.Contains(callback))
        {
            callbacks.Add(callback);
            messageCallback = callbacks.ToArray();
        }
    }

    private void StartTwitchConnection()
    {
        if (string.IsNullOrEmpty(TwitchChannel))
        {
            return;
        }

        lastConnectionAttempt = System.DateTime.Now;

        if (ircThread != null && ircThread.IsAlive)
        {
            return;
        }

        ircThread = new Thread((Il2CppSystem.Threading.ThreadStart)ConnectAndReadMessages);
        ircThread.Start();
    }

    private void ConnectAndReadMessages()
    {
        try
        {
            Plugin.Log.LogInfo($"Connecting to Twitch IRC...");

            tcpClient = new TcpClient();
            tcpClient.Connect(TWITCH_IRC_SERVER, TWITCH_IRC_PORT);

            inputStream = new StreamReader(tcpClient.GetStream());
            outputStream = new StreamWriter(tcpClient.GetStream());

            string anonymousNick = $"justinfan{random.Next(10000, 99999)}";
            outputStream.WriteLine($"NICK {anonymousNick}");
            outputStream.WriteLine($"JOIN #{TwitchChannel}");
            outputStream.Flush();

            isConnected = true;
            Plugin.Log.LogInfo($"Connected to #{TwitchChannel}");

            ReadIrcMessages();
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Connection failed: {ex.Message}");
            isConnected = false;

            DisconnectFromTwitch();

            Plugin.Log.LogInfo($"Retrying in {RECONNECT_DELAY_MS / 1000} seconds...");
            Thread.Sleep(RECONNECT_DELAY_MS);
        }
    }

    private void ReadIrcMessages()
    {
        try
        {
            string message;
            while (isConnected && tcpClient.Connected &&
                   (message = inputStream.ReadLine()) != null)
            {
                if (message.StartsWith("PING"))
                {
                    string pongResponse = message.Replace("PING", "PONG");
                    outputStream.WriteLine(pongResponse);
                    outputStream.Flush();
                    continue;
                }

                if (message.Contains("PRIVMSG"))
                {
                    try
                    {
                        int nameStartIndex = message.IndexOf(':') + 1;
                        int nameEndIndex = message.IndexOf('!');
                        string username = message.Substring(nameStartIndex, nameEndIndex - nameStartIndex);

                        int privMsgMark = message.IndexOf("twitch.tv PRIVMSG");
                        int messageStart = message.IndexOf(':', privMsgMark) + 1;
                        string chatMessage = message.Substring(messageStart);

                        Plugin.Log.LogInfo($"Chatter found: {username}");

                        foreach (var callback in messageCallback)
                        {
                            callback(username, chatMessage);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError($"Error parsing message: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            if (isConnected)
            {
                Plugin.Log.LogError($"Error reading IRC messages: {ex.Message}");
            }
        }
        finally
        {
            DisconnectFromTwitch();
        }
    }

    public void DisconnectFromTwitch()
    {
        isConnected = false;

        try
        {
            inputStream?.Dispose();
            outputStream?.Dispose();
            tcpClient?.Close();
        }
        catch (System.Exception) { }
        finally
        {
            inputStream = null;
            outputStream = null;
            tcpClient = null;
        }
    }
}
