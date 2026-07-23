// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

using CommunityToolkit.Mvvm.ComponentModel;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.ViewModels;

internal enum DiagramStyleEditorPropertyKind
{
    Boolean,
    Choice,
    Font,
    Numeric,
    Text
}

internal sealed class DiagramStyleEditorPropertyRow : ObservableObject, IDisposable
{
    private readonly ObservableDiagramStyle _style;
    private readonly PropertyInfo? _localProperty;
    private readonly PropertyInfo _valueProperty;

    internal string? LocalPropertyName => _localProperty?.Name;

    internal string ValuePropertyName => _valueProperty.Name;

    public string Label { get; }

    public string ToolTip { get; }

    public bool HasLocalToggle => _localProperty is not null;

    public bool HasPlainLabel => !HasLocalToggle;

    public bool IsEditable => _style.IsEditable;

    public bool IsValueEnabled => !HasLocalToggle || (IsEditable && IsLocal);

    public bool IsBoolean => Kind == DiagramStyleEditorPropertyKind.Boolean;

    public bool IsChoice => Kind == DiagramStyleEditorPropertyKind.Choice;

    public bool IsFont => Kind == DiagramStyleEditorPropertyKind.Font;

    public bool IsNumeric => Kind == DiagramStyleEditorPropertyKind.Numeric;

    public bool IsText => Kind == DiagramStyleEditorPropertyKind.Text;

    public DiagramStyleEditorPropertyKind Kind { get; }

    public decimal Minimum { get; }

    public decimal Maximum { get; }

    public decimal Increment { get; }

    public string FormatString { get; }

    public IReadOnlyList<string> Options { get; }

    public bool IsLocal
    {
        get => _localProperty is null || Read<bool>(_localProperty);
        set
        {
            if (_localProperty is not null)
            {
                Write(_localProperty, value);
                OnPropertyChanged(nameof(IsLocal));
                OnPropertyChanged(nameof(IsValueEnabled));
            }
        }
    }

    public bool BooleanValue
    {
        get => Read<bool>(_valueProperty);
        set => WriteValue(value, nameof(BooleanValue));
    }

    public decimal NumericValue
    {
        get => Convert.ToDecimal(_valueProperty.GetValue(_style), CultureInfo.InvariantCulture);
        set
        {
            object converted = _valueProperty.PropertyType == typeof(int)
                ? decimal.ToInt32(value)
                : Convert.ToDouble(value, CultureInfo.InvariantCulture);
            WriteValue(converted, nameof(NumericValue));
        }
    }

    public string TextValue
    {
        get => Convert.ToString(_valueProperty.GetValue(_style), CultureInfo.CurrentCulture) ?? string.Empty;
        set => WriteValue(value, nameof(TextValue));
    }

    public int SelectedIndex
    {
        get => Convert.ToInt32(_valueProperty.GetValue(_style), CultureInfo.InvariantCulture);
        set => WriteValue(value, nameof(SelectedIndex));
    }

    private DiagramStyleEditorPropertyRow(
        ObservableDiagramStyle style,
        string? localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName,
        DiagramStyleEditorPropertyKind kind,
        string? optionsPropertyName = null,
        decimal minimum = 0,
        decimal maximum = decimal.MaxValue,
        decimal increment = 0.25m,
        string formatString = "0.00")
    {
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _localProperty = localPropertyName is null ? null : GetProperty(localPropertyName);
        _valueProperty = GetProperty(valuePropertyName);
        Label = ReadString(labelPropertyName);
        ToolTip = ReadString(toolTipPropertyName);
        Kind = kind;
        Minimum = minimum;
        Maximum = maximum;
        Increment = increment;
        FormatString = formatString;
        Options = optionsPropertyName is null ? Array.Empty<string>() : ReadOptions(optionsPropertyName);

        _style.PropertyChanged += Style_PropertyChanged;
    }

    public static DiagramStyleEditorPropertyRow Boolean(
        ObservableDiagramStyle style,
        string localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName)
    {
        return new DiagramStyleEditorPropertyRow(
            style,
            localPropertyName,
            valuePropertyName,
            labelPropertyName,
            toolTipPropertyName,
            DiagramStyleEditorPropertyKind.Boolean);
    }

    public static DiagramStyleEditorPropertyRow Choice(
        ObservableDiagramStyle style,
        string? localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName,
        string optionsPropertyName)
    {
        return new DiagramStyleEditorPropertyRow(
            style,
            localPropertyName,
            valuePropertyName,
            labelPropertyName,
            toolTipPropertyName,
            DiagramStyleEditorPropertyKind.Choice,
            optionsPropertyName);
    }

    public static DiagramStyleEditorPropertyRow Font(
        ObservableDiagramStyle style,
        string localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName)
    {
        return new DiagramStyleEditorPropertyRow(
            style,
            localPropertyName,
            valuePropertyName,
            labelPropertyName,
            toolTipPropertyName,
            DiagramStyleEditorPropertyKind.Font,
            nameof(ObservableDiagramStyle.FontFamilies));
    }

    public static DiagramStyleEditorPropertyRow Numeric(
        ObservableDiagramStyle style,
        string localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName,
        decimal minimum = 0,
        decimal maximum = decimal.MaxValue,
        decimal increment = 0.25m,
        string formatString = "0.00")
    {
        return new DiagramStyleEditorPropertyRow(
            style,
            localPropertyName,
            valuePropertyName,
            labelPropertyName,
            toolTipPropertyName,
            DiagramStyleEditorPropertyKind.Numeric,
            minimum: minimum,
            maximum: maximum,
            increment: increment,
            formatString: formatString);
    }

    public static DiagramStyleEditorPropertyRow Text(
        ObservableDiagramStyle style,
        string localPropertyName,
        string valuePropertyName,
        string labelPropertyName,
        string toolTipPropertyName)
    {
        return new DiagramStyleEditorPropertyRow(
            style,
            localPropertyName,
            valuePropertyName,
            labelPropertyName,
            toolTipPropertyName,
            DiagramStyleEditorPropertyKind.Text);
    }

    public void Dispose()
    {
        _style.PropertyChanged -= Style_PropertyChanged;
    }

    private PropertyInfo GetProperty(string propertyName)
    {
        return typeof(ObservableDiagramStyle).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ObservableDiagramStyle property '{propertyName}' was not found.");
    }

    private string ReadString(string propertyName)
    {
        PropertyInfo property = GetProperty(propertyName);
        return Convert.ToString(property.GetValue(property.GetMethod?.IsStatic == true ? null : _style), CultureInfo.CurrentCulture)
            ?? propertyName;
    }

    private IReadOnlyList<string> ReadOptions(string propertyName)
    {
        PropertyInfo property = GetProperty(propertyName);
        object? value = property.GetValue(property.GetMethod?.IsStatic == true ? null : _style);
        if (value is not IEnumerable values)
        {
            throw new InvalidOperationException($"ObservableDiagramStyle property '{propertyName}' is not enumerable.");
        }

        return values.Cast<object?>()
            .Select(item => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty)
            .ToArray();
    }

    private T Read<T>(PropertyInfo property)
    {
        return (T)(property.GetValue(_style)
            ?? throw new InvalidOperationException($"ObservableDiagramStyle property '{property.Name}' returned null."));
    }

    private void Write(PropertyInfo property, object value)
    {
        property.SetValue(_style, value);
    }

    private void WriteValue(object value, string notificationPropertyName)
    {
        Write(_valueProperty, value);
        OnPropertyChanged(notificationPropertyName);
    }

    private void Style_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == _localProperty?.Name ||
            e.PropertyName == _valueProperty.Name ||
            e.PropertyName == nameof(ObservableDiagramStyle.SelectedMarkTypeIndex))
        {
            OnPropertyChanged(nameof(IsLocal));
            OnPropertyChanged(nameof(IsValueEnabled));
            OnPropertyChanged(nameof(BooleanValue));
            OnPropertyChanged(nameof(NumericValue));
            OnPropertyChanged(nameof(TextValue));
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }
}

internal sealed class DiagramStyleEditorSection : IDisposable
{
    public string Header { get; }

    public IReadOnlyList<DiagramStyleEditorPropertyRow> Rows { get; }

    public DiagramStyleEditorSection(string header, params DiagramStyleEditorPropertyRow[] rows)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public void Dispose()
    {
        foreach (DiagramStyleEditorPropertyRow row in Rows)
        {
            row.Dispose();
        }
    }
}

internal sealed class DiagramStyleEditorPresentation : IDisposable
{
    public IReadOnlyList<DiagramStyleEditorSection> DiagramSections { get; }

    public IReadOnlyList<DiagramStyleEditorSection> GridSections { get; }

    public IReadOnlyList<DiagramStyleEditorSection> TitleSections { get; }

    public IReadOnlyList<DiagramStyleEditorSection> MarkSections { get; }

    public IReadOnlyList<DiagramStyleEditorSection> FretLabelSections { get; }

    public IReadOnlyList<DiagramStyleEditorSection> BarreSections { get; }

    public IEnumerable<DiagramStyleEditorSection> Sections =>
        DiagramSections
            .Concat(GridSections)
            .Concat(TitleSections)
            .Concat(MarkSections)
            .Concat(FretLabelSections)
            .Concat(BarreSections);

    public IEnumerable<DiagramStyleEditorPropertyRow> Rows => Sections.SelectMany(section => section.Rows);

    private DiagramStyleEditorPresentation(ObservableDiagramStyle style)
    {
        DiagramSections =
        [
            Section(style, nameof(ObservableDiagramStyle.DiagramLayoutGroupLabel),
                Choice(style, nameof(ObservableDiagramStyle.OrientationIsLocal), nameof(ObservableDiagramStyle.SelectedOrientationIndex), nameof(ObservableDiagramStyle.SelectedOrientationLabel), nameof(ObservableDiagramStyle.SelectedOrientationToolTip), nameof(ObservableDiagramStyle.Orientations)),
                Choice(style, nameof(ObservableDiagramStyle.LabelLayoutModelIsLocal), nameof(ObservableDiagramStyle.SelectedLabelLayoutModelIndex), nameof(ObservableDiagramStyle.SelectedLabelLayoutModelLabel), nameof(ObservableDiagramStyle.SelectedLabelLayoutModelToolTip), nameof(ObservableDiagramStyle.LabelLayoutModels))),
            Section(style, nameof(ObservableDiagramStyle.DiagramBackgroundGroupLabel),
                Text(style, nameof(ObservableDiagramStyle.DiagramColorIsLocal), nameof(ObservableDiagramStyle.DiagramColor), nameof(ObservableDiagramStyle.DiagramColorLabel), nameof(ObservableDiagramStyle.DiagramColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.DiagramOpacityIsLocal), nameof(ObservableDiagramStyle.DiagramOpacity), nameof(ObservableDiagramStyle.DiagramOpacityLabel), nameof(ObservableDiagramStyle.DiagramOpacityToolTip))),
            Section(style, nameof(ObservableDiagramStyle.NewDiagramGroupLabel),
                Integer(style, nameof(ObservableDiagramStyle.NewDiagramNumStringsIsLocal), nameof(ObservableDiagramStyle.NewDiagramNumStrings), nameof(ObservableDiagramStyle.NewDiagramNumStringsLabel), nameof(ObservableDiagramStyle.NewDiagramNumStringsToolTip), minimum: 2, maximum: 24),
                Integer(style, nameof(ObservableDiagramStyle.NewDiagramNumFretsIsLocal), nameof(ObservableDiagramStyle.NewDiagramNumFrets), nameof(ObservableDiagramStyle.NewDiagramNumFretsLabel), nameof(ObservableDiagramStyle.NewDiagramNumFretsToolTip), minimum: 1, maximum: 36)),
            Section(style, nameof(ObservableDiagramStyle.DiagramBorderGroupLabel),
                Text(style, nameof(ObservableDiagramStyle.DiagramBorderColorIsLocal), nameof(ObservableDiagramStyle.DiagramBorderColor), nameof(ObservableDiagramStyle.DiagramBorderColorLabel), nameof(ObservableDiagramStyle.DiagramBorderColorToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.DiagramBorderThicknessIsLocal), nameof(ObservableDiagramStyle.DiagramBorderThickness), nameof(ObservableDiagramStyle.DiagramBorderThicknessLabel), nameof(ObservableDiagramStyle.DiagramBorderThicknessToolTip)))
        ];

        GridSections =
        [
            Section(style, nameof(ObservableDiagramStyle.GridSpacingGroupLabel),
                Pixel(style, nameof(ObservableDiagramStyle.GridFretSpacingIsLocal), nameof(ObservableDiagramStyle.GridFretSpacing), nameof(ObservableDiagramStyle.GridFretSpacingLabel), nameof(ObservableDiagramStyle.GridFretSpacingToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridStringSpacingIsLocal), nameof(ObservableDiagramStyle.GridStringSpacing), nameof(ObservableDiagramStyle.GridStringSpacingLabel), nameof(ObservableDiagramStyle.GridStringSpacingToolTip))),
            Section(style, nameof(ObservableDiagramStyle.GridMarginGroupLabel),
                Pixel(style, nameof(ObservableDiagramStyle.GridMarginIsLocal), nameof(ObservableDiagramStyle.GridMargin), nameof(ObservableDiagramStyle.GridMarginLabel), nameof(ObservableDiagramStyle.GridMarginToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridMarginLeftIsLocal), nameof(ObservableDiagramStyle.GridMarginLeft), nameof(ObservableDiagramStyle.GridMarginLeftLabel), nameof(ObservableDiagramStyle.GridMarginLeftToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridMarginRightIsLocal), nameof(ObservableDiagramStyle.GridMarginRight), nameof(ObservableDiagramStyle.GridMarginRightLabel), nameof(ObservableDiagramStyle.GridMarginRightToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridMarginTopIsLocal), nameof(ObservableDiagramStyle.GridMarginTop), nameof(ObservableDiagramStyle.GridMarginTopLabel), nameof(ObservableDiagramStyle.GridMarginTopToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridMarginBottomIsLocal), nameof(ObservableDiagramStyle.GridMarginBottom), nameof(ObservableDiagramStyle.GridMarginBottomLabel), nameof(ObservableDiagramStyle.GridMarginBottomToolTip))),
            Section(style, nameof(ObservableDiagramStyle.GridBackgroundGroupLabel),
                Text(style, nameof(ObservableDiagramStyle.GridColorIsLocal), nameof(ObservableDiagramStyle.GridColor), nameof(ObservableDiagramStyle.GridColorLabel), nameof(ObservableDiagramStyle.GridColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.GridOpacityIsLocal), nameof(ObservableDiagramStyle.GridOpacity), nameof(ObservableDiagramStyle.GridOpacityLabel), nameof(ObservableDiagramStyle.GridOpacityToolTip))),
            Section(style, nameof(ObservableDiagramStyle.GridLineGroupLabel),
                Text(style, nameof(ObservableDiagramStyle.GridLineColorIsLocal), nameof(ObservableDiagramStyle.GridLineColor), nameof(ObservableDiagramStyle.GridLineColorLabel), nameof(ObservableDiagramStyle.GridLineColorToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.GridLineThicknessIsLocal), nameof(ObservableDiagramStyle.GridLineThickness), nameof(ObservableDiagramStyle.GridLineThicknessLabel), nameof(ObservableDiagramStyle.GridLineThicknessToolTip))),
            Section(style, nameof(ObservableDiagramStyle.GridNutGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.GridNutVisibleIsLocal), nameof(ObservableDiagramStyle.GridNutVisible), nameof(ObservableDiagramStyle.GridNutVisibleLabel), nameof(ObservableDiagramStyle.GridNutVisibleToolTip)),
                PositiveRatio(style, nameof(ObservableDiagramStyle.GridNutRatioIsLocal), nameof(ObservableDiagramStyle.GridNutRatio), nameof(ObservableDiagramStyle.GridNutRatioLabel), nameof(ObservableDiagramStyle.GridNutRatioToolTip)))
        ];

        TitleSections =
        [
            Section(style, nameof(ObservableDiagramStyle.TitleTextGroupLabel),
                Font(style, nameof(ObservableDiagramStyle.TitleFontFamilyIsLocal), nameof(ObservableDiagramStyle.TitleFontFamily), nameof(ObservableDiagramStyle.TitleFontFamilyLabel), nameof(ObservableDiagramStyle.TitleFontFamilyToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.TitleTextStyleIsLocal), nameof(ObservableDiagramStyle.SelectedTitleTextStyleIndex), nameof(ObservableDiagramStyle.SelectedTitleTextStyleLabel), nameof(ObservableDiagramStyle.SelectedTitleTextStyleToolTip), nameof(ObservableDiagramStyle.TitleTextStyles)),
                Pixel(style, nameof(ObservableDiagramStyle.TitleTextSizeIsLocal), nameof(ObservableDiagramStyle.TitleTextSize), nameof(ObservableDiagramStyle.TitleTextSizeLabel), nameof(ObservableDiagramStyle.TitleTextSizeToolTip)),
                Text(style, nameof(ObservableDiagramStyle.TitleColorIsLocal), nameof(ObservableDiagramStyle.TitleColor), nameof(ObservableDiagramStyle.TitleColorLabel), nameof(ObservableDiagramStyle.TitleColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.TitleOpacityIsLocal), nameof(ObservableDiagramStyle.TitleOpacity), nameof(ObservableDiagramStyle.TitleOpacityLabel), nameof(ObservableDiagramStyle.TitleOpacityToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.TitleLabelStyleIsLocal), nameof(ObservableDiagramStyle.SelectedTitleLabelStyleIndex), nameof(ObservableDiagramStyle.SelectedTitleLabelStyleLabel), nameof(ObservableDiagramStyle.SelectedTitleLabelStyleToolTip), nameof(ObservableDiagramStyle.TitleLabelStyles)),
                Percentage(style, nameof(ObservableDiagramStyle.TitleTextSizeModRatioIsLocal), nameof(ObservableDiagramStyle.TitleTextSizeModRatio), nameof(ObservableDiagramStyle.TitleTextSizeModRatioLabel), nameof(ObservableDiagramStyle.TitleTextSizeModRatioToolTip))),
            Section(style, nameof(ObservableDiagramStyle.TitleLayoutGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.TitleVisibleIsLocal), nameof(ObservableDiagramStyle.TitleVisible), nameof(ObservableDiagramStyle.TitleVisibleLabel), nameof(ObservableDiagramStyle.TitleVisibleToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.TitleTextAlignmentIsLocal), nameof(ObservableDiagramStyle.SelectedTitleTextAlignmentIndex), nameof(ObservableDiagramStyle.SelectedTitleTextAlignmentLabel), nameof(ObservableDiagramStyle.SelectedTitleTextAlignmentToolTip), nameof(ObservableDiagramStyle.TitleTextAlignments)),
                Pixel(style, nameof(ObservableDiagramStyle.TitleGridPaddingIsLocal), nameof(ObservableDiagramStyle.TitleGridPadding), nameof(ObservableDiagramStyle.TitleGridPaddingLabel), nameof(ObservableDiagramStyle.TitleGridPaddingToolTip)))
        ];

        MarkSections =
        [
            Section(style, nameof(ObservableDiagramStyle.MarksGroupLabel),
                Choice(style, null, nameof(ObservableDiagramStyle.SelectedMarkTypeIndex), nameof(ObservableDiagramStyle.SelectedMarkTypeLabel), nameof(ObservableDiagramStyle.SelectedMarkTypeToolTip), nameof(ObservableDiagramStyle.MarkTypes))),
            Section(style, nameof(ObservableDiagramStyle.MarkBackgroundGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.MarkVisibleIsLocal), nameof(ObservableDiagramStyle.MarkVisible), nameof(ObservableDiagramStyle.MarkVisibleLabel), nameof(ObservableDiagramStyle.MarkVisibleToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.MarkShapeIsLocal), nameof(ObservableDiagramStyle.SelectedMarkShapeIndex), nameof(ObservableDiagramStyle.SelectedMarkShapeLabel), nameof(ObservableDiagramStyle.SelectedMarkShapeToolTip), nameof(ObservableDiagramStyle.MarkShapes)),
                Text(style, nameof(ObservableDiagramStyle.MarkColorIsLocal), nameof(ObservableDiagramStyle.MarkColor), nameof(ObservableDiagramStyle.MarkColorLabel), nameof(ObservableDiagramStyle.MarkColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.MarkOpacityIsLocal), nameof(ObservableDiagramStyle.MarkOpacity), nameof(ObservableDiagramStyle.MarkOpacityLabel), nameof(ObservableDiagramStyle.MarkOpacityToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.MarkRadiusRatioIsLocal), nameof(ObservableDiagramStyle.MarkRadiusRatio), nameof(ObservableDiagramStyle.MarkRadiusRatioLabel), nameof(ObservableDiagramStyle.MarkRadiusRatioToolTip))),
            Section(style, nameof(ObservableDiagramStyle.MarkBorderGroupLabel),
                Text(style, nameof(ObservableDiagramStyle.MarkBorderColorIsLocal), nameof(ObservableDiagramStyle.MarkBorderColor), nameof(ObservableDiagramStyle.MarkBorderColorLabel), nameof(ObservableDiagramStyle.MarkBorderColorToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.MarkBorderThicknessIsLocal), nameof(ObservableDiagramStyle.MarkBorderThickness), nameof(ObservableDiagramStyle.MarkBorderThicknessLabel), nameof(ObservableDiagramStyle.MarkBorderThicknessToolTip))),
            Section(style, nameof(ObservableDiagramStyle.MarkTextGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.MarkTextVisibleIsLocal), nameof(ObservableDiagramStyle.MarkTextVisible), nameof(ObservableDiagramStyle.MarkTextVisibleLabel), nameof(ObservableDiagramStyle.MarkTextVisibleToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.MarkTextAlignmentIsLocal), nameof(ObservableDiagramStyle.SelectedMarkTextAlignmentIndex), nameof(ObservableDiagramStyle.SelectedMarkTextAlignmentLabel), nameof(ObservableDiagramStyle.SelectedMarkTextAlignmentToolTip), nameof(ObservableDiagramStyle.MarkTextAlignments)),
                Font(style, nameof(ObservableDiagramStyle.MarkFontFamilyIsLocal), nameof(ObservableDiagramStyle.MarkFontFamily), nameof(ObservableDiagramStyle.MarkFontFamilyLabel), nameof(ObservableDiagramStyle.MarkFontFamilyToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.MarkTextStyleIsLocal), nameof(ObservableDiagramStyle.SelectedMarkTextStyleIndex), nameof(ObservableDiagramStyle.SelectedMarkTextStyleLabel), nameof(ObservableDiagramStyle.SelectedMarkTextStyleToolTip), nameof(ObservableDiagramStyle.MarkTextStyles)),
                Text(style, nameof(ObservableDiagramStyle.MarkTextColorIsLocal), nameof(ObservableDiagramStyle.MarkTextColor), nameof(ObservableDiagramStyle.MarkTextColorLabel), nameof(ObservableDiagramStyle.MarkTextColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.MarkTextOpacityIsLocal), nameof(ObservableDiagramStyle.MarkTextOpacity), nameof(ObservableDiagramStyle.MarkTextOpacityLabel), nameof(ObservableDiagramStyle.MarkTextOpacityToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.MarkTextSizeRatioIsLocal), nameof(ObservableDiagramStyle.MarkTextSizeRatio), nameof(ObservableDiagramStyle.MarkTextSizeRatioLabel), nameof(ObservableDiagramStyle.MarkTextSizeRatioToolTip)))
        ];

        FretLabelSections =
        [
            Section(style, nameof(ObservableDiagramStyle.FretLabelTextGroupLabel),
                Font(style, nameof(ObservableDiagramStyle.FretLabelFontFamilyIsLocal), nameof(ObservableDiagramStyle.FretLabelFontFamily), nameof(ObservableDiagramStyle.FretLabelFontFamilyLabel), nameof(ObservableDiagramStyle.FretLabelFontFamilyToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.FretLabelTextStyleIsLocal), nameof(ObservableDiagramStyle.SelectedFretLabelTextStyleIndex), nameof(ObservableDiagramStyle.SelectedFretLabelTextStyleLabel), nameof(ObservableDiagramStyle.SelectedFretLabelTextStyleToolTip), nameof(ObservableDiagramStyle.FretLabelTextStyles)),
                Text(style, nameof(ObservableDiagramStyle.FretLabelTextColorIsLocal), nameof(ObservableDiagramStyle.FretLabelTextColor), nameof(ObservableDiagramStyle.FretLabelTextColorLabel), nameof(ObservableDiagramStyle.FretLabelTextColorToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.FretLabelTextOpacityIsLocal), nameof(ObservableDiagramStyle.FretLabelTextOpacity), nameof(ObservableDiagramStyle.FretLabelTextOpacityLabel), nameof(ObservableDiagramStyle.FretLabelTextOpacityToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.FretLabelTextSizeRatioIsLocal), nameof(ObservableDiagramStyle.FretLabelTextSizeRatio), nameof(ObservableDiagramStyle.FretLabelTextSizeRatioLabel), nameof(ObservableDiagramStyle.FretLabelTextSizeRatioToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.FretLabelTextWidthRatioIsLocal), nameof(ObservableDiagramStyle.FretLabelTextWidthRatio), nameof(ObservableDiagramStyle.FretLabelTextWidthRatioLabel), nameof(ObservableDiagramStyle.FretLabelTextWidthRatioToolTip))),
            Section(style, nameof(ObservableDiagramStyle.FretLabelLayoutGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.FretLabelTextVisibleIsLocal), nameof(ObservableDiagramStyle.FretLabelTextVisible), nameof(ObservableDiagramStyle.FretLabelTextVisibleLabel), nameof(ObservableDiagramStyle.FretLabelTextVisibleToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.FretLabelTextAlignmentIsLocal), nameof(ObservableDiagramStyle.SelectedFretLabelTextAlignmentIndex), nameof(ObservableDiagramStyle.SelectedFretLabelTextAlignmentLabel), nameof(ObservableDiagramStyle.SelectedFretLabelTextAlignmentToolTip), nameof(ObservableDiagramStyle.FretLabelTextAlignments)),
                Pixel(style, nameof(ObservableDiagramStyle.FretLabelGridPaddingIsLocal), nameof(ObservableDiagramStyle.FretLabelGridPadding), nameof(ObservableDiagramStyle.FretLabelGridPaddingLabel), nameof(ObservableDiagramStyle.FretLabelGridPaddingToolTip)))
        ];

        BarreSections =
        [
            Section(style, nameof(ObservableDiagramStyle.BarreStyleGroupLabel),
                PositiveRatio(style, nameof(ObservableDiagramStyle.BarreArcRatioIsLocal), nameof(ObservableDiagramStyle.BarreArcRatio), nameof(ObservableDiagramStyle.BarreArcRatioLabel), nameof(ObservableDiagramStyle.BarreArcRatioToolTip)),
                Percentage(style, nameof(ObservableDiagramStyle.BarreOpacityIsLocal), nameof(ObservableDiagramStyle.BarreOpacity), nameof(ObservableDiagramStyle.BarreOpacityLabel), nameof(ObservableDiagramStyle.BarreOpacityToolTip)),
                Text(style, nameof(ObservableDiagramStyle.BarreLineColorIsLocal), nameof(ObservableDiagramStyle.BarreLineColor), nameof(ObservableDiagramStyle.BarreLineColorLabel), nameof(ObservableDiagramStyle.BarreLineColorToolTip)),
                Pixel(style, nameof(ObservableDiagramStyle.BarreLineThicknessIsLocal), nameof(ObservableDiagramStyle.BarreLineThickness), nameof(ObservableDiagramStyle.BarreLineThicknessLabel), nameof(ObservableDiagramStyle.BarreLineThicknessToolTip))),
            Section(style, nameof(ObservableDiagramStyle.BarreLayoutGroupLabel),
                Boolean(style, nameof(ObservableDiagramStyle.BarreVisibleIsLocal), nameof(ObservableDiagramStyle.BarreVisible), nameof(ObservableDiagramStyle.BarreVisibleLabel), nameof(ObservableDiagramStyle.BarreVisibleToolTip)),
                Choice(style, nameof(ObservableDiagramStyle.BarreVerticalAlignmentIsLocal), nameof(ObservableDiagramStyle.SelectedBarreVerticalAlignmentIndex), nameof(ObservableDiagramStyle.SelectedBarreVerticalAlignmentLabel), nameof(ObservableDiagramStyle.SelectedBarreVerticalAlignmentToolTip), nameof(ObservableDiagramStyle.BarreVerticalAlignments)),
                Choice(style, nameof(ObservableDiagramStyle.BarreStackIsLocal), nameof(ObservableDiagramStyle.SelectedBarreStackIndex), nameof(ObservableDiagramStyle.SelectedBarreStackLabel), nameof(ObservableDiagramStyle.SelectedBarreStackToolTip), nameof(ObservableDiagramStyle.BarreStacks)))
        ];
    }

    public static DiagramStyleEditorPresentation Create(ObservableDiagramStyle style)
    {
        return new DiagramStyleEditorPresentation(style ?? throw new ArgumentNullException(nameof(style)));
    }

    public void Dispose()
    {
        foreach (DiagramStyleEditorSection section in Sections)
        {
            section.Dispose();
        }
    }

    private static DiagramStyleEditorSection Section(
        ObservableDiagramStyle style,
        string headerPropertyName,
        params DiagramStyleEditorPropertyRow[] rows)
    {
        PropertyInfo property = typeof(ObservableDiagramStyle).GetProperty(headerPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ObservableDiagramStyle property '{headerPropertyName}' was not found.");
        string header = Convert.ToString(property.GetValue(property.GetMethod?.IsStatic == true ? null : style), CultureInfo.CurrentCulture)
            ?? headerPropertyName;
        return new DiagramStyleEditorSection(header, rows);
    }

    private static DiagramStyleEditorPropertyRow Boolean(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Boolean(style, local, value, label, toolTip);

    private static DiagramStyleEditorPropertyRow Choice(
        ObservableDiagramStyle style,
        string? local,
        string value,
        string label,
        string toolTip,
        string options) => DiagramStyleEditorPropertyRow.Choice(style, local, value, label, toolTip, options);

    private static DiagramStyleEditorPropertyRow Font(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Font(style, local, value, label, toolTip);

    private static DiagramStyleEditorPropertyRow Integer(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip,
        decimal minimum,
        decimal maximum) => DiagramStyleEditorPropertyRow.Numeric(style, local, value, label, toolTip, minimum, maximum, increment: 1, formatString: "0");

    private static DiagramStyleEditorPropertyRow Percentage(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Numeric(style, local, value, label, toolTip, minimum: 0, maximum: 1, increment: 0.01m, formatString: "0.00");

    private static DiagramStyleEditorPropertyRow Pixel(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Numeric(style, local, value, label, toolTip, minimum: 0, increment: 0.25m, formatString: "0.00");

    private static DiagramStyleEditorPropertyRow PositiveRatio(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Numeric(style, local, value, label, toolTip, minimum: 0, increment: 0.01m, formatString: "0.00");

    private static DiagramStyleEditorPropertyRow Text(
        ObservableDiagramStyle style,
        string local,
        string value,
        string label,
        string toolTip) => DiagramStyleEditorPropertyRow.Text(style, local, value, label, toolTip);
}
