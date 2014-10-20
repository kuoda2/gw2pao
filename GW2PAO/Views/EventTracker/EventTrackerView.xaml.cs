﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GW2PAO.Controllers.Interfaces;
using GW2PAO.ViewModels.EventTracker;
using NLog;

namespace GW2PAO.Views.EventTracker
{
    /// <summary>
    /// Interaction logic for EventTrackerView.xaml
    /// </summary>
    public partial class EventTrackerView : OverlayWindow
    {
        /// <summary>
        /// Default logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Actual height of an event in the list
        /// </summary>
        private double eventHeight;

        /// <summary>
        /// Count used for keeping track of when we need to adjust our
        /// maximum height/width if the number of visible events
        /// changes
        /// </summary>
        private int prevVisibleEventsCount = 0;

        /// <summary>
        /// Height before collapsing the control
        /// </summary>
        private double beforeCollapseHeight;

        /// <summary>
        /// Event tracker controller
        /// </summary>
        private IEventsController controller;

        /// <summary>
        /// Event tracker view model
        /// </summary>
        private EventTrackerViewModel viewModel;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="eventListController">The event tracker controller</param>
        public EventTrackerView(IEventsController eventListController)
        {
            logger.Debug("New EventTrackerView created");
            this.controller = eventListController;
            this.viewModel = new EventTrackerViewModel(this.controller);
            this.DataContext = this.viewModel;
            InitializeComponent();

            this.eventHeight = new WorldEventView().Height;

            this.ResizeHelper.InitializeResizeElements(this.ResizeHeight, null);
            this.Loaded += EventTrackerView_Loaded;
        }

        private void EventTrackerView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up resize snapping
            this.ResizeHelper.SnappingHeightOffset = 7;
            this.ResizeHelper.SnappingThresholdHeight = (int)this.TitleBar.ActualHeight;
            this.ResizeHelper.SnappingIncrementHeight = (int)this.eventHeight;

            // Save the height values for use when collapsing the window
            this.RefreshWindowHeights();
            this.Height = GW2PAO.Properties.Settings.Default.EventTrackerHeight;

            this.EventsContainer.LayoutUpdated += EventsContainer_LayoutUpdated;
            this.Closing += EventTrackerView_Closing;
            this.beforeCollapseHeight = this.Height;
        }

        /// <summary>
        /// Refreshes the MinHeight and MaxHeight of the window
        /// based on collapsed status and number of visible items
        /// </summary>
        private void RefreshWindowHeights()
        {
            var visibleObjsCount = this.viewModel.WorldEvents.Count(o => o.IsVisible);
            if (this.EventsContainer.Visibility == System.Windows.Visibility.Visible)
            {
                // Expanded
                this.MinHeight = eventHeight * 2 + this.TitleBar.ActualHeight; // Minimum of 2 events
                if (visibleObjsCount < 2)
                    this.MaxHeight = this.MinHeight;
                else
                    this.MaxHeight = (visibleObjsCount * eventHeight) + this.TitleBar.ActualHeight + 2;
            }
            else
            {
                // Collapsed, don't touch the height
            }
        }

        private void EventsContainer_LayoutUpdated(object sender, EventArgs e)
        {
            var visibleObjsCount = this.viewModel.WorldEvents.Count(o => o.IsVisible);
            if (prevVisibleEventsCount != visibleObjsCount)
            {
                prevVisibleEventsCount = visibleObjsCount;
                this.RefreshWindowHeights();
            }
        }

        private void EventTrackerView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                Properties.Settings.Default.EventTrackerHeight = this.Height;
                Properties.Settings.Default.EventTrackerWidth = this.Width;
                Properties.Settings.Default.EventTrackerX = this.Left;
                Properties.Settings.Default.EventTrackerY = this.Top;
                Properties.Settings.Default.Save();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.controller.Stop();
            logger.Debug("EventTrackerView closed");
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This prevents Aero snapping
            if (this.ResizeMode != System.Windows.ResizeMode.NoResize)
            {
                this.ResizeMode = System.Windows.ResizeMode.NoResize;
                this.UpdateLayout();
            }

            this.DragMove();
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.ResizeMode == System.Windows.ResizeMode.NoResize)
            {
                // Restore resize grips (removed on mouse-down to prevent Aero snapping)
                this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
                this.UpdateLayout();
            }
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CollapseExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.EventsContainer.Visibility == System.Windows.Visibility.Visible)
            {
                this.beforeCollapseHeight = this.Height;
                this.MinHeight = this.TitleBar.ActualHeight;
                this.MaxHeight = this.TitleBar.ActualHeight;
                this.Height = this.TitleBar.ActualHeight;
                this.EventsContainer.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.EventsContainer.Visibility = System.Windows.Visibility.Visible;
                this.RefreshWindowHeights();
                this.Height = this.beforeCollapseHeight;
            }
        }

        private void TitleImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Image image = sender as Image;
                ContextMenu contextMenu = image.ContextMenu;
                contextMenu.PlacementTarget = image;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
    }
}

