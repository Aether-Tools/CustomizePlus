using CustomizePlus.Core.Data;
using CustomizePlus.Core.Events;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Newtonsoft.Json.Linq;
using OtterGui.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CustomizePlus.Templates;

public class TemplateManager
{
    private readonly SaveService _saveService;
    private readonly Logger _logger;
    private readonly TemplateChanged _event;
    private readonly ReloadEvent _reloadEvent;

    private readonly List<Template> _templates = new();

    public IReadOnlyList<Template> Templates
        => _templates;

    public TemplateManager(
        SaveService saveService,
        Logger logger,
        TemplateChanged @event,
        ReloadEvent reloadEvent)
    {
        _saveService = saveService;
        _logger = logger;
        _event = @event;
        _reloadEvent = reloadEvent;
        _reloadEvent.Subscribe(OnReload, ReloadEvent.Priority.TemplateManager);

        CreateTemplateFolder(saveService);
        LoadTemplates();
    }

    public Template? GetTemplate(Guid templateId) => _templates.FirstOrDefault(d => d.UniqueId == templateId);

    public void LoadTemplates()
    {
        _logger.Information("Loading templates from directory...");

        _templates.Clear();
        List<(Template, string)> invalidNames = new();
        foreach (var file in _saveService.FileNames.Templates())
        {
            try
            {
                var text = File.ReadAllText(file.FullName);
                var data = JObject.Parse(text);
                var template = Template.Load(data);
                if (template.UniqueId.ToString() != Path.GetFileNameWithoutExtension(file.Name))
                    invalidNames.Add((template, file.FullName));
                if (_templates.Any(f => f.UniqueId == template.UniqueId))
                    throw new Exception($"ID {template.UniqueId} was not unique.");

                PruneIdempotentTransforms(template);

                _templates.Add(template);
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not load template, skipped:\n{ex}");
                //++skipped;
            }
        }

        var failed = MoveInvalidNames(invalidNames);
        if (invalidNames.Count > 0)
            _logger.Information(
                $"Moved {invalidNames.Count - failed} templates to correct names.{(failed > 0 ? $" Failed to move {failed} templates to correct names." : string.Empty)}");

        _logger.Information("Directory load complete");
        _event.Invoke(TemplateChanged.Type.ReloadedAll, null, null);
    }

    public Template Create(string name, Dictionary<string, BoneTransform>? bones, bool handlePath)
    {
        var (actualName, path) = NameParsingHelper.ParseName(name, handlePath);
        var template = new Template
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = CreateNewGuid(),
            Name = actualName,
            Bones = bones != null && bones.Count > 0 ? new Dictionary<string, BoneTransform>(bones) : new()
        };

        if (template.Bones.Count > 0)
            PruneIdempotentTransforms(template);

        _templates.Add(template);
        _logger.Debug($"Added new template {template.UniqueId}.");

        _saveService.ImmediateSave(template);

        _event.Invoke(TemplateChanged.Type.Created, template, path);

        return template;
    }

    public Template Create(string name, bool handlePath)
    {
        return Create(name, null, handlePath);
    }

    /// <summary>
    /// Create a new template by cloning passed template
    /// </summary>
    /// <returns></returns>
    public Template Clone(Template clone, string name, bool handlePath)
    {
        var (actualName, path) = NameParsingHelper.ParseName(name, handlePath);
        var template = new Template(clone)
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = CreateNewGuid(),
            Name = actualName
        };

        _templates.Add(template);
        _logger.Debug($"Added new template {template.UniqueId} by cloning.");

        _saveService.ImmediateSave(template);

        _event.Invoke(TemplateChanged.Type.Created, template, path);

        return template;
    }

    /// <summary>
    /// Rename template
    /// </summary>
    public void Rename(Template template, string newName)
    {
        var oldName = template.Name.Text;
        if (oldName == newName)
            return;

        template.Name = newName;

        SaveTemplate(template);

        _logger.Debug($"Renamed template {template.UniqueId}.");
        _event.Invoke(TemplateChanged.Type.Renamed, template, oldName);
    }

    /// <summary>
    /// Delete template
    /// </summary>
    /// <param name="template"></param>
    public void Delete(Template template)
    {
        _templates.Remove(template);
        _saveService.ImmediateDelete(template);
        _event.Invoke(TemplateChanged.Type.Deleted, template, null);
    }

    /// <summary>
    /// Set write protection state for template
    /// </summary>
    public void SetWriteProtection(Template template, bool value)
    {
        if (template.IsWriteProtected == value)
            return;

        template.IsWriteProtected = value;

        SaveTemplate(template);

        _logger.Debug($"Set template {template.UniqueId} to {(value ? string.Empty : "no longer be ")} write-protected.");
        _event.Invoke(TemplateChanged.Type.WriteProtection, template, value);
    }

    /// <summary>
    /// Copy bone data from source template to target template and queue a save for target template
    /// </summary>
    /// <param name="targetTemplate"></param>
    /// <param name="sourceTemplate"></param>
    public void ApplyBoneChangesAndSave(Template targetTemplate, Template sourceTemplate)
    {
        _logger.Debug($"Copying bones from {sourceTemplate.Name} to {targetTemplate.Name}");
        var deletedBones = targetTemplate.Bones.Keys.Except(sourceTemplate.Bones.Keys).ToList();
        foreach (var kvPair in sourceTemplate.Bones)
        {
            ModifyBoneTransform(targetTemplate, kvPair.Key, kvPair.Value);
        }

        foreach (var boneName in deletedBones)
        {
            DeleteBoneTransform(targetTemplate, boneName);
        }

        SaveTemplate(targetTemplate);
    }

    //Creates, updates or deletes bone transform
    //not to be used on editor-related features by anything but TemplateEditorManager
    public bool ModifyBoneTransform(Template template, string boneName, BoneTransform transform)
    {
        if (template.Bones.TryGetValue(boneName, out var boneTransform)
            && boneTransform != null)
        {
            if (boneTransform == transform)
                return false;

            if (transform.IsEdited())
            {
                template.Bones[boneName].UpdateToMatch(transform);

                _logger.Debug($"Updated bone {boneName} on {template.Name}");
                _event.Invoke(TemplateChanged.Type.UpdatedBone, template, boneName);
            }
            else
            {
                template.Bones.Remove(boneName);

                _logger.Debug($"Deleted bone {boneName} on {template.Name}");
                _event.Invoke(TemplateChanged.Type.DeletedBone, template, boneName);
            }

        }
        else
        {
            template.Bones[boneName] = new BoneTransform(transform);

            _logger.Debug($"Created bone {boneName} on {template.Name}");
            _event.Invoke(TemplateChanged.Type.NewBone, template, boneName);
        }

        return true;
    }

    private void DeleteBoneTransform(Template template, string boneName)
    {
        if (!template.Bones.ContainsKey(boneName))
            return;

        template.Bones.Remove(boneName);

        _logger.Debug($"Deleted bone {boneName} on {template.Name}");
        _event.Invoke(TemplateChanged.Type.DeletedBone, template, boneName);
    }

    private static void PruneIdempotentTransforms(Template template)
    {
        foreach (var kvp in template.Bones)
        {
            if (!kvp.Value.IsEdited())
            {
                template.Bones.Remove(kvp.Key);
            }
        }
    }

    private void SaveTemplate(Template template)
    {
        template.ModifiedDate = DateTimeOffset.UtcNow;
        _saveService.QueueSave(template);
    }

    private void OnReload(ReloadEvent.Type type)
    {
        if (type != ReloadEvent.Type.ReloadTemplates &&
            type != ReloadEvent.Type.ReloadAll)
            return;

        _logger.Debug("Reload event received");
        LoadTemplates();
    }

    private static void CreateTemplateFolder(SaveService service)
    {
        var ret = service.FileNames.TemplateDirectory;
        if (Directory.Exists(ret))
            return;

        try
        {
            Directory.CreateDirectory(ret);
        }
        catch (Exception ex)
        {
            Plugin.Logger.Error($"Could not create template directory {ret}:\n{ex}");
        }
    }

    /// <summary> Move all files that were discovered to have names not corresponding to their identifier to correct names, if possible. </summary>
    /// <returns>The number of files that could not be moved.</returns>
    private int MoveInvalidNames(IEnumerable<(Template, string)> invalidNames)
    {
        var failed = 0;
        foreach (var (template, name) in invalidNames)
        {
            try
            {
                var correctName = _saveService.FileNames.TemplateFile(template);
                File.Move(name, correctName, false);
                _logger.Information($"Moved invalid template file from {Path.GetFileName(name)} to {Path.GetFileName(correctName)}.");
            }
            catch (Exception ex)
            {
                ++failed;
                _logger.Error($"Failed to move invalid template file from {Path.GetFileName(name)}:\n{ex}");
            }
        }

        return failed;
    }

    /// <summary>
    /// Create new guid until we find one which isn't used by existing template
    /// </summary>
    /// <returns></returns>
    private Guid CreateNewGuid()
    {
        while (true)
        {
            var guid = Guid.NewGuid();
            if (_templates.All(d => d.UniqueId != guid))
                return guid;
        }
    }
}
