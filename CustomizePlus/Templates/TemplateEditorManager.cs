using CustomizePlus.Core.Data;
using CustomizePlus.Game.Events;
using CustomizePlus.Game.Services;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Enums;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using OtterGui.Log;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CustomizePlus.Templates;

public class TemplateEditorManager : IDisposable
{
    private readonly TemplateChanged _event;
    private readonly GPoseStateChanged _gposeStateChanged;
    private readonly Logger _logger;
    private readonly GameObjectService _gameObjectService;
    private readonly TemplateManager _templateManager;

    /// <summary>
    /// Reference to the original template which is currently being edited, should not be edited!
    /// </summary>
    private Template _currentlyEditedTemplateOriginal;

    /// <summary>
    /// Internal profile for the editor
    /// </summary>
    public Profile EditorProfile { get; private set; }

    /// <summary>
    /// Original ID of the template which is currently being edited
    /// </summary>
    public Guid CurrentlyEditedTemplateId { get; private set; }

    /// <summary>
    /// A copy of currently edited template, all changes must be done on this template
    /// </summary>
    public Template? CurrentlyEditedTemplate { get; private set; }

    public bool IsEditorActive { get; private set; }

    /// <summary>
    /// Is editor currently paused? Happens automatically when editor is not compatible with the current game state.
    /// Keeps editor state frozen and prevents any changes to it, also sets editor profile as disabled.
    /// </summary>
    public bool IsEditorPaused { get; private set; }

    /// <summary>
    /// Indicates if there are any changes in current editing session or not
    /// </summary>
    public bool HasChanges { get; private set; }

    public bool IsKeepOnlyEditorProfileActive { get; set; } //todo

    public TemplateEditorManager(
        TemplateChanged @event,
        GPoseStateChanged gposeStateChanged,
        Logger logger,
        TemplateManager templateManager,
        GameObjectService gameObjectService)
    {
        _event = @event;
        _gposeStateChanged = gposeStateChanged;
        _logger = logger;
        _templateManager = templateManager;
        _gameObjectService = gameObjectService;

        _gposeStateChanged.Subscribe(OnGPoseStateChanged, GPoseStateChanged.Priority.TemplateEditorManager);

        EditorProfile = new Profile() { Templates = new List<Template>(), Enabled = false, Name = "Template editor profile", ProfileType = ProfileType.Editor };
    }

    public void Dispose()
    {
        _gposeStateChanged.Unsubscribe(OnGPoseStateChanged);
    }

    /// <summary>
    /// Turn on editing of a specific template. If character name not set will default to local player.
    /// </summary>
    internal bool EnableEditor(Template template, string? characterName = null) //todo: editor is borked
    {
        if (IsEditorActive || IsEditorPaused)
            return false;

        _logger.Debug($"Enabling editor profile for {template.Name} via character {characterName}");

        CurrentlyEditedTemplateId = template.UniqueId;
        _currentlyEditedTemplateOriginal = template;
        CurrentlyEditedTemplate = new Template(template)
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = Guid.NewGuid(),
            Name = "Template editor temporary template"
        };

        if (characterName != null)
            EditorProfile.CharacterName = characterName;
        else //safeguard
            EditorProfile.CharacterName = _gameObjectService.GetCurrentPlayerName();

        EditorProfile.Templates.Clear(); //safeguard
        EditorProfile.Templates.Add(CurrentlyEditedTemplate);
        EditorProfile.Enabled = true;
        HasChanges = false;
        IsEditorActive = true;

        _event.Invoke(TemplateChanged.Type.EditorEnabled, template, characterName);

        return true;
    }

    /// <summary>
    /// Turn off editing of a specific template
    /// </summary>
    internal bool DisableEditor()
    {
        if (!IsEditorActive || IsEditorPaused)
            return false;

        _logger.Debug($"Disabling editor profile");

        string characterName = EditorProfile.CharacterName;

        CurrentlyEditedTemplateId = Guid.Empty;
        CurrentlyEditedTemplate = null;
        EditorProfile.Enabled = false;
        EditorProfile.CharacterName = "";
        EditorProfile.Templates.Clear();
        IsEditorActive = false;
        HasChanges = false;

        _event.Invoke(TemplateChanged.Type.EditorDisabled, null, characterName);

        return true;
    }

    public void SaveChanges(bool asCopy = false)
    {
        var targetTemplate = _templateManager.GetTemplate(CurrentlyEditedTemplateId);
        if (targetTemplate == null)
            throw new Exception($"Fatal editor error: Template with ID {CurrentlyEditedTemplateId} not found in template manager");

        if (asCopy)
            targetTemplate = _templateManager.Clone(targetTemplate, $"{targetTemplate.Name} - Copy {Guid.NewGuid().ToString().Substring(0, 4)}", false);

        _templateManager.ApplyBoneChangesAndSave(targetTemplate, CurrentlyEditedTemplate!);
    }

    public bool ChangeEditorCharacter(string characterName)
    {
        if (!IsEditorActive || EditorProfile.CharacterName == characterName || IsEditorPaused)
            return false;

        _logger.Debug($"Changing character name for editor profile from {EditorProfile.CharacterName} to {characterName}");

        EditorProfile.CharacterName = characterName;
        _event.Invoke(TemplateChanged.Type.EditorCharacterChanged, CurrentlyEditedTemplate, (characterName, EditorProfile));

        return true;
    }

    public bool SetLimitLookupToOwned(bool value)
    {
        if (!IsEditorActive || IsEditorPaused || value == EditorProfile.LimitLookupToOwnedObjects)
            return false;

        //_profileManager.SetLimitLookupToOwned(EditorProfile, value);
        EditorProfile.LimitLookupToOwnedObjects = value;
        _event.Invoke(TemplateChanged.Type.EditorLimitLookupToOwnedChanged, CurrentlyEditedTemplate, EditorProfile);

        return true;
    }

    /// <summary>
    /// Resets changes in currently edited template to default values
    /// </summary>
    public bool ResetBoneAttributeChanges(string boneName, BoneAttribute attribute)
    {
        if (!IsEditorActive || IsEditorPaused)
            return false;

        if (!CurrentlyEditedTemplate!.Bones.ContainsKey(boneName))
            return false;

        var resetValue = GetResetValueForAttribute(attribute);

        switch (attribute)
        {
            case BoneAttribute.Position:
                if (resetValue == CurrentlyEditedTemplate!.Bones[boneName].Translation)
                    return false;
                break;
            case BoneAttribute.Rotation:
                if (resetValue == CurrentlyEditedTemplate!.Bones[boneName].Rotation)
                    return false;
                break;
            case BoneAttribute.Scale:
                if (resetValue == CurrentlyEditedTemplate!.Bones[boneName].Scaling)
                    return false;
                break;
        }

        CurrentlyEditedTemplate!.Bones[boneName].UpdateAttribute(attribute, resetValue);

        if (!HasChanges)
            HasChanges = true;

        return true;
    }

    /// <summary>
    /// Reverts changes in currently edited template to values set in saved copy of the template.
    /// Resets to default value if saved copy doesn't have that bone edited
    /// </summary>
    public bool RevertBoneAttributeChanges(string boneName, BoneAttribute attribute)
    {
        if (!IsEditorActive || IsEditorPaused)
            return false;

        if (!CurrentlyEditedTemplate!.Bones.ContainsKey(boneName))
            return false;

        Vector3? originalValue = null!;

        if (_currentlyEditedTemplateOriginal.Bones.ContainsKey(boneName))
        {
            switch (attribute)
            {
                case BoneAttribute.Position:
                    originalValue = _currentlyEditedTemplateOriginal.Bones[boneName].Translation;
                    if (originalValue == CurrentlyEditedTemplate!.Bones[boneName].Translation)
                        return false;
                    break;
                case BoneAttribute.Rotation:
                    originalValue = _currentlyEditedTemplateOriginal.Bones[boneName].Rotation;
                    if (originalValue == CurrentlyEditedTemplate!.Bones[boneName].Rotation)
                        return false;
                    break;
                case BoneAttribute.Scale:
                    originalValue = _currentlyEditedTemplateOriginal.Bones[boneName].Scaling;
                    if (originalValue == CurrentlyEditedTemplate!.Bones[boneName].Scaling)
                        return false;
                    break;
            }
        }
        else
            originalValue = GetResetValueForAttribute(attribute);

        CurrentlyEditedTemplate!.Bones[boneName].UpdateAttribute(attribute, originalValue.Value);

        if (!HasChanges)
            HasChanges = true;

        return true;
    }

    public bool ModifyBoneTransform(string boneName, BoneTransform transform)
    {
        if (!IsEditorActive || IsEditorPaused)
            return false;

        if (!_templateManager.ModifyBoneTransform(CurrentlyEditedTemplate!, boneName, transform))
            return false;

        if (!HasChanges)
            HasChanges = true;

        return true;
    }

    private Vector3 GetResetValueForAttribute(BoneAttribute attribute)
    {
        switch (attribute)
        {
            case BoneAttribute.Scale:
                return Vector3.One;
            default:
                return Vector3.Zero;
        }
    }

    private void OnGPoseStateChanged(GPoseStateChanged.Type type)
    {
        switch (type)
        {
            case GPoseStateChanged.Type.Entered:
                IsEditorPaused = true;
                EditorProfile.Enabled = false;
                break;
            case GPoseStateChanged.Type.Exited:
                EditorProfile.Enabled = true;
                IsEditorPaused = false;
                break;
        }
    }
}
