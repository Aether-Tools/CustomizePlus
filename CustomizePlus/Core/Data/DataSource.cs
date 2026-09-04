using System;

namespace CustomizePlus.Core.Data;

public enum DataSource
{
    User = 0, //Default data source, created/modified by local user
    ClipboardImport = 1, //Imported from clipboard base64 string
    PoseImport = 2, //Imported from pose file
    PCPImport = 3, //Imported via Penumbra PCP integration
}
