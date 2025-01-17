﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Echo.ViewModels;
using LibVLCSharp.Shared;

namespace Echo.Views
{
    public partial class VideoControlView : UserControl
    {

        public VideoControlView()
        {
            InitializeComponent();
        }
        public void Initialize(MediaPlayer mediaPlayer)
        {
            DataContext = new VideoControlViewModel(mediaPlayer);
        }
    }
}