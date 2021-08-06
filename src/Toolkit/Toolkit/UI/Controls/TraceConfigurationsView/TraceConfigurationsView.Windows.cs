﻿// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if !XAMARIN
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "UtilityNetworkPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "TraceConfigurationPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "AddingLocationToggle", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "ResetButton", Type = typeof(Button))]
    [TemplatePart(Name = "TraceButton", Type = typeof(Button))]
    [TemplatePart(Name = "BusyIndicator", Type = typeof(ProgressBar))]
    [TemplatePart(Name = "StatusLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "FunctionResultList", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "StartingLocationsList", Type = typeof(ListView))]
    public partial class TraceConfigurationsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        public TraceConfigurationsView()
        {
            _synchronizationContext = System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext();
            DefaultStyleKey = typeof(TraceConfigurationsView);
        }

        private ComboBox _utilityNetworkPicker;
        private ComboBox _traceConfigurationPicker;
        private ProgressBar _busyIndicator;
        private TextBlock _statusLabel;
        private ItemsControl _functionResultList;
        private ListView _startingLocationsList;
        private ObservableCollection<StartingLocationsListModel> _startingLocationsListItemsSource = new ObservableCollection<StartingLocationsListModel>();

        /// <summary>
        /// <inheritdoc />
        /// </summary>
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("UtilityNetworkPicker") is ComboBox utilityNetworkPicker)
            {
                _utilityNetworkPicker = utilityNetworkPicker;
                _utilityNetworkPicker.ItemsSource = _utilityNetworks;
                _utilityNetworks.CollectionChanged += (s, e) =>
                {
                    if (_utilityNetworkPicker != null)
                    {
                        _utilityNetworkPicker.Visibility = _utilityNetworks.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
                _utilityNetworkPicker.SelectionChanged += (s, e) => SelectedUtilityNetwork = (s as ComboBox)?.SelectedItem as UtilityNetwork;
            }

            if (GetTemplateChild("TraceConfigurationPicker") is ComboBox traceConfigurationPicker)
            {
                _traceConfigurationPicker = traceConfigurationPicker;
                _traceConfigurationPicker.ItemsSource = _traceConfigurations;
                _traceConfigurations.CollectionChanged += (s, e) =>
                {
                    if (_traceConfigurationPicker != null)
                    {
                        _traceConfigurationPicker.Visibility = _traceConfigurations.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
                _traceConfigurationPicker.SelectionChanged += (s, e) => SelectedTraceConfiguration = (s as ComboBox)?.SelectedItem as UtilityNamedTraceConfiguration;
            }

            if (GetTemplateChild("AddingLocationToggle") is ToggleButton addingLocationToggle)
            {
                addingLocationToggle.Click += (s, e) => IsAddingTraceLocation = s is ToggleButton tb && tb.IsChecked.HasValue && tb.IsChecked.Value;
            }

            if (GetTemplateChild("ResetButton") is Button resetButton)
            {
                resetButton.Click += (s, e) => Reset();
            }

            if (GetTemplateChild("TraceButton") is Button traceButton)
            {
                traceButton.Click += (s, e) => _ = TraceAsync();
            }

            if (GetTemplateChild("BusyIndicator") is ProgressBar busyIndicator)
            {
                _busyIndicator = busyIndicator;
            }

            if (GetTemplateChild("StatusLabel") is TextBlock statusLabel)
            {
                _statusLabel = statusLabel;
            }

            if (GetTemplateChild("FunctionResultList") is ItemsControl functionResultList)
            {
                _functionResultList = functionResultList;
                _functionResultList.ItemsSource = _traceFunctionResults;
                _traceFunctionResults.CollectionChanged += (s, e) =>
                {
                    if (_functionResultList != null)
                    {
                        _functionResultList.Visibility = _traceFunctionResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
            }

            if (GetTemplateChild("StartingLocationsList") is ListView startingLocationsList)
            {
                _startingLocationsList = startingLocationsList;
                _startingLocationsList.ItemsSource = _startingLocationsListItemsSource;
                _startingLocations.CollectionChanged += UpdateStartingLocationItemSourceOnCollectionChanged;
                _startingLocationsList.SelectionChanged += StartingLocationsList_SelectionChanged;
            }

            _propertyChangedAction = new Action<string>((propertyName) =>
            {
                if (propertyName == nameof(IsBusy))
                {
                    if (_busyIndicator != null)
                    {
                        _busyIndicator.Visibility = IsBusy ? Visibility.Visible : Visibility.Collapsed;
                        _busyIndicator.IsIndeterminate = IsBusy;
                    }

                    IsEnabled = !IsBusy;
                }
                else if (propertyName == nameof(Status))
                {
                    if (_statusLabel != null)
                    {
                        _statusLabel.Text = Status;
                    }
                }
            });

            Status = GetStatusBasedOnSelection();
        }

        private void StartingLocationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is StartingLocationsListModel listItem)
            {
                ShowStartingPoint(listItem.TerminalPickerModel.Element);
            }
        }

        private void UpdateStartingLocationItemSourceOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_startingLocationsList != null)
            {
                _startingLocationsList.Visibility = _startingLocations.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                if (_startingLocationsListItemsSource.Count > _startingLocations.Count)
                {
                    var list = _startingLocationsListItemsSource.Where(t => _startingLocations.All(element => element != t.TerminalPickerModel.Element)).ToList();
                    foreach (StartingLocationsListModel l in list)
                    {
                        _startingLocationsListItemsSource.Remove(l);
                    }
                }
                else if (_startingLocationsListItemsSource.Count < _startingLocations.Count)
                {
                    var list = _startingLocations.Where(element => _startingLocationsListItemsSource.All(t => t.TerminalPickerModel.Element != element)).ToList();
                    foreach (UtilityElement l in list)
                    {
                        TerminalPickerModel terminalPickerModel = new TerminalPickerModel(l, new DelegateCommand((o) =>
                        {
                            if (o is UtilityElement utilityElement)
                            {
                                DeleteStartingLocation(utilityElement);
                            }
                        }));
                        Visibility terminalPickerVisibility = (l.AssetType?.TerminalConfiguration?.Terminals.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
                        Visibility fractionAlongPickerVisibility = (l.NetworkSource.SourceType == UtilityNetworkSourceType.Edge) ? Visibility.Visible : Visibility.Collapsed;
                        _startingLocationsListItemsSource.Add(new StartingLocationsListModel(terminalPickerModel, terminalPickerVisibility, fractionAlongPickerVisibility));
                    }
                }
            }
        }

        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateGeoView(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        private bool AutoZoomToTraceResultsImpl
        {
            get { return (bool)GetValue(AutoZoomProperty); }
            set { SetValue(AutoZoomProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoZoomProperty =
            DependencyProperty.Register(nameof(AutoZoomToTraceResults), typeof(bool), typeof(TraceConfigurationsView), new PropertyMetadata(true));

        private Symbol StartingLocationSymbolImpl
        {
            get { return (Symbol)GetValue(StartingLocationSymbolProperty); }
            set { SetValue(StartingLocationSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationSymbolProperty =
            DependencyProperty.Register(nameof(StartingLocationSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnStartingLocationSymbolPropertyChanged));

        private static void OnStartingLocationSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateTraceLocationSymbol(e.NewValue as Symbol);
        }

        private Symbol ResultPointSymbolImpl
        {
            get { return (Symbol)GetValue(ResultPointSymbolProperty); }
            set { SetValue(ResultPointSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultPointSymbolProperty =
            DependencyProperty.Register(nameof(ResultPointSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultPointSymbolPropertyChanged));

        private static void OnResultPointSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Multipoint);
        }

        private Symbol ResultLineSymbolImpl
        {
            get { return (Symbol)GetValue(ResultLineSymbolProperty); }
            set { SetValue(ResultLineSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultLineSymbolProperty =
            DependencyProperty.Register(nameof(ResultLineSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultLineSymbolPropertyChanged));

        private static void OnResultLineSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Polyline);
        }

        private Symbol ResultFillSymbolImpl
        {
            get { return (Symbol)GetValue(ResultFillSymbolProperty); }
            set { SetValue(ResultFillSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultFillSymbolProperty =
            DependencyProperty.Register(nameof(ResultFillSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultFillSymbolPropertyChanged));

        private static void OnResultFillSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Polygon);
        }

        /*private Task<UtilityElement> GetElementWithTerminalAsync(MapPoint location, UtilityElement element)
        {
            var tcs = new TaskCompletionSource<UtilityElement>();
            if (GeoView is GeoView geoView && TerminalPickerTemplate is FrameworkElement terminalPicker)
            {
                terminalPicker.DataContext = new TerminalPickerModel(element, new DelegateCommand((o) =>
                {
                    if (o is UtilityElement traceLocation)
                    {
                        tcs.TrySetResult(traceLocation);
                    }

                    geoView.DismissCallout();
                }));
                geoView.ShowCalloutAt(location, terminalPicker);
            }

            return tcs.Task;
        }*/

        private class StartingLocationsListModel
        {
            internal StartingLocationsListModel(TerminalPickerModel terminalPickerModel, Visibility terminalPickerVisibility, Visibility fractionAlongPickerVisibility)
            {
                TerminalPickerModel = terminalPickerModel;
                TerminalPickerVisibility = terminalPickerVisibility;
                FractionAlongPickerVisibility = fractionAlongPickerVisibility;
            }

            public TerminalPickerModel TerminalPickerModel { get; }

            public Visibility TerminalPickerVisibility { get; }

            public Visibility FractionAlongPickerVisibility { get; }
        }

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public FrameworkElement TerminalPickerTemplate
        {
            get { return (FrameworkElement)GetValue(TerminalPickerTemplateProperty); }
            set { SetValue(TerminalPickerTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TerminalPickerTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TerminalPickerTemplateProperty =
            DependencyProperty.Register(nameof(TerminalPickerTemplate), typeof(FrameworkElement), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style used by the list view items in the underlying list view control.
        /// </summary>
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public DataTemplate ResultItemTemplate
        {
            get { return (DataTemplate)GetValue(ResultItemTemplateProperty); }
            set { SetValue(ResultItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemTemplateProperty =
            DependencyProperty.Register(nameof(ResultItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style used by the list view items in the underlying list view control.
        /// </summary>
        public Style ResultItemContainerStyle
        {
            get { return (Style)GetValue(ResultItemContainerStyleProperty); }
            set { SetValue(ResultItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ResultItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);

        /// <summary>
        /// Gets or sets the item template used to render starting locations in the list.
        /// </summary>
        public DataTemplate StartingLocationsItemTemplate
        {
            get { return (DataTemplate)GetValue(StartingLocationsItemTemplateProperty); }
            set { SetValue(StartingLocationsItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationsItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationsItemTemplateProperty =
            DependencyProperty.Register(nameof(StartingLocationsItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style used by the list view items in the underlying list view control.
        /// </summary>
        public Style StartingLocationsItemContainerStyle
        {
            get { return (Style)GetValue(StartingLocationsItemContainerStyleProperty); }
            set { SetValue(StartingLocationsItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationstItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationsItemContainerStyleProperty =
            DependencyProperty.Register(nameof(StartingLocationsItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);
    }
}
#endif