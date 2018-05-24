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
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that creates a visualizer for a <see cref="Popup"/>.
    /// </summary>
    public class PopupView : Control
    {
        private UIElement _attachmentArea;
        private System.Windows.Controls.Primitives.ButtonBase _editButton;
        private System.Windows.Controls.Primitives.ButtonBase _attachmentButton;
        private System.Windows.Controls.Primitives.ButtonBase _applyEditsButton;
        private System.Windows.Controls.Primitives.ButtonBase _cancelEditsButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupView"/> class.
        /// </summary>
        public PopupView()
            : base()
        {
            DefaultStyleKey = typeof(PopupView);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _attachmentArea = GetTemplateChild("AttachmentArea") as UIElement;
            _editButton = GetTemplateChild("EditButton") as System.Windows.Controls.Primitives.ButtonBase;
            _attachmentButton = GetTemplateChild("AttachmentButton") as System.Windows.Controls.Primitives.ButtonBase;
            _applyEditsButton = GetTemplateChild("ApplyEditsButton") as System.Windows.Controls.Primitives.ButtonBase;
            _cancelEditsButton = GetTemplateChild("CancelEditsButton") as System.Windows.Controls.Primitives.ButtonBase;

            if (_editButton != null)
            {
                _editButton.Click += EditButton_Click;
            }

            if (_applyEditsButton != null)
            {
                _applyEditsButton.Click += ApplyEditsButton_Click;
            }

            if (_cancelEditsButton != null)
            {
                _cancelEditsButton.Click += CancelEditsButton_Click;
            }

            InitPopup();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            IsEditModeEnabled = true;
        }

        private void CancelEditsButton_Click(object sender, RoutedEventArgs e)
        {
            PopupManager.CancelEditing();
            IsEditModeEnabled = false;
        }

        private async void ApplyEditsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PopupManager.IsGeoElementValid)
            {
                try
                {
                    await PopupManager.FinishEditingAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                IsEditModeEnabled = false;
            }
            else
            {
                if (!PopupManager.IsGeometryValid)
                {
                    MessageBox.Show("Geometry is in invalid state");
                }
                else
                {
                    MessageBox.Show("One or more fields are in an invalid state");
                }
            }
        }

        private void InitPopup()
        {
            if (_editButton != null)
            {
                _editButton.Visibility = Visibility.Collapsed;
            }

            if (PopupManager == null)
            {
                VisualStateManager.GoToState(this, "ViewMode" , true);
                return;
            }

            if (_attachmentArea != null)
            {
                _attachmentArea.Visibility = PopupManager.ShowAttachments ? Visibility.Visible : Visibility.Collapsed;
                if (PopupManager.ShowAttachments)
                {
                    var _ = PopupManager.AttachmentManager.FetchAttachmentsAsync();
                }
            }

            if (_editButton != null)
            {
                if (PopupManager.AllowEdit && !IsReadOnly && !IsEditModeEnabled)
                {
                    _editButton.Visibility = Visibility.Visible;
                }
            }

            VisualStateManager.GoToState(this, !PopupManager.AllowEdit || IsReadOnly || !IsEditModeEnabled ? "ViewMode" : "EditMode", true);
        }

        private void ToggleEditMode()
        {
            if (PopupManager != null && IsEditModeEnabled && !IsReadOnly && PopupManager.AllowEdit)
            {
                VisualStateManager.GoToState(this, "EditMode", true);
                PopupManager.StartEditing();
                if (_editButton != null)
                {
                    _editButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                VisualStateManager.GoToState(this, "ViewMode", true);
                if (_editButton != null && PopupManager?.AllowEdit == true && !IsReadOnly && !IsEditModeEnabled)
                {
                    _editButton.Visibility = Visibility.Collapsed;
                }
            }

            if (_attachmentButton != null)
            {
                _attachmentButton.Visibility = PopupManager.ShowAttachments && PopupManager.AllowEditAttachments ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public PopupManager PopupManager
        {
            get { return (PopupManager)GetValue(PopupManagerProperty); }
            set { SetValue(PopupManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PopupManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PopupManagerProperty =
            DependencyProperty.Register("PopupManager", typeof(PopupManager), typeof(PopupView), new PropertyMetadata(null, OnPopupManagerPropertyChanged));

        private static void OnPopupManagerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PopupView)d).InitPopup();
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PopupView), new PropertyMetadata(false, OnIsReadOnlyPropertyChanged));

        private static void OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((PopupView)d).IsEditModeEnabled = false;
            }
        }

        public bool IsEditModeEnabled
        {
            get { return (bool)GetValue(IsEditModeEnabledProperty); }
            set { SetValue(IsEditModeEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEditModeEnabledProperty =
            DependencyProperty.Register("IsEditModeEnabled", typeof(bool), typeof(PopupView), new PropertyMetadata(false, OnIsEditModeEnabledPropertyChanged));

        private static void OnIsEditModeEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PopupView)d).ToggleEditMode();
        }
    }
}