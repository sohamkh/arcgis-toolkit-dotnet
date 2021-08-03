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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UtilityNetworks;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Data source for showing a TraceConfiguration list in a <see cref="UITableView" /> with <see cref="TraceConfigurationSelected" /> event.
    /// </summary>
    internal class TraceConfigurationsTableSource : UITableViewSource, INotifyCollectionChanged
    {
        private readonly ObservableCollection<UtilityNamedTraceConfiguration> _traceConfigurations;

        internal static readonly NSString CellId = new NSString(nameof(UITableViewCell));

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public TraceConfigurationsTableSource(ObservableCollection<UtilityNamedTraceConfiguration> dataSource)
        {
            _traceConfigurations = dataSource;
            if (_traceConfigurations is INotifyCollectionChanged incc)
            {
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc);
                listener.OnEventAction = (instance, source, eventArgs) => CollectionChanged?.Invoke(this, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                incc.CollectionChanged += listener.OnEvent;
            }
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _traceConfigurations?.Count() ?? 0;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var traceConfiguration = _traceConfigurations.ElementAt(indexPath.Row);
            var cell = tableView.DequeueReusableCell(CellId, indexPath);
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, CellId);
            }

            cell.TextLabel.Text = traceConfiguration.Name;
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, false);
            TraceConfigurationSelected?.Invoke(this, _traceConfigurations.ElementAt(indexPath.Row));
        }

        public event EventHandler<UtilityNamedTraceConfiguration> TraceConfigurationSelected;
    }
}