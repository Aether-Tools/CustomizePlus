using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Game.Events;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Enums;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using OtterGui.Classes;
using OtterGui.Log;
using Penumbra.GameData.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CustomizePlus.Templates;

public class TemplateEditorManager : IDisposable
{
    private readonly TemplateChanged _event;
    private readonly Logger _logger;
    private readonly GameObjectService _gameObjectService;
    private readonly TemplateManager _templateManager;
    private readonly IClientState _clientState;
    private readonly PluginConfiguration _configuration;

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

    /// <summary>
    /// Name of the preview character for the editor
    /// </summary>
    public ActorIdentifier Character => EditorProfile.Character;

    /// <summary>
    /// Checks if preview character exists at the time of call
    /// </summary>
    public bool IsCharacterFound
    { 
        get
        {
            //todo: check with mounts/companions
            var playerName = _gameObjectService.GetCurrentPlayerName();
            return _gameObjectService.FindActorsByIdentifierIgnoringOwnership(Character)
                .Where(x => x.Item1.Type != Penumbra.GameData.Enums.IdentifierType.Owned || x.Item1.IsOwnedByLocalPlayer())
                .Any();
        } 
    }

    public bool IsKeepOnlyEditorProfileActive { get; set; } //todo

    public TemplateEditorManager(
        TemplateChanged @event,
        Logger logger,
        TemplateManager templateManager,
        GameObjectService gameObjectService,
        IClientState clientState,
        PluginConfiguration configuration)
    {
        _event = @event;
        _logger = logger;
        _templateManager = templateManager;
        _gameObjectService = gameObjectService;
        _clientState = clientState;
        _configuration = configuration;

        _clientState.Login += OnLogin;

        EditorProfile = new Profile()
        {
            Templates = new List<Template>(),
            Enabled = false,
            Name = "Template editor profile",
            ProfileType = ProfileType.Editor,
            Character = configuration.EditorConfiguration.PreviewCharacter
        };
    }

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
    }

    /// <summary>
    /// Turn on editing of a specific template. If character name not set will default to local player.
    /// </summary>
    internal bool EnableEditor(Template template)
    {
        if (IsEditorActive || IsEditorPaused)
            return false;

        _logger.Debug($"Enabling editor profile for {template.Name} via character {Character.Incognito(null)}");

        CurrentlyEditedTemplateId = template.UniqueId;
        _currentlyEditedTemplateOriginal = template;
        CurrentlyEditedTemplate = new Template(template)
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = Guid.NewGuid(),
            Name = "Template editor temporary template"
        };

        if (!Character.IsValid) //safeguard
            ChangeEditorCharacterInternal(_gameObjectService.GetCurrentPlayerActorIdentifier()); //will set EditorProfile.Character

        EditorProfile.Templates.Clear(); //safeguard
        EditorProfile.Templates.Add(CurrentlyEditedTemplate);
        EditorProfile.Enabled = true;
        HasChanges = false;
        IsEditorActive = true;

        _event.Invoke(TemplateChanged.Type.EditorEnabled, template, Character);

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

        CurrentlyEditedTemplateId = Guid.Empty;
        CurrentlyEditedTemplate = null;
        EditorProfile.Enabled = false;
        EditorProfile.Templates.Clear();
        IsEditorActive = false;
        HasChanges = false;

        _event.Invoke(TemplateChanged.Type.EditorDisabled, null, Character);

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

    public bool ChangeEditorCharacter(ActorIdentifier character)
    {
        if (!IsEditorActive || Character == character || IsEditorPaused || !character.IsValid)
            return false;

        return ChangeEditorCharacterInternal(character);
    }

    private bool ChangeEditorCharacterInternal(ActorIdentifier character)
    {
        _logger.Debug($"Changing character name for editor profile from {EditorProfile.Character.Incognito(null)} to {character.Incognito(null)}");

        EditorProfile.Character = character;

        _configuration.EditorConfiguration.PreviewCharacter = character;
        _configuration.Save();

        _event.Invoke(TemplateChanged.Type.EditorCharacterChanged, CurrentlyEditedTemplate, (character, EditorProfile));

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

    private void OnLogin()
    {
        if (_configuration.EditorConfiguration.SetPreviewToCurrentCharacterOnLogin ||
            !_configuration.EditorConfiguration.PreviewCharacter.IsValid)
        {
            var localPlayer = _gameObjectService.GetCurrentPlayerActorIdentifier();
            if(!localPlayer.IsValid)
            {
                _logger.Warning("Can't retrieve local player on login");
                return;
            }

            if (_configuration.EditorConfiguration.PreviewCharacter != localPlayer)
            {
                _logger.Debug("Resetting editor character because automatic condition triggered in OnLogin");
                ChangeEditorCharacterInternal(localPlayer);
            }
        }
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
}
