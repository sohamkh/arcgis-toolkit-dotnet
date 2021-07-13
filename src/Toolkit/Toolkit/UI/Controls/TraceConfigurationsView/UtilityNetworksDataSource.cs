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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Drawing;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Collections.ObjectModel;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class UtilityNetworksDataSource : IList<UtilityNetwork>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private GeoView _geoView;
        private IList<UtilityNetwork> _utilityNetworksList = new UtilityNetwork[] { };

        public UtilityNetwork this[int index] { get => ActiveUtilityNetworkList[index]; set => ActiveUtilityNetworkList[index] = value; }

        public int Count => ActiveUtilityNetworkList.Count;

        public bool IsReadOnly => ActiveUtilityNetworkList.IsReadOnly;

        private IList<UtilityNetwork> ActiveUtilityNetworkList
        {
            get
            {
                return _utilityNetworksList;
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        /// <summary>
        /// Sets the GeoView from which TraceConfigurations will be shown.
        /// </summary>
        /// <param name="view">The view from which to get Map/Scene TraceConfigurations.</param>
        public void SetGeoView(GeoView view)
        {
            if (_geoView == view)
            {
                return;
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
#else
                (_geoView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#endif
            }

            _geoView = view;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
#else

                (_geoView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#endif

                // Handle case where geoview loads map while events are being set up
                GeoViewDocumentChanged(null, null);
            }
        }

#if XAMARIN || XAMARIN_FORMS
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                GeoViewDocumentChanged(sender, e);
            }
        }
#endif

        private void GeoViewDocumentChanged(object sender, object e)
        {
            if (_geoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(mapLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                mapLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = mv.Map.RetryLoadAsync();
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private async void Doc_Loaded(object sender, EventArgs e)
        {
            // Get new TraceConfigurations collection
            ObservableCollection<UtilityNetwork> utilityNetworkCollection = new ObservableCollection<UtilityNetwork>();
            if (sender is Map map && map.UtilityNetworks.Count > 0)
            {
                _utilityNetworksList = map.UtilityNetworks;
                utilityNetworkCollection = new ObservableCollection<UtilityNetwork>(_utilityNetworksList);
            }
            else
            {
                return;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(utilityNetworkCollection);
            listener.OnEventAction = (instance, source, eventArgs) => HandleGeoViewUtilityNetworkCollectionChanged(source, eventArgs);
            listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
            utilityNetworkCollection.CollectionChanged += listener.OnEvent;
        }

        private void HandleGeoViewUtilityNetworkCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _geoView.InvokeOnMainThread(action);
#elif __ANDROID__
            _geoView.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _geoView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _geoView.Dispatcher.Invoke(action);
#endif
        }

        #region IList<UtilityNetwork> implementation

        UtilityNetwork IList<UtilityNetwork>.this[int index] { get => ActiveUtilityNetworkList?[index]; set => throw new NotImplementedException(); }

        int ICollection<UtilityNetwork>.Count => ActiveUtilityNetworkList?.Count ?? 0;

        bool ICollection<UtilityNetwork>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => ActiveUtilityNetworkList?.Count ?? 0;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => ActiveUtilityNetworkList?[index]; set => throw new NotImplementedException(); }

        void ICollection<UtilityNetwork>.Add(UtilityNetwork item) => throw new NotImplementedException();

        void ICollection<UtilityNetwork>.Clear() => throw new NotImplementedException();

        bool ICollection<UtilityNetwork>.Contains(UtilityNetwork item) => ActiveUtilityNetworkList?.Contains(item) ?? false;

        void ICollection<UtilityNetwork>.CopyTo(UtilityNetwork[] array, int arrayIndex) => ActiveUtilityNetworkList?.CopyTo(array, arrayIndex);

        IEnumerator<UtilityNetwork> IEnumerable<UtilityNetwork>.GetEnumerator() => ActiveUtilityNetworkList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ActiveUtilityNetworkList?.GetEnumerator();

        int IList<UtilityNetwork>.IndexOf(UtilityNetwork item) => ActiveUtilityNetworkList?.IndexOf(item) ?? -1;

        void IList<UtilityNetwork>.Insert(int index, UtilityNetwork item) => throw new NotImplementedException();

        bool ICollection<UtilityNetwork>.Remove(UtilityNetwork item) => throw new NotImplementedException();

        void IList<UtilityNetwork>.RemoveAt(int index) => throw new NotImplementedException();

        int IList.Add(object value) => throw new NotImplementedException();

        bool IList.Contains(object value) => ActiveUtilityNetworkList?.Contains(value) ?? false;

        void IList.Clear() => throw new NotImplementedException();

        int IList.IndexOf(object value) => ActiveUtilityNetworkList?.IndexOf(value as UtilityNetwork) ?? -1;

        void IList.Insert(int index, object value) => throw new NotImplementedException();

        void IList.Remove(object value) => throw new NotImplementedException();

        void IList.RemoveAt(int index) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) => (ActiveUtilityNetworkList as ICollection)?.CopyTo(array, index);
        #endregion IList<UtilityNetwork> implementation

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            RunOnUIThread(() =>
            {
                CollectionChanged?.Invoke(this, args);
                OnPropertyChanged("Item[]");
                if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(IList.Count));
                }
            });
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
#endif