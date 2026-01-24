using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterAmongUs.Modules;

/// <summary>
/// Represents a customizable client option item that can be toggled in the options menu.
/// </summary>
internal sealed class ClientOptionItem
{
    /// <summary>
    /// Gets or sets the configuration entry associated with this option.
    /// </summary>
    internal ConfigEntry<bool>? Config { get; set; }

    /// <summary>
    /// Gets or sets the toggle button behavior for this option.
    /// </summary>
    internal ToggleButtonBehaviour ToggleButton { get; set; }

    /// <summary>
    /// Gets or sets the custom background for BetterAmongUs options.
    /// </summary>
    internal static SpriteRenderer? CustomBackground { get; set; }

    private static int numOptions = 0;

    /// <summary>
    /// Initializes a new instance of the ClientOptionItem class.
    /// </summary>
    /// <param name="name">The display name of the option.</param>
    /// <param name="config">The configuration entry for this option.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <param name="additionalOnClickAction">Additional action to perform on click.</param>
    /// <param name="toggleCheck">Function to check if the toggle can be interacted with.</param>
    /// <param name="IsToggle">Whether this is a toggle option or a button.</param>
    internal ClientOptionItem(string name, ConfigEntry<bool>? config, OptionsMenuBehaviour optionsMenuBehaviour, Action? additionalOnClickAction = null, Func<bool>? toggleCheck = null, bool IsToggle = true)
    {
        try
        {
            Config = config;

            var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

            // Initialize custom background on first creation
            if (CustomBackground == null)
            {
                numOptions = 0;
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "CustomBackground";
                CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
                CustomBackground.transform.localPosition += Vector3.back * 8;
                CustomBackground.gameObject.SetActive(false);

                // Create back button
                var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
                closeButton.name = "Back";
                closeButton.Text.text = "Back";
                closeButton.Background.color = Palette.DisabledGrey;
                var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                closePassiveButton.OnClick = new();
                closePassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(false);
                }));

                // Find existing UI elements
                var selectableButtons = optionsMenuBehaviour.ControllerSelectable;
                PassiveButton? leaveButton = null;
                PassiveButton? returnButton = null;
                foreach (var button in selectableButtons)
                {
                    if (button == null) continue;

                    if (button.name == "LeaveGameButton")
                        leaveButton = button.GetComponent<PassiveButton>();
                    else if (button.name == "ReturnToGameButton")
                        returnButton = button.GetComponent<PassiveButton>();
                }

                // Create main mod options button
                var generalTab = mouseMoveToggle.transform.parent.parent.parent;
                var modOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                modOptionsButton.transform.localPosition = leaveButton?.transform?.localPosition ?? new(0f, -2.4f, 1f);
                modOptionsButton.name = "Better Options";
                modOptionsButton.Text.text = Translator.GetString("BetterOption");
                modOptionsButton.Background.color = new Color32(0, 150, 0, byte.MaxValue);
                var modOptionsPassiveButton = modOptionsButton.GetComponent<PassiveButton>();
                modOptionsPassiveButton.OnClick = new();
                modOptionsPassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(true);
                }));

                // Reposition existing buttons
                if (leaveButton != null)
                    leaveButton.transform.localPosition = new(-1.35f, -2.411f, -1f);
                if (returnButton != null)
                    returnButton.transform.localPosition = new(1.35f, -2.411f, -1f);
            }

            // Create this specific option button
            ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            ToggleButton.transform.localPosition = new Vector3(
                numOptions % 2 == 0 ? -1.3f : 1.3f,
                2.2f - 0.5f * (numOptions / 2),
                -6f);
            ToggleButton.name = name;
            ToggleButton.Text.text = name;
            ToggleButton.Text.text += Config != null && Config.Value ? ": On" : ": Off";

            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();

            // Handle non-toggle button
            if (IsToggle == false)
            {
                ToggleButton.Text.text = name;
                ToggleButton.Rollover?.ChangeOutColor(new(0, 150, 0, byte.MaxValue));
                ToggleButton.Text.color = new(1f, 1f, 1f, 1f);

                passiveButton.OnClick.AddListener(new Action(() =>
                {
                    if (toggleCheck?.Invoke() == false)
                    {
                        return;
                    }

                    additionalOnClickAction?.Invoke();
                }));

                return;
            }

            // Handle toggle button
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                if (toggleCheck?.Invoke() == false)
                {
                    return;
                }

                if (config != null) config.Value = !config.Value;
                UpdateToggle();
                additionalOnClickAction?.Invoke();
            }));

            UpdateToggle();
        }
        finally
        {
            numOptions++;
        }
    }

    /// <summary>
    /// Factory method to create a new ClientOptionItem.
    /// </summary>
    /// <param name="name">The display name of the option.</param>
    /// <param name="config">The configuration entry for this option.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <param name="additionalOnClickAction">Additional action to perform on click.</param>
    /// <param name="toggleCheck">Function to check if the toggle can be interacted with.</param>
    /// <param name="IsToggle">Whether this is a toggle option or a button.</param>
    /// <returns>A new ClientOptionItem instance.</returns>
    internal static ClientOptionItem Create(string name, ConfigEntry<bool>? config, OptionsMenuBehaviour optionsMenuBehaviour, Action? additionalOnClickAction = null, Func<bool>? toggleCheck = null, bool IsToggle = true)
    {
        toggleCheck ??= () => true;

        return new ClientOptionItem(name, config, optionsMenuBehaviour, additionalOnClickAction, toggleCheck, IsToggle);
    }

    /// <summary>
    /// Updates the visual state of the toggle button based on the configuration value.
    /// </summary>
    internal void UpdateToggle()
    {
        if (ToggleButton == null) return;

        var color = Config != null && Config.Value ?
            new Color32(0, 150, 0, byte.MaxValue) :
            new Color32(77, 77, 77, byte.MaxValue);
        var textColor = Config != null && Config.Value ?
            new Color(1f, 1f, 1f, 1f) :
            new Color(1f, 1f, 1f, 0.5f);

        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
        ToggleButton.Text.color = textColor;
        ToggleButton.Text.text = ToggleButton.name;
        ToggleButton.Text.text += Config != null && Config.Value ? ": On" : ": Off";
    }
}