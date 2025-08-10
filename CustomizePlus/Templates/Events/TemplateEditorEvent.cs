using CustomizePlusPlus.Templates.Data;
using OtterGui.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlusPlus.Templates.Events;

/// <summary>
/// Triggered when something related to template editor happens
/// </summary>
public class TemplateEditorEvent() : EventWrapper<TemplateEditorEvent.Type, Template?, TemplateEditorEvent.Priority>(nameof(TemplateEditorEvent))
{
    public enum Type
    {
        /// <summary>
        /// Called when something requests editor to be enabled.
        /// </summary>
        EditorEnableRequested,
        /// <summary>
        /// Called when something requests editor to be enabled. Stage 2 - logic after tab has been switched.
        /// </summary>
        EditorEnableRequestedStage2
    }

    public enum Priority
    {
        MainWindow = -1,
        TemplatePanel
    }
}