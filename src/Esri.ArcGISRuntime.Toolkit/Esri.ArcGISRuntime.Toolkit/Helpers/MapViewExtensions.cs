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

using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Samples
{
    public static class MapViewExtensions
    {
        /// <summary>
        /// Creates a IdentifyController property
        /// </summary>
        public static readonly DependencyProperty IdentifyControllerProperty =
            DependencyProperty.Register(nameof(IdentifyController), typeof(IIdentifyController), typeof(MapView), new PropertyMetadata(null, OnIdentifyControllerChanged));

        /// <summary>
        /// Invoked when the IdentifyController's value has changed
        /// </summary>
        private static void OnIdentifyControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue is IIdentifyController)
            {
                ((BindableIdentifyController)args.NewValue).MapView = d as MapView;
            }
        }

        /// <summary>
        /// IdentifyController getter method
        /// </summary>
        public static IIdentifyController GetIdentifyController(DependencyObject mapView)
        {
            return (mapView as MapView)?.GetValue(IdentifyControllerProperty) as IIdentifyController;
        }

        /// <summary>
        /// IdentifyController setter method
        /// </summary>
        public static void SetIdentifyController(DependencyObject mapView, IIdentifyController identifyController)
        {
            (mapView as MapView)?.SetValue(IdentifyControllerProperty, identifyController);
        }
    }
}