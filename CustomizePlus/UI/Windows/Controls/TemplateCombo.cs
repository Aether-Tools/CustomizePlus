using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;

namespace CustomizePlus.UI.Windows.Controls;

public abstract class TemplateComboBase : FilterComboBase<TemplateCacheItem>, IDisposable
{
    private readonly PluginConfiguration _configuration;
    private readonly TemplateChanged _templateChanged;

    protected Template? CurrentTemplate;

    protected TemplateComboBase(
        TemplateChanged templateChanged,
        PluginConfiguration configuration)
        : base(new TemplateFilter(), ConfigData.Default with { ComputeWidth = true })
    {
        _templateChanged = templateChanged;
        _configuration = configuration;
        _templateChanged.Subscribe(OnTemplateChange, TemplateChanged.Priority.TemplateCombo);
    }

    public bool Incognito
        => _configuration.UISettings.IncognitoMode;

    void IDisposable.Dispose()
        => _templateChanged.Unsubscribe(OnTemplateChange);

    protected TemplateCacheItem CreateItem(Template template)
    {
        var path = template.Node?.FullPath ?? string.Empty;
        var name = template.Name;
        if (path == name)
            path = string.Empty;
        return new TemplateCacheItem(template, path, name);
    }

    protected override bool IsSelected(TemplateCacheItem item, int globalIndex)
        => item.Template == CurrentTemplate;

    //todo: is this needed?
    /*  protected override bool DrawItem(in SimpleCacheItem<TemplateCacheItem> item, int globalIndex, bool selected)
      {
          using var color = Im.Color.Push(ImGuiColor.Text, item.TextColor);
          var ret = Im.Selectable(item.DisplayString, selected);
          DrawPath(item.Item.Item2, item.Item.Item1);

          return ret;
      }

      protected override bool IsSelected(SimpleCacheItem<TemplateCacheItem> item, int globalIndex)
          => ReferenceEquals(item.Item.Item1, _currentTemplate);

      private static void DrawPath(string path, Template template)
      {
          if (path.Length <= 0 || template.Name == path)
              return;

          DrawRightAligned(template.Name, path, Im.Color.Get(ImGuiColor.TextDisabled));
      }

      protected bool Draw(Template? currentTemplate, string? label, float width)
      {
          _currentTemplate = currentTemplate;
          var name = label ?? "Select Template Here...";
          var ret = base.Draw("##template"u8, name, string.Empty, width, out var selection);
          CurrentSelection = selection?.Item;

          _currentTemplate = null;

          return ret;
      }*/

    public virtual bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, Template? currentTemplate, out Template? newSelection, float width)
    {
        CurrentTemplate = currentTemplate;
        bool ret;
        using (ImGuiColor.Text.Push(ImGuiColor.Text))
        {
            ret = currentTemplate is null
                ? base.Draw(label, "Select Template Here..."u8, StringU8.Empty, width, out var result)
                : base.Draw(label, _configuration.UISettings.IncognitoMode ? currentTemplate!.Incognito : currentTemplate!.Name, StringU8.Empty, width, out result);
            newSelection = ret ? result.Template : currentTemplate;
        }

        CurrentTemplate = null;
        return ret;
    }

    private void OnTemplateChange(in TemplateChanged.Arguments args)
    {
        var type = args.Type;
        if (type is TemplateChanged.Type.Created or TemplateChanged.Type.Renamed or TemplateChanged.Type.Deleted)
            CacheManager.Instance.SetDirty(CurrentId);
    }

    //todo: is this needed?
    /* private static void DrawRightAligned(string leftText, string text, Rgba32 color)
     {
         var start = Im.Item.Bounds.Minimum;
         var pos = start.X + Im.Font.CalculateSize(leftText).X;
         var maxSize = Im.Window.Position.X + Im.Window.MaximumContentRegion.X;
         var remainingSpace = maxSize - pos;
         var requiredSize = Im.Font.CalculateSize(text).X + Im.Style.ItemInnerSpacing.X;
         var offset = remainingSpace - requiredSize;
         if (Im.Scroll.MaximumY == 0)
             offset -= Im.Style.ItemInnerSpacing.X;

         if (offset < Im.Style.ItemSpacing.X)
             UiHelpers.DrawHoverTooltip(text);
         else
             Im.Window.DrawList.Text(start with { X = pos + offset }, color, text);
     }*/

    protected sealed class TemplateFilter : Utf8FilterBase<TemplateCacheItem>
    {
        public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
        {
            using var _ = ImGuiColor.Text.PushDefault();
            return base.DrawFilter(label, availableRegion);
        }

        public override bool WouldBeVisible(in TemplateCacheItem item, int globalIndex)
            => WouldBeVisible(item.Name.Utf8) || WouldBeVisible(item.Incognito.Utf8) || WouldBeVisible(item.FullPath.Utf8);

        protected override ReadOnlySpan<byte> ToFilterString(in TemplateCacheItem item, int globalIndex)
            => item.Name.Utf8;
    }

    protected override bool DrawItem(in TemplateCacheItem item, int globalIndex, bool selected)
    {
        using var color = ImGuiColor.Text.Push(ImGuiColor.Text);
        var name = _configuration.UISettings.IncognitoMode ? item.Incognito.Utf8 : item.Name.Utf8;
        var ret = Im.Selectable(name, selected);
        if (!item.FullPath.IsEmpty && !_configuration.UISettings.IncognitoMode)
        {
            Im.Line.Same();
            color.Push(ImGuiColor.Text, Im.Style[ImGuiColor.TextDisabled]);
            ImEx.TextRightAligned(item.FullPath.Utf8);
        }

        return ret;
    }

    protected override float ItemHeight
        => Im.Style.TextHeightWithSpacing;
}

public readonly struct TemplateCacheItem(Template template, string path, string name)
{
    public readonly Template Template = template;
    public readonly StringPair Name = new(name);
    public readonly StringPair Incognito = new(template.Incognito);
    public readonly StringPair FullPath = new(path);

    public static string Ordering(TemplateCacheItem item)
        => item.FullPath.Utf16.Length > 0 ? item.FullPath.Utf16 : item.Name.Utf16;
}

public sealed class TemplateCombo : TemplateComboBase
{
    private readonly TemplateManager _templateManager;
    private readonly ProfileManager _profileManager;

    public TemplateCombo(
        TemplateManager templateManager,
        ProfileManager profileManager,
        TemplateChanged templateChanged,
        PluginConfiguration configuration)
        : base(templateChanged, configuration)
    {
        _templateManager = templateManager;
        _profileManager = profileManager;
    }

    public bool Draw(Profile profile, Template? template, int templateIndex)
    {
        if (!base.Draw("##c"u8, template, out var newTemplate, Im.ContentRegion.Available.X) || newTemplate is null)
            return false;

        if (templateIndex >= 0)
            _profileManager.ChangeTemplate(profile, templateIndex, newTemplate);
        else
            _profileManager.AddTemplate(profile, newTemplate);

        return true;
    }

    protected override IEnumerable<TemplateCacheItem> GetItems()
        => _templateManager.Templates
            .Select(CreateItem)
            .OrderBy(TemplateCacheItem.Ordering);
}
