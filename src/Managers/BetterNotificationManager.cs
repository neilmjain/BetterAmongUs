using BetterAmongUs.Data;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using Cpp2IL.Core.Extensions;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Managers;

/// <summary>
/// Manages in-game notifications for BetterAmongUs, including cheat detection alerts and system messages.
/// </summary>
internal static class BetterNotificationManager
{
    internal static GameObject? BAUNotificationManagerObj;
    internal static TextMeshPro? NameText;
    internal static TextMeshPro? TextArea => BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>();
    internal static Dictionary<string, float> NotifyQueue = [];
    internal static float showTime = 0f;
    private static Camera? localCamera;
    internal static bool Notifying = false;

    /// <summary>
    /// Displays a notification message in-game.
    /// </summary>
    /// <param name="text">The text to display in the notification.</param>
    /// <param name="Time">The duration in seconds to show the notification.</param>
    internal static void Notify(string text, float Time = 5f)
    {
        if (!BAUPlugin.BetterNotifications.Value) return;

        if (BAUNotificationManagerObj != null)
        {
            if (Notifying)
            {
                if (text == BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text)
                    return;
                NotifyQueue[text] = Time;
                return;
            }

            showTime = Time;
            BAUNotificationManagerObj.SetActive(true);
            NameText.text = $"<color=#00ff44>{Translator.GetString("SystemNotification")}</color>";
            TextArea.text = text;
            SoundManager.Instance.PlaySound(HudManager.Instance.TaskCompleteSound, false, 1f);
            Notifying = true;
        }
    }



    /// <summary>
    /// Updates the notification manager each frame.
    /// </summary>
    internal static void Update()
    {
        if (BAUNotificationManagerObj != null)
        {
            if (!localCamera)
            {
                if (HudManager.InstanceExists)
                {
                    localCamera = HudManager.Instance.GetComponentInChildren<Camera>();
                }
                else
                {
                    localCamera = Camera.main;
                }
            }

            BAUNotificationManagerObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.Bottom, new Vector3(-1.3f, 0.7f, localCamera.nearClipPlane + 0.1f));

            showTime -= Time.deltaTime;
            if (showTime <= 0f && GameState.IsInGame)
            {
                BAUNotificationManagerObj.transform.Find("Sizer/ChatText (TMP)").GetComponent<TextMeshPro>().text = "";
                BAUNotificationManagerObj.SetActive(false);
                Notifying = false;

                CheckNotifyQueue();
            }

            if (!GameState.IsInGame)
            {
                BAUNotificationManagerObj.SetActive(false);
                showTime = 0f;
            }
        }
    }

    /// <summary>
    /// Checks and processes queued notifications.
    /// </summary>
    private static void CheckNotifyQueue()
    {
        if (NotifyQueue.Any())
        {
            var key = NotifyQueue.Keys.First();
            var value = NotifyQueue[key];
            Notify(key, value);
            NotifyQueue.Remove(key);
        }
    }
}