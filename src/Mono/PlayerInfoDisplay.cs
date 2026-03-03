using AmongUs.Data;
using AmongUs.GameOptions;

using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;

using BetterAmongUs.Structs;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Mono;

/// <summary>
/// Displays extended player information during gameplay.
/// </summary>
internal class PlayerInfoDisplay : MonoBehaviour
{
    protected PlayerControl? _player;
    protected TextMeshPro? _nameText;
    protected TextMeshPro? _infoText;
    protected TextMeshPro? _topText;
    protected TextMeshPro? _bottomText;

    private readonly StringBuilder _sbTag = new(256);
    private readonly StringBuilder _sbTagTop = new(256);
    private readonly StringBuilder _sbTagBottom = new(256);
    private string _lastTopText = "", _lastBottomText = "", _lastInfoText = "";
    private int _lastUpdateFrame;
    private const int UPDATE_COOLDOWN = 10;



    private CachedTranslations _cachedTranslations = new();

    /// <summary>
    /// Cached color values for performance optimization.
    /// </summary>
    private static readonly Dictionary<string, Color32> _cachedColors = new()
    {
        ["#00f583"] = Utils.HexToColor32("#00f583"),
        ["#4f0000"] = Utils.HexToColor32("#4f0000"),
        ["#fc0000"] = Utils.HexToColor32("#fc0000"),
        ["#8731e7"] = Utils.HexToColor32("#8731e7")
    };

    /// <summary>
    /// Cached translations for performance optimization.
    /// </summary>
    private class CachedTranslations
    {
        internal readonly string Loading = Translator.GetString("Player.Loading");
        internal readonly string PlatformHidden = Translator.GetString("Player.PlatformHidden");
        internal readonly string SickoUser = Translator.GetString("Player.SickoUser");
        internal readonly string AUMUser = Translator.GetString("Player.AUMUser");
        internal readonly string KNUser = Translator.GetString("Player.KNUser");
        internal readonly string KnownCheater = Translator.GetString("Player.KnownCheater");
        internal readonly string BetterUser = Translator.GetString("Player.BetterUser");
    }

    /// <summary>
    /// Initializes the player info display.
    /// </summary>
    /// <param name="player">The player to display info for.</param>
    internal void Init(PlayerControl player)
    {
        _player = player;

        var nameTextTransform = player.gameObject.transform.Find("Names/NameText_TMP");
        _nameText = nameTextTransform?.GetComponent<TextMeshPro>();

        _infoText = InstantiatePlayerInfoText("InfoText_Info_TMP", new Vector3(0f, 0.25f), nameTextTransform);
        _topText = InstantiatePlayerInfoText("InfoText_T_TMP", new Vector3(0f, 0.15f), nameTextTransform);
        _bottomText = InstantiatePlayerInfoText("InfoText_B_TMP", new Vector3(0f, -0.15f), nameTextTransform);
        _infoText.fontSize = 1.3f;
        _topText.fontSize = 1.3f;
        _bottomText.fontSize = 1.3f;
    }

    /// <summary>
    /// Instantiates a player info text object.
    /// </summary>
    /// <param name="name">The name of the text object.</param>
    /// <param name="positionOffset">The position offset from the parent.</param>
    /// <param name="parent">The parent transform.</param>
    /// <returns>The created TextMeshPro component.</returns>
    protected TextMeshPro InstantiatePlayerInfoText(string name, Vector3 positionOffset, Transform parent)
    {
        var newTextObject = Instantiate(_nameText, parent);
        newTextObject.name = name;
        newTextObject.transform.DestroyChildren();
        newTextObject.transform.position += positionOffset;

        var textMesh = newTextObject.GetComponent<TextMeshPro>();
        textMesh.text = string.Empty;
        newTextObject.gameObject.SetActive(true);

        return textMesh;
    }

    /// <summary>
    /// Resets all text displays to empty.
    /// </summary>
    private void ResetText()
    {
        _infoText?.SetText(string.Empty);
        _topText?.SetText(string.Empty);
        _bottomText?.SetText(string.Empty);
    }

    /// <summary>
    /// LateUpdate override with cooldown for performance optimization.
    /// </summary>
    protected virtual void LateUpdate()
    {
        if (Time.frameCount - _lastUpdateFrame < UPDATE_COOLDOWN)
            return;

        if (_player == null || _player.Data == null || _nameText == null)
        {
            ResetText();
            return;
        }

        _sbTag.Clear();
        _sbTagTop.Clear();
        _sbTagBottom.Clear();

        UpdatePlayerInfo();
        UpdatePlayerHighlight();
        UpdateColorBlindTextPosition();
        _nameText.transform.parent.localPosition = new Vector3(0f, 0.8f, -0.5f);

        _lastUpdateFrame = Time.frameCount;
    }

    /// <summary>
    /// Updates player information display.
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (_player?.Data == null) return;



        if (!_player.DataIsCollected())
        {
            _nameText.text = _cachedTranslations.Loading;
            return;
        }

        if (!BAUPlugin.LobbyPlayerInfo.Value && GameState.IsLobby)
        {
            ResetText();
            _player.RawSetName(_player.Data.PlayerName);
            return;
        }

        string newName = _player.Data.PlayerName;
        string hashPuid = Utils.GetHashPuid(_player);
        string platform = Utils.GetPlatformName(_player, useTag: true);



        if (DataManager.Settings.Gameplay.StreamerMode)
        {
            platform = _cachedTranslations.PlatformHidden;
        }

        if (!_player.IsInShapeshift())
        {
            SetPlayerOutline(_sbTag);
        }

        if (GameState.IsInGame && GameState.IsLobby && !GameState.IsFreePlay)
        {
            SetLobbyInfo(ref newName, _sbTag);
            _sbTagTop.Append($"<color=#9e9e9e>{platform}</color>+++")
                    .Append($"<color=#ffd829>Lv: {_player.Data.PlayerLevel + 1}</color>+++");


        }
        else if ((GameState.IsInGame || GameState.IsFreePlay) && !GameState.IsHideNSeek)
        {
            // Town of Us Mira-style role name display above the player name
            SetInGameRoleDisplay(_sbTagTop);
        }

        if (!_player.IsInShapeshift())
        {
            if (_player.IsImpostorTeammate())
                newName = newName.ToColor(Colors.ImpostorRed);
            _player.RawSetName(newName);
        }
        else
        {
            var targetData = Utils.PlayerDataFromPlayerId(_player.shapeshiftTargetPlayerId);
            var name = targetData.PlayerName;
            if (_player.IsImpostorTeammate())
                name = name.ToColor(Colors.ImpostorRed);
            if (targetData != null) _player.RawSetName(name);
        }

        UpdateTextIfChanged(_topText, _sbTagTop, ref _lastTopText);
        UpdateTextIfChanged(_bottomText, _sbTagBottom, ref _lastBottomText);
        UpdateTextIfChanged(_infoText, _sbTag, ref _lastInfoText);
    }

    /// <summary>
    /// Updates text if changed, optimizing performance.
    /// </summary>
    /// <param name="textMesh">TextMeshPro component to update.</param>
    /// <param name="sb">StringBuilder containing new text.</param>
    /// <param name="lastValue">Reference to last value for comparison.</param>
    private static void UpdateTextIfChanged(TextMeshPro textMesh, StringBuilder sb, ref string lastValue)
    {
        if (textMesh == null) return;

        string newText = Utils.FormatInfo(sb);
        if (newText != lastValue)
        {
            textMesh?.SetText(newText);
            lastValue = newText;
        }
    }


    /// <summary>
    /// Sets player outline based on data from BetterDataManager.
    /// </summary>
    /// <param name="sbTag">StringBuilder for tag text.</param>
    [HideFromIl2Cpp]
    private void SetPlayerOutline(StringBuilder sbTag)
    {
        if (_player?.Data == null) return;

        string hashPuid = Utils.GetHashPuid(_player);
        string friendCode = _player.Data.FriendCode;

        var color = _player.cosmetics.currentBodySprite.BodySprite.material.GetColor("_OutlineColor");

        if (ContainsPlayerData(BetterDataManager.BetterDataFile.SickoData, _player.Data))
        {
            sbTag.Append($"<color=#00f583>{_cachedTranslations.SickoUser}</color>+++");
            _player.SetOutlineByHex(true, "#00f583");
        }
        else if (ContainsPlayerData(BetterDataManager.BetterDataFile.AUMData, _player.Data))
        {
            sbTag.Append($"<color=#4f0000>{_cachedTranslations.AUMUser}</color>+++");
            _player.SetOutlineByHex(true, "#4f0000");
        }
        else if (ContainsPlayerData(BetterDataManager.BetterDataFile.KNData, _player.Data))
        {
            sbTag.Append($"<color=#8731e7>{_cachedTranslations.KNUser}</color>+++");
            _player.SetOutlineByHex(true, "#8731e7");
        }
        else if (ContainsPlayerData(BetterDataManager.BetterDataFile.CheatData, _player.Data))
        {
            sbTag.Append($"<color=#fc0000>{_cachedTranslations.KnownCheater}</color>+++");
            _player.SetOutlineByHex(true, "#fc0000");
        }
        else if (_cachedColors.Any(kvp => color == kvp.Value))
        {
            _player.SetOutline(false, null);
        }
    }

    /// <summary>
    /// Checks if player data exists in a HashSet of UserInfo.
    /// </summary>
    /// <param name="dataList">HashSet of UserInfo to check.</param>
    /// <param name="playerData">Player data to look for.</param>
    /// <returns>True if player data exists in the HashSet.</returns>
    [HideFromIl2Cpp]
    private static bool ContainsPlayerData(HashSet<UserInfo> dataList, NetworkedPlayerInfo playerData)
    {
        foreach (var info in dataList)
        {
            if (info.CheckPlayerData(playerData))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Sets lobby-specific information.
    /// </summary>
    /// <param name="newName">Reference to the player's name.</param>
    /// <param name="betterData">Extended player data.</param>
    /// <param name="sbTag">StringBuilder for tag text.</param>
    [HideFromIl2Cpp]
    private void SetLobbyInfo(ref string newName, StringBuilder sbTag)
    {
        if (_player.IsHost() && BAUPlugin.LobbyPlayerInfo.Value)
            newName = _player.GetPlayerNameAndColor();

        sbTag.Append($"<color=#b554ff>ID: {_player.PlayerId}</color>+++");
    }

    /// <summary>
    /// Sets in-game role display above the player name, styled after Town of Us Mira.
    /// Role name is shown in team color (red for Impostor, blue for Crewmate) in bold brackets.
    /// Only shown to the local player for their own role, or to Impostors for their teammates.
    /// </summary>
    /// <param name="sbTagTop">StringBuilder for top tag text.</param>
    [HideFromIl2Cpp]
    private void SetInGameRoleDisplay(StringBuilder sbTagTop)
    {
        if (_player == null || _player.Data == null) return;

        bool isLocalPlayer = _player.IsLocalPlayer();
        bool isImpostorTeammate = _player.IsImpostorTeammate();

        // Only show role name if this is our own player, or an impostor teammate we can see
        if (!isLocalPlayer && !isImpostorTeammate) return;

        string roleName = _player.GetRoleName();
        if (string.IsNullOrEmpty(roleName)) return;

        string teamColor = _player.GetTeamHexColor();

        // TOU Mira format: bold role name in team color, wrapped in brackets
        sbTagTop.Append($"<color={teamColor}><b>[{roleName}]</b></color>+++");
    }

    /// <summary>
    /// Sets in-game specific information.
    /// </summary>
    /// <param name="sbTagTop">StringBuilder for top tag text.</param>
    [HideFromIl2Cpp]
    

    /// <summary>
    /// Updates player highlight/outline.
    /// </summary>
    private void UpdatePlayerHighlight()
    {
        SetPlayerOutline(new StringBuilder(32));
    }

    /// <summary>
    /// Updates color blind text position.
    /// </summary>
    private void UpdateColorBlindTextPosition()
    {
        var text = _player.cosmetics.colorBlindText;
        if (!text.enabled) return;
        if (!_player.onLadder && !_player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            text.transform.localPosition = new Vector3(0f, -1.3f, 0.4999f);
        }
        else
        {
            text.transform.localPosition = new Vector3(0f, -1.5f, 0.4999f);
        }
    }
}