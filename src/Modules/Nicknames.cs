using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Enemies;

namespace TwitchIntegration;

public class Nicknames
{
    private float NicknameSize => PluginConfig.NicknamesSize.Value;
    private int MaxChatters => PluginConfig.NicknamesMaxChatters.Value;

    private float lastEnemyFindTime = 0f;
    private const float ENEMY_FIND_DELAY = 0.25f;

    private readonly System.Random random = new();
    private string[] chattersBuffer;
    private int currentIndex = 0;
    private int totalChatters = 0;
    private readonly object bufferLock = new object();

    private readonly HashSet<EnemyData> enemiesSet = new();
    private class EnemyData
    {
        public Enemy enemy;
        public string name;
        public TextMeshProUGUI usernameText;
        public GameObject canvas;
    }

    public void Update()
    {
        if (totalChatters > 0)
        {
            FindEnemies();
        }

        ClearDeadEnemies();
        UpdatePositions();
    }

    public void Reset()
    {
        lock (bufferLock)
        {
            chattersBuffer = new string[MaxChatters];
            currentIndex = 0;
            totalChatters = 0;
        }

        foreach (var enemyData in enemiesSet)
        {
            GameObject.Destroy(enemyData.canvas);
        }
        enemiesSet.Clear();
    }

    public void AddChatter(string username, string _)
    {
        lock (bufferLock)
        {
            chattersBuffer[currentIndex] = username;
            currentIndex = (currentIndex + 1) % MaxChatters;

            if (totalChatters < MaxChatters)
            {
                totalChatters++;
            }
        }
    }

    private void FindEnemies()
    {
        if (Time.time - lastEnemyFindTime < ENEMY_FIND_DELAY)
        {
            return;
        }

        Enemy[] enemies = GameObject.FindObjectsOfType<Enemy>().Where(e => e.hp > 0 && !e.IsBoss()).ToArray();
        lastEnemyFindTime = Time.time;

        foreach (Enemy enemy in enemies)
        {
            if (IsEnemyAlreadyTracked(enemy))
            {
                continue;
            }

            int randomChatterIndex = random.Next(totalChatters);
            string chatterName = chattersBuffer[randomChatterIndex];

            EnemyData enemyData = CreateEnemyData(enemy, chatterName);
            enemiesSet.Add(enemyData);
        }

    }

    private void ClearDeadEnemies()
    {
        var toRemove = new List<EnemyData>();

        foreach (var enemyData in enemiesSet)
        {
            if (enemyData.enemy == null || enemyData.enemy.hp <= 0 || enemyData.enemy.IsDeadOrDyingNextFrame())
            {
                GameObject.Destroy(enemyData.canvas);
                toRemove.Add(enemyData);
            }
        }

        foreach (var item in toRemove)
        {
            enemiesSet.Remove(item);
        }
    }

    private void UpdatePositions()
    {
        foreach (var enemyData in enemiesSet)
        {
            float newAlpha = Mathf.Lerp(enemyData.usernameText.color.a, 0.5f, Time.deltaTime * 2f);
            enemyData.usernameText.color = new Color(1f, 1f, 1f, newAlpha);

            Vector3 bossPosition = enemyData.enemy.GetHeadPosition();
            enemyData.canvas.transform.position = bossPosition;

            if (Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                enemyData.canvas.transform.LookAt(cameraPosition);
                enemyData.canvas.transform.Rotate(0, 180, 0);
            }
        }
    }

    private bool IsEnemyAlreadyTracked(Enemy enemy)
    {
        foreach (var ed in enemiesSet)
        {
            if (ed.enemy == enemy) return true;
        }
        return false;
    }

    private EnemyData CreateEnemyData(Enemy enemy, string username)
    {
        GameObject canvasObject = new GameObject($"TwitchUsername");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(NicknameSize, 0);

        GameObject textObject = new GameObject("MessageText");
        textObject.transform.SetParent(canvasObject.transform, false);

        TextMeshProUGUI messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.text = username;
        messageText.fontSize = NicknameSize / 20f;
        messageText.color = new Color(1f, 1f, 1f, 0.0f);
        messageText.alignment = TextAlignmentOptions.Center;

        return new EnemyData
        {
            enemy = enemy,
            name = username,
            usernameText = messageText,
            canvas = canvasObject
        };
    }
}
