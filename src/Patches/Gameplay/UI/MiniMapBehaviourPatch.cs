using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class MiniMapBehaviourPatch
{
    private const float VentLayerOffset = -0.1f;
    private const float VentArrowLayerOffset = -0.1f; // VentLayerOffset + VentArrowLayerOffset = -0.2
    private const float UsableLayerOffset = -0.3f;

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowNormalMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowDetectiveMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowDetectiveMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowSabotageMap_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(1f, 0.3f, 0f, 1f));

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPostfix]
    private static void MapBehaviour_ShowCountOverlay_Postfix(MapBehaviour __instance)
        => __instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));

    [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
    [HarmonyPostfix]
    private static void MapConsole_ShowCountOverlay_Postfix()
        => MapBehaviour.Instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));

    private static Transform? _icons;

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
    [HarmonyPostfix]
    private static void MapBehaviour_Show_Postfix(MapBehaviour __instance)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_MinimapIcons)) return;

        if (_icons == null)
        {
            var icons = new GameObject("Icons")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            icons.transform.SetParent(__instance.transform.Find("HereIndicatorParent"));
            icons.transform.localPosition = Vector3.zero;
            icons.transform.localScale = Vector3.one;
            _icons = icons.transform;

            foreach (var vent in BAUPlugin.AllVents)
            {
                CreateVentIcon(vent);
            }

            foreach (var map in UnityEngine.Object.FindObjectsOfType<MapConsole>())
            {
                CreateUsableIcon(map.Cast<IUsable>());
            }

            foreach (var system in UnityEngine.Object.FindObjectsOfType<SystemConsole>())
            {
                CreateSystemConsoleIcon(system);
            }

            foreach (var zipline in UnityEngine.Object.FindObjectsOfType<ZiplineConsole>())
            {
                var icon = CreateIcon(zipline.image.sprite, "SystemIcon");
                icon.color = Color.white * 0.7f;
                icon.transform.localScale = Vector3.one * 0.25f;
                SetPosFromShip(zipline.transform.position, icon.transform, new Vector3(0f, 0.16f, UsableLayerOffset));
            }
        }
    }

    private static void CreateVentIcon(Vent vent)
    {
        var icon = CreateIcon(Utils.LoadSprite("BetterAmongUs.Resources.Images.Icons.Vent.png", 280), "VentIcon");
        icon.color = VentGroups.GetVentGroupColor(vent) * 0.8f;

        SetPosFromShip(vent.transform.position, icon.transform, new Vector3(0f, 0f, VentLayerOffset));

        Vent[] nearbyVents = [vent.Left, vent.Right, vent.Center];
        float maxSpreadShift = 1f;
        float minSpreadShift = 0.3f;
        float closeDistance = 10f;

        for (int i = 0; i < nearbyVents.Length; i++)
        {
            Vent neighborVent = nearbyVents[i];
            if (neighborVent)
            {
                var arrowIcon = CreateIcon(Utils.LoadSprite("BetterAmongUs.Resources.Images.Icons.Arrow.png", 500), "VentArrowIcon");
                arrowIcon.color = VentGroups.GetVentGroupColor(neighborVent);
                arrowIcon.transform.SetParent(icon.transform);

                Vector3 directionToNeighbor = neighborVent.transform.position - vent.transform.position;
                float distanceToNeighbor = directionToNeighbor.magnitude;

                float spreadShift;
                if (distanceToNeighbor < closeDistance)
                {
                    float t = distanceToNeighbor / closeDistance;
                    spreadShift = Mathf.Lerp(minSpreadShift, maxSpreadShift, t);
                }
                else
                {
                    spreadShift = maxSpreadShift;
                }

                Vector3 arrowOffset = directionToNeighbor.normalized * (0.7f + spreadShift);
                arrowOffset.y -= 0.08f;

                SetPosFromShip(arrowOffset, arrowIcon.transform, new Vector3(0f, 0f, VentArrowLayerOffset));

                Vector3 ventMapPos = vent.transform.position / ShipStatus.Instance.MapScale;
                ventMapPos.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);

                Vector3 neighborMapPos = neighborVent.transform.position / ShipStatus.Instance.MapScale;
                neighborMapPos.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);

                Vector3 mapDirection = neighborMapPos - ventMapPos;

                float angle = Mathf.Atan2(mapDirection.y, mapDirection.x) * Mathf.Rad2Deg;
                arrowIcon.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }
    }

    private static void CreateSystemConsoleIcon(SystemConsole systemConsole)
    {
        if (systemConsole.UseIcon is not ImageNames.UseButton)
        {
            CreateUsableIcon(systemConsole.Cast<IUsable>());
        }
        else
        {
            var icon = CreateIcon(systemConsole.Image.sprite, "SystemIcon");
            icon.color = Color.white * 0.7f;
            icon.transform.localScale = Vector3.one * 0.4f;
            SetPosFromShip(systemConsole.transform.position, icon.transform, new Vector3(0f, 0f, UsableLayerOffset));
        }
    }

    private static void CreateUsableIcon(IUsable usable)
    {
        if (usable.TryCast<SystemConsole>(out var systemConsole))
        {
            if (systemConsole.MinigamePrefab.name == "EmergencyMinigame")
            {
                var icon = CreateIcon(Utils.LoadSprite("BetterAmongUs.Resources.Images.Icons.Meeting.png", 500), "MeetingIcon");
                icon.color = Color.white * 0.7f;
                icon.transform.localScale = Vector3.one * 0.35f;
                SetPosFromShip(usable.Cast<MonoBehaviour>().transform.position, icon.transform, new Vector3(0f, 0f, UsableLayerOffset));
                return;
            }
            else
            {
                if (systemConsole.UseIcon is ImageNames.UseButton)
                {
                    return;
                }
            }
        }

        if (HudManager.Instance.UseButton.fastUseSettings.TryGetValue(usable.UseIcon, out var settings))
        {
            var icon = CreateIcon(settings.Image, "UsableIcon");
            icon.color = Color.white * 0.7f;
            icon.transform.localScale = Vector3.one * 0.35f;
            SetPosFromShip(usable.Cast<MonoBehaviour>().transform.position, icon.transform, new Vector3(0f, 0f, UsableLayerOffset));
        }
    }

    private static SpriteRenderer CreateIcon(Sprite sprite, string name = "Icon")
    {
        var go = new GameObject(name)
        {
            layer = LayerMask.NameToLayer("UI")
        };
        go.transform.SetParent(_icons);
        go.transform.localScale = Vector3.one;
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        return spriteRenderer;
    }

    private static void SetPosFromShip(Vector3 shipPos, Transform mapTransform, Vector3? offset = null)
    {
        offset ??= Vector3.zero;
        Vector3 vector = shipPos;
        vector /= ShipStatus.Instance.MapScale;
        vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
        vector.z = -1f;
        mapTransform.transform.localPosition = new Vector3(vector.x + offset.Value.x, vector.y + offset.Value.y, offset.Value.z);
    }
}
