// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Chordious.Desktop.Services;

internal static class DesktopDialogAccessibility
{
    public static T Describe<T>(T control, string name, string? helpText = null)
        where T : StyledElement
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        AutomationProperties.SetName(control, name);
        if (!string.IsNullOrWhiteSpace(helpText))
        {
            AutomationProperties.SetHelpText(control, helpText);
        }

        return control;
    }

    public static void AttachInitialFocus(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        void OnOpened(object? sender, EventArgs args)
        {
            Dispatcher.UIThread.Post(
                () => FocusFirstControlIfNeeded(dialog),
                DispatcherPriority.Input);
        }

        void OnClosed(object? sender, EventArgs args)
        {
            dialog.Opened -= OnOpened;
            dialog.Closed -= OnClosed;
        }

        dialog.Opened += OnOpened;
        dialog.Closed += OnClosed;
    }

    internal static Control? FindInitialFocusCandidate(IEnumerable<Control> controls)
    {
        ArgumentNullException.ThrowIfNull(controls);

        return controls.FirstOrDefault(control =>
            control.Focusable &&
            control.IsTabStop &&
            control.IsEffectivelyEnabled &&
            control.IsEffectivelyVisible);
    }

    private static void FocusFirstControlIfNeeded(Window dialog)
    {
        Control[] controls = dialog
            .GetVisualDescendants()
            .OfType<Control>()
            .ToArray();

        if (controls.Any(control => control.IsFocused))
        {
            return;
        }

        FindInitialFocusCandidate(controls)?.Focus(NavigationMethod.Tab);
    }
}
