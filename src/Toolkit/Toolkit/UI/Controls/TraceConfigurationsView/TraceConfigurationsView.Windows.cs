// /*******************************************************************************
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
#if !XAMARIN
using System;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Popups;
#else
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "List", Type = typeof(ListView))]
    public partial class TraceConfigurationsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        public TraceConfigurationsView()
        {
            DefaultStyleKey = typeof(TraceConfigurationsView);
        }

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

            CboNamedTraceConfigurations = GetTemplateChild("cboNamedTraceConfigurations") as ComboBox;
            CboUtilityNetworks = GetTemplateChild("cboUtilityNetworks") as ComboBox;
            TxtUtilityNetworks = GetTemplateChild("txtUtilityNetworks") as TextBlock;
            _txtStatus = GetTemplateChild("txtStatus") as TextBlock;
            _pbMapLoad = GetTemplateChild("pbMapLoad") as ProgressBar;
#if NETFX_CORE
            _traceConfigurationsViewContent = GetTemplateChild("traceConfigurationsViewContent") as Windows.UI.Xaml.Controls.Grid;
#else
            _traceConfigurationsViewContent = GetTemplateChild("traceConfigurationsViewContent") as System.Windows.Controls.Grid;
#endif
            // TerminalPicker = GetTemplateChild("TerminalPicker") as UIElement;

            if (CboNamedTraceConfigurations != null)
            {
                CboNamedTraceConfigurations.ItemsSource = _dataSource;
            }

            if (CboUtilityNetworks != null)
            {
                CboUtilityNetworks.ItemsSource = _utilityNetworksDataSource;
            }

            var btnTrace = GetTemplateChild("btnTrace") as Button;
            btnTrace.Click += OnTraceClick;
            var btnReset = GetTemplateChild("btnReset") as Button;
            btnReset.Click += OnResetClick;
            var btnAddStartingPoints = GetTemplateChild("btnAddStartingPoints") as ToggleButton;
            btnAddStartingPoints.Click += OnToggleGeoViewTapped;
        }

        private ComboBox _cboNamedTraceConfigurations;
        private ComboBox _cboUtilityNetworks;
        private TextBlock TxtUtilityNetworks;
        private TextBlock _txtStatus;
        private ProgressBar _pbMapLoad;
#if NETFX_CORE
        private Windows.UI.Xaml.Controls.Grid _traceConfigurationsViewContent;
#else
        private System.Windows.Controls.Grid _traceConfigurationsViewContent;
#endif

        private ComboBox CboNamedTraceConfigurations
        {
            get => _cboNamedTraceConfigurations;
            set
            {
                if (value != _cboNamedTraceConfigurations)
                {
                    if (_cboNamedTraceConfigurations != null)
                    {
                        _cboNamedTraceConfigurations.SelectionChanged -= TraceConfigurationSelectionChanged;
                    }

                    _cboNamedTraceConfigurations = value;
                    if (_cboNamedTraceConfigurations != null)
                    {
                        _cboNamedTraceConfigurations.SelectionChanged += TraceConfigurationSelectionChanged;
                    }
                }
            }
        }

        private ComboBox CboUtilityNetworks
        {
            get => _cboUtilityNetworks;
            set
            {
                if (value != _cboUtilityNetworks)
                {
                    if (_cboUtilityNetworks != null)
                    {
                        _cboUtilityNetworks.SelectionChanged -= UtilityNetworkSelectionChanged;
                    }

                    _cboUtilityNetworks = value;
                    if (_cboUtilityNetworks != null)
                    {
                        _cboUtilityNetworks.SelectionChanged += UtilityNetworkSelectionChanged;
                    }
                }
            }
        }

        private void TraceConfigurationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _pbMapLoad.Visibility = Visibility.Collapsed;
                _pbMapLoad.IsIndeterminate = false;
                _traceConfigurationsViewContent.Visibility = Visibility.Visible;
            }

            ClearStatus();
        }

        private void UtilityNetworkSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is UtilityNetwork un)
            {
                if (e.AddedItems.Count > 1)
                {
                    CboUtilityNetworks.Visibility = Visibility.Visible;
                    TxtUtilityNetworks.Visibility = Visibility.Visible;
                }

                SetUtilityNetwork(un);
            }
        }

        public void OnToggleGeoViewTapped(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            if (toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value)
            {
                AddViewTapped();
            }
            else
            {
                RemoveViewTapped();
            }
        }

        private void OnTraceClick(object sender, RoutedEventArgs e)
        {
            if (CboNamedTraceConfigurations.SelectedValue is UtilityNamedTraceConfiguration namedTraceConfiguration)
            {
                SelectAndNavigateToTraceConfiguration(namedTraceConfiguration);
            }
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            ResetMap();
        }

        private void ShowCallout(UtilityElement element, GeoViewInputEventArgs e)
        {
            GeoView.DismissCallout();
            FrameworkElement calloutContent = SelectorTemplate.LoadContent() as FrameworkElement;
            calloutContent.DataContext = element;
            GeoView.ShowCalloutAt(e.Location, calloutContent);
        }

        private void ShowStatus(string status, bool isLoading = false)
        {
            _txtStatus.Text = status;
            if (isLoading)
            {
                _pbMapLoad.Visibility = Visibility.Visible;
                _pbMapLoad.IsIndeterminate = true;
            }
            else
            {
                _pbMapLoad.Visibility = Visibility.Collapsed;
                _pbMapLoad.IsIndeterminate = false;
            }
        }

        private void ClearStatus()
        {
            if (CboNamedTraceConfigurations.SelectedValue is UtilityNamedTraceConfiguration namedTraceConfiguration)
            {
                _txtStatus.Text = "Starting locations required: " + ((int)namedTraceConfiguration.MinimumStartingLocations);
            }
            else
            {
                _txtStatus.Text = "No Trace Configurations found.";
            }
        }

        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        private UtilityNetwork UtilityNetworkImpl
        {
            get { return (UtilityNetwork)GetValue(UtilityNetworkProperty); }
            set { SetValue(UtilityNetworkProperty, value); }
        }

        private Exception TraceExceptionImpl
        {
            get { return (Exception)GetValue(TraceExceptionProperty); }
            set { SetValue(TraceExceptionProperty, value); }
        }

        /*private IEnumerable<UtilityTraceResult> TraceResultsImpl
        {
            get { return (IEnumerable<UtilityTraceResult>)GetValue(TraceResultsProperty); }
            set { SetValue(TraceResultsProperty, value); }
        }*/

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="UtilityNetwork" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworkProperty =
            DependencyProperty.Register(nameof(UtilityNetwork), typeof(UtilityNetwork), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnUtilityNetworkPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TraceException" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceExceptionProperty =
            DependencyProperty.Register(nameof(TraceException), typeof(object), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnTraceExceptionPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TraceResults" /> dependency property.
        /// </summary>
        /*public static readonly DependencyProperty TraceResultsProperty =
            DependencyProperty.Register(nameof(TraceResults), typeof(object), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnTraceResultsPropertyChanged));*/

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d)._utilityNetworksDataSource.SetGeoView(e.NewValue as GeoView);
            ((TraceConfigurationsView)d)._dataSource.SetGeoView(e.NewValue as GeoView);
        }

        private static void OnUtilityNetworkPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d)._dataSource.SetUtilityNetwork(e.NewValue as UtilityNetwork);
        }

        private static void OnTraceExceptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d)._dataSource.SetTraceException(e.NewValue as Exception);
        }

        /*private static void OnTraceResultsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d)._dataSource.SetTraceResults(e.NewValue as IEnumerable<UtilityTraceResult>);
        }*/

        /// <summary>
        /// Gets or sets the item template used to render TraceConfiguration entries in the list.
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
        /// Gets or sets the template used for callouts.
        /// </summary>
        public DataTemplate SelectorTemplate
        {
            get { return (DataTemplate)GetValue(SelectorTemplateProperty); }
            set { SetValue(SelectorTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectorTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectorTemplateProperty =
            DependencyProperty.Register(nameof(SelectorTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));


        /*public async void DisplayException(Exception ex)
        {
#if UAP10_0_17134
            await new MessageDialog(ex.Message).ShowAsync();
#else
            MessageBox.Show(ex.Message);
#endif
        }*/
    }
}
#endif