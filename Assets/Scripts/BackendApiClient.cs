using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class BackendApiClient
{
    private const string ApiBaseUrl = "http://91.160.87.34:18000";
    private const string GameAppId = "safechem-unity-client";
    private const string PublicKeyResourcePath = "Security/game_public_key";

    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly object RsaLock = new object();

    private static RSACryptoServiceProvider _rsa;
    private static bool _initDone;
    private static bool _disabled;

    [Serializable]
    private class CreatePlayerPayload
    {
        public string player_uuid;
        public string pseudo;
        public string sent_at;
    }

    [Serializable]
    private class UpdatePseudoPayload
    {
        public string pseudo;
        public string sent_at;
    }

    [Serializable]
    private class LevelFinishedPayload
    {
        public string player_uuid;
        public int level_index;
        public string level_id;
        public float duration_seconds;
        public int stars;
        public string sent_at;
    }

    public static void CreatePlayer(string playerUuid, string pseudo, DateTime sentAtUtc)
    {
        if (string.IsNullOrWhiteSpace(playerUuid)) return;
        if (!InitializeIfNeeded()) return;

        CreatePlayerPayload payload = new CreatePlayerPayload
        {
            player_uuid = playerUuid.Trim(),
            pseudo = string.IsNullOrWhiteSpace(pseudo) ? "Joueur" : pseudo.Trim(),
            sent_at = ToIso(sentAtUtc)
        };
        FireAndForget(HttpMethod.Post, "/players", JsonUtility.ToJson(payload));
    }

    public static void DeletePlayer(string playerUuid, DateTime sentAtUtc)
    {
        if (string.IsNullOrWhiteSpace(playerUuid)) return;
        if (!InitializeIfNeeded()) return;

        string encodedId = Uri.EscapeDataString(playerUuid.Trim());
        string encodedAt = Uri.EscapeDataString(ToIso(sentAtUtc));
        FireAndForget(HttpMethod.Delete, "/players/" + encodedId + "?sent_at=" + encodedAt, null);
    }

    public static void UpdatePseudo(string playerUuid, string pseudo, DateTime sentAtUtc)
    {
        if (string.IsNullOrWhiteSpace(playerUuid)) return;
        if (!InitializeIfNeeded()) return;

        UpdatePseudoPayload payload = new UpdatePseudoPayload
        {
            pseudo = string.IsNullOrWhiteSpace(pseudo) ? "Joueur" : pseudo.Trim(),
            sent_at = ToIso(sentAtUtc)
        };
        string encodedId = Uri.EscapeDataString(playerUuid.Trim());
        FireAndForget(new HttpMethod("PATCH"), "/players/" + encodedId + "/pseudo", JsonUtility.ToJson(payload));
    }

    public static void SendLevelFinished(
        string playerUuid,
        int levelIndex,
        string levelId,
        float durationSeconds,
        int stars,
        DateTime sentAtUtc
    )
    {
        if (string.IsNullOrWhiteSpace(playerUuid)) return;
        if (!InitializeIfNeeded()) return;

        LevelFinishedPayload payload = new LevelFinishedPayload
        {
            player_uuid = playerUuid.Trim(),
            level_index = Mathf.Max(0, levelIndex),
            level_id = string.IsNullOrWhiteSpace(levelId) ? null : levelId.Trim(),
            duration_seconds = Mathf.Max(0f, durationSeconds),
            stars = Mathf.Clamp(stars, 0, 3),
            sent_at = ToIso(sentAtUtc)
        };
        FireAndForget(HttpMethod.Post, "/levels/finished", JsonUtility.ToJson(payload));
    }

    public static bool InitializeIfNeeded()
    {
        if (_disabled) return false;
        if (_initDone && _rsa != null) return true;

        try
        {
            TextAsset keyAsset = Resources.Load<TextAsset>(PublicKeyResourcePath);
            if (keyAsset == null || string.IsNullOrWhiteSpace(keyAsset.text))
            {
                _disabled = true;
                RuntimeFileLogger.Warn("BackendApiClient", "Public key missing at Resources/" + PublicKeyResourcePath);
                return false;
            }

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(keyAsset.text);
            _rsa = rsa;
            _initDone = true;
            return true;
        }
        catch (Exception exception)
        {
            _disabled = true;
            RuntimeFileLogger.Error("BackendApiClient", "Failed to init RSA public key: " + exception.Message);
            return false;
        }
    }

    private static void FireAndForget(HttpMethod method, string route, string jsonBody)
    {
        _ = SendAsync(method, route, jsonBody);
    }

    private static async Task SendAsync(HttpMethod method, string route, string jsonBody)
    {
        if (_disabled) return;

        try
        {
            string url = ApiBaseUrl.TrimEnd('/') + route;
            using HttpRequestMessage request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Game-Proof", BuildGameProof());
            if (!string.IsNullOrWhiteSpace(jsonBody))
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                RuntimeFileLogger.Warn(
                    "BackendApiClient",
                    "HTTP " + (int)response.StatusCode + " " + method + " " + route + " body=" + body
                );
            }
        }
        catch (Exception exception)
        {
            RuntimeFileLogger.Warn("BackendApiClient", "Request failed: " + method + " " + route + " - " + exception.Message);
        }
    }

    private static string BuildGameProof()
    {
        if (_rsa == null)
            throw new InvalidOperationException("RSA key not initialized.");

        long unixTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string payload = GameAppId + "|" + unixTs;
        byte[] plain = Encoding.UTF8.GetBytes(payload);

        lock (RsaLock)
        {
            byte[] cipher = _rsa.Encrypt(plain, false);
            return Convert.ToBase64String(cipher);
        }
    }

    private static string ToIso(DateTime utc)
    {
        return utc.ToUniversalTime().ToString("o");
    }
}
