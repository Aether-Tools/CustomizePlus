using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.UI.Windows.Controls;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public partial class BoneEditorPanel
{
    private static readonly Vector4 AxisXHeaderColor = new(0.80f, 0.28f, 0.28f, 0.28f);
    private static readonly Vector4 AxisYHeaderColor = new(0.35f, 0.72f, 0.35f, 0.24f);
    private static readonly Vector4 AxisZHeaderColor = new(0.32f, 0.52f, 0.95f, 0.26f);

    private static readonly Vector4 AxisXCellColor = new(0.80f, 0.28f, 0.28f, 0.08f);
    private static readonly Vector4 AxisYCellColor = new(0.35f, 0.72f, 0.35f, 0.07f);
    private static readonly Vector4 AxisZCellColor = new(0.32f, 0.52f, 0.95f, 0.08f);
    private static readonly Vector4 AxisXEditedCellColor = new(0.80f, 0.28f, 0.28f, 0.18f);
    private static readonly Vector4 AxisYEditedCellColor = new(0.35f, 0.72f, 0.35f, 0.16f);
    private static readonly Vector4 AxisZEditedCellColor = new(0.32f, 0.52f, 0.95f, 0.18f);

    //private readonly TemplateFileSystemSelector _templateFileSystemSelector;
    private readonly TemplateFileSystem _fileSystem;
    private readonly TemplateEditorManager _editorManager;
    private readonly PluginConfiguration _configuration;
    private readonly GameObjectService _gameObjectService;
    private readonly ActorAssignmentUi _actorAssignmentUi;
    private readonly PopupSystem _popupSystem;
    private readonly Logger _logger;

    private BoneAttribute _editingAttribute;
    private int _precision;

    private bool _isShowLiveBones;
    private bool _isMirrorModeEnabled;

    private Dictionary<BoneData.BoneFamily, bool> _groupExpandedState = new();

    private bool _openSavePopup;

    private bool _isUnlocked = false;

    private string _boneSearch = string.Empty;

    private float _propagateButtonXPos = 0;
    private float _parentRowScreenPosY = 0;

    // favorite bone stuff
    private HashSet<string> _favoriteBones;

    private string? _pendingClipboardText;
    private string? _pendingImportText;
    public bool HasChanges => _editorManager.HasChanges;
    public bool IsEditorActive => _editorManager.IsEditorActive;
    public bool IsEditorPaused => _editorManager.IsEditorPaused;
    public bool IsCharacterFound => _editorManager.IsCharacterFound;

    public BoneEditorPanel(
        // TemplateFileSystemSelector templateFileSystemSelector,
        TemplateFileSystem fileSystem,
        TemplateEditorManager editorManager,
        PluginConfiguration configuration,
        GameObjectService gameObjectService,
        ActorAssignmentUi actorAssignmentUi,
        PopupSystem popupSystem,
        Logger logger)
    {
        //   _templateFileSystemSelector = templateFileSystemSelector;
        _fileSystem = fileSystem;
        _editorManager = editorManager;
        _configuration = configuration;
        _gameObjectService = gameObjectService;
        _actorAssignmentUi = actorAssignmentUi;
        _popupSystem = popupSystem;
        _logger = logger;

        _isShowLiveBones = configuration.EditorConfiguration.ShowLiveBones;
        _isMirrorModeEnabled = configuration.EditorConfiguration.BoneMirroringEnabled;
        _precision = configuration.EditorConfiguration.EditorValuesPrecision;
        _editingAttribute = configuration.EditorConfiguration.EditorMode;
        _favoriteBones = new HashSet<string>(_configuration.EditorConfiguration.FavoriteBones);
    }

    public bool EnableEditor(Template template)
    {
        if (_editorManager.EnableEditor(template))
        {
            //_editorManager.SetLimitLookupToOwned(_configuration.EditorConfiguration.LimitLookupToOwnedObjects);
            return true;
        }

        return false;
    }

    public bool DisableEditor()
    {
        if (!_editorManager.HasChanges)
            return _editorManager.DisableEditor();

        if (_editorManager.HasChanges && !IsEditorActive)
            throw new Exception("Invalid state in BoneEditorPanel: has changes but editor is not active");

        _openSavePopup = true;

        return false;
    }
}
