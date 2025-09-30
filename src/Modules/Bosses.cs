using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Enemies;

namespace TwitchIntegration;

public class Bosses
{
    private float DialogSize => PluginConfig.BossesDialogSize.Value;
    private int MaxChatters => PluginConfig.BossesMaxChatters.Value;

    private float lastBossFindTime = 0f;
    private const float BOSS_FIND_DELAY = 0.5f;

    private readonly System.Random random = new();
    private readonly Dictionary<string, BossData> userToBoss = new();
    private Dictionary<Enemy, string> enemyToUser = new();

    private readonly HashSet<string> chattersSet = new();
    private string[] chattersBuffer;
    private int currentIndex = 0;
    private int totalChatters = 0;
    private readonly object bufferLock = new();

    private class BossData
    {
        public TargetOfInterestPrefab target;
        public EnemyData enemyData;
        public string name;

        public GameObject dialogCanvas;
        public TextMeshProUGUI messageText;
        public System.DateTime lastMessageTime;
        public float hideTimer;
        public bool isFading;
    }

    public void Update()
    {
        if (totalChatters > 0)
        {
            FindBosses();
        }

        UpdateBossesData();
    }

    public void Reset()
    {
        foreach (var kvp in userToBoss)
        {
            GameObject.Destroy(kvp.Value.dialogCanvas);
        }

        userToBoss.Clear();
        enemyToUser.Clear();

        lock (bufferLock)
        {
            chattersSet.Clear();
            chattersBuffer = new string[MaxChatters];
            currentIndex = 0;
            totalChatters = 0;
        }
    }

    public void ProcessNewChatMessage(string username, string message)
    {
        AddChatter(username);
        ProcessMessage(username, message);
    }

    private void AddChatter(string username)
    {
        lock (bufferLock)
        {
            if (chattersSet.Contains(username)
                || userToBoss.ContainsKey(username))
            {
                return;
            }

            if (totalChatters >= MaxChatters)
            {
                string oldChatter = chattersBuffer[currentIndex];
                chattersSet.Remove(oldChatter);
            }

            chattersBuffer[currentIndex] = username;
            chattersSet.Add(username);
            currentIndex = (currentIndex + 1) % MaxChatters;

            if (totalChatters < MaxChatters)
            {
                totalChatters++;
            }
        }
    }

    private void ProcessMessage(string username, string message)
    {
        if (userToBoss.TryGetValue(username, out BossData bossData))
        {
            bossData.messageText.text = message;
            bossData.lastMessageTime = System.DateTime.Now;
            bossData.hideTimer = 15f;

            bossData.isFading = false;
            bossData.messageText.alpha = 1f;

            Plugin.Log.LogInfo($"Showing dialog for {username}: {message}");
        }
    }

    private void FindBosses()
    {
        if (Time.time - lastBossFindTime < BOSS_FIND_DELAY)
        {
            return;
        }

        var targets = GameObject.FindObjectsOfType<TargetOfInterestPrefab>()
                    .Where(t => t.enemy?.IsBoss() == true && !enemyToUser.ContainsKey(t.enemy));

        lastBossFindTime = Time.time;

        foreach (TargetOfInterestPrefab target in targets)
        {
            string chatterName = PopRandomChatter();
            if (chatterName == null)
            {
                break;
            }

            enemyToUser[target.enemy] = chatterName;
            userToBoss[chatterName] = CreateBossData(target, chatterName);

            Plugin.Log.LogInfo($"Boss renamed to: {chatterName}");
        }
    }

    private void UpdateBossesData()
    {
        foreach (var kvp in userToBoss.ToList())
        {
            string username = kvp.Key;
            BossData bossData = kvp.Value;

            if (bossData.target.enemy == null
                || bossData.target.enemy.hp <= 0
                || bossData.target.enemy.IsDeadOrDyingNextFrame()
                || bossData.target.enemy.IsBoss() == false
                || bossData.enemyData != bossData.target.enemy.enemyData)
            {
                GameObject.Destroy(bossData.dialogCanvas);
                userToBoss.Remove(username);
                chattersSet.Remove(username);
                enemyToUser = enemyToUser.Where(e => e.Value != username).ToDictionary(e => e.Key, e => e.Value);
                AddChatter(username);
                Plugin.Log.LogInfo($"Boss {username} removed (dead or null)");
                continue;
            }

            if (bossData.target.t_name.text != bossData.name)
            {
                bossData.target.t_name.text = bossData.name;
            }

            Vector3 bossPosition = bossData.target.enemy.statusSymbols.boss.transform.position;
            bossData.dialogCanvas.transform.position = bossPosition;

            if (Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                bossData.dialogCanvas.transform.LookAt(cameraPosition);
                bossData.dialogCanvas.transform.Rotate(0, 180, 0);
            }

            if (bossData.hideTimer > 0)
            {
                bossData.hideTimer -= Time.deltaTime;

                if (bossData.hideTimer <= 2f && !bossData.isFading)
                {
                    bossData.isFading = true;
                }

                if (bossData.isFading)
                {
                    float fadeProgress = bossData.hideTimer / 2f;
                    float alpha = Mathf.Lerp(0f, 1f, fadeProgress);
                    bossData.messageText.alpha = alpha;
                }

                if (bossData.hideTimer <= 0)
                {
                    bossData.isFading = false;
                    bossData.messageText.alpha = 0f;
                    Plugin.Log.LogInfo($"Hidden dialog for {username} after timeout");
                }
            }
        }
    }

    private string PopRandomChatter()
    {
        lock (bufferLock)
        {
            if (totalChatters == 0)
            {
                return null;
            }
            
            int randomChatterIndex = random.Next(totalChatters);
            string chatterName = chattersBuffer[randomChatterIndex];

            if (totalChatters > 1)
            {
                chattersBuffer[randomChatterIndex] = chattersBuffer[totalChatters - 1];
            }
            else
            {
                chattersBuffer[randomChatterIndex] = null;
            }

            totalChatters--;
            totalChatters = Mathf.Clamp(totalChatters, 0, MaxChatters);
            currentIndex = totalChatters % MaxChatters;

            return chatterName;
        }
    }

    private BossData CreateBossData(TargetOfInterestPrefab target, string username)
    {
        GameObject canvasObject = new GameObject($"BossDialog_{username}");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(DialogSize, 0);

        GameObject nicknameObject = new GameObject("NicknameText");
        nicknameObject.transform.SetParent(canvasObject.transform, false);

        TextMeshProUGUI nicknameText = nicknameObject.AddComponent<TextMeshProUGUI>();
        nicknameText.text = username;
        nicknameText.fontSize = DialogSize / 15f;
        nicknameText.color = new Color(0.7f, 0.2f, 0.2f, 1f);
        nicknameText.alignment = TextAlignmentOptions.Center;
        nicknameText.margin = new Vector4(0, 0, 0, -(1f + DialogSize / 15f));

        GameObject textObject = new GameObject("MessageText");
        textObject.transform.SetParent(canvasObject.transform, false); TextMeshProUGUI messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.text = "";
        messageText.fontSize = DialogSize / 20f;
        messageText.color = Color.white;
        messageText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        messageText.verticalAlignment = VerticalAlignmentOptions.Bottom;
        messageText.enableWordWrapping = true;
        messageText.margin = new Vector4(0, 0, 0, DialogSize / 20f);

        ContentSizeFitter sizeFitter = textObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(0, 0);

        return new BossData
        {
            target = target,
            enemyData = target.enemy?.enemyData,
            name = username,
            dialogCanvas = canvasObject,
            messageText = messageText,
            lastMessageTime = System.DateTime.MinValue,
            hideTimer = 0f,
            isFading = false,
        };
    }
}
