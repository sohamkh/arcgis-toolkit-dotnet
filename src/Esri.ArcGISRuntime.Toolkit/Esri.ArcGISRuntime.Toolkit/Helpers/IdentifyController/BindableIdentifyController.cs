// /*******************************************************************************
//  * Copyright 2018 Esri
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Samples
{
    public sealed class BindableIdentifyController : DependencyObject, IIdentifyController
    {
        private IdentifyController _identifyController = new IdentifyController();

        /// <summary>
        /// Initializes a new instance of the <see cref="BindableIdentifyController"/> class.
        /// </summary>
        public BindableIdentifyController()
        {
            _identifyController.IdentifyCompleted += (o, e) => OnIdentifyCompleted(e.IdentifiedGeoElement);
        }

        /// <summary>
        /// Identifies the <see cref="MapView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MapViewProperty =
            DependencyProperty.Register(nameof(MapView), typeof(MapView), typeof(BindableIdentifyController), new PropertyMetadata(null, OnMapViewPropertyChanged));

        private static void OnMapViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BindableIdentifyController)d)._identifyController.MapView = e.NewValue as MapView;
        }

        /// <summary>
        /// Gets or sets the MapView on which to perform identify operations
        /// </summary>
        public MapView MapView
        {
            get => GetValue(MapViewProperty) as MapView;
            set => SetValue(MapViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target), typeof(IPopupSource), typeof(BindableIdentifyController), new PropertyMetadata(null, OnTargetPropertyChanged));

        private static void OnTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BindableIdentifyController)d)._identifyController.Target = e.NewValue as IPopupSource;
        }

        /// <summary>
        /// Gets or sets the layer or overlay on which to perform identify operations
        /// </summary>
        public IPopupSource Target
        {
            get => GetValue(TargetProperty) as IPopupSource;
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IdentifiedGeoElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IdentifiedGeoElementProperty =
            DependencyProperty.Register(nameof(IdentifiedGeoElement), typeof(GeoElement), typeof(BindableIdentifyController), null);

        /// <summary>
        /// Gets the identified GeoElement
        /// </summary>
        public GeoElement IdentifiedGeoElement
        {
            get => GetValue(IdentifiedGeoElementProperty) as GeoElement;
            private set => SetValue(IdentifiedGeoElementProperty, value);
        }

        public event EventHandler<IdentifyEventArgs> IdentifyCompleted;

        private void OnIdentifyCompleted(GeoElement identifiedGeoElement)
        {
            IdentifyCompleted?.Invoke(this, new IdentifyEventArgs(identifiedGeoElement));
        }
    }
}
