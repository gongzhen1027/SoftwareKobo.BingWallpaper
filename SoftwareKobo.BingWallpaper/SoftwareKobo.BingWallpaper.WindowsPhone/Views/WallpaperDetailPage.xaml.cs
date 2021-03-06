﻿// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkID=390556 上有介绍

using GalaSoft.MvvmLight.Messaging;
using SoftwareKobo.BingWallpaper.Datas;
using SoftwareKobo.BingWallpaper.Helpers;
using SoftwareKobo.BingWallpaper.Interfaces;
using SoftwareKobo.BingWallpaper.Model;
using SoftwareKobo.BingWallpaper.Services;
using SoftwareKobo.BingWallpaper.ViewModels;
using System;
using System.Collections.Generic;
using UmengSocialSDK;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;
using WinRTXamlToolkit.Tools;

namespace SoftwareKobo.BingWallpaper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WallpaperDetailPage : Page, IContinueFileSave
    {
        public WallpaperDetailPage()
        {
            this.InitializeComponent();
        }

        public WallpaperDetailPageViewModel ViewModel
        {
            get
            {
                return (WallpaperDetailPageViewModel)DataContext;
            }
        }

        public void ContinueFileSave(FileSavePickerContinuationEventArgs fileSavePickerContinuationEventArgs)
        {
            // 交由 ViewModel 继续保存。
            ViewModel.ContinueFileSave(fileSavePickerContinuationEventArgs);
        }

        public void ProcessFromViewModel(string message)
        {
            if (message == "Save Success")
            {
                SaveFileSuccess();
            }
            else if (message == "Network Error")
            {
                SideToastHelper.Error(ResourcesHelper.NetworkError);
            }
            else if (message == "Save Failed")
            {
                SideToastHelper.Error(ResourcesHelper.SaveFailed);
            }
        }

        public void SaveFileSuccess()
        {
            SideToastHelper.Success(ResourcesHelper.SaveSuccess);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;

            Messenger.Default.Unregister<string>(this, ProcessFromViewModel);
            Messenger.Default.Unregister<ImageArchive>(this, BackOrForward);

            // 显示状态栏。
            StatusBar statusBar = StatusBar.GetForCurrentView();
            await statusBar.ShowAsync();
        }

        /// <summary>
        /// 在此页将要在 Frame 中显示时进行调用。
        /// </summary>
        /// <param name="e">描述如何访问此页的事件数据。
        /// 此参数通常用于配置页。</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            ViewModel.ImageArchive = e.Parameter as ImageArchive;

            Messenger.Default.Register<string>(this, ProcessFromViewModel);
            Messenger.Default.Register<ImageArchive>(this, BackOrForward);

            // 隐藏状态栏。
            StatusBar statusBar = StatusBar.GetForCurrentView();
            await statusBar.HideAsync();
        }

        private async void BackOrForward(ImageArchive imageArchive)
        {
            // 点击上一幅或下一幅时关闭热点。
            if (btnHotspot.IsChecked == true)
            {
                btnHotspot.IsChecked = false;
            }

            // 在上一幅或下一幅图片加载完成之前显示进度圈。
            prgIsImageOpened.IsActive = true;

            // 尝试加载更多，防止下一幅按钮无法点击。
            await Global.WallpaperCollection.LoadMoreItemsAsync(1);
        }

        private void BrdHotspot_Loaded(object sender, RoutedEventArgs e)
        {
            // 计算比例，设置热点的位置。
            Border brdHotspot = (Border)sender;
            Hotspot hotspot = (Hotspot)brdHotspot.DataContext;
            double locationX = hotspot.LocationX / 100.0d * iscHotspot.ActualWidth;
            double locationY = hotspot.LocationY / 100.0d * iscHotspot.ActualHeight;
            Canvas.SetLeft(brdHotspot, locationX);
            Canvas.SetTop(brdHotspot, locationY);
        }

        private void BrdHotspot_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Border brdHotspot = (Border)sender;
            IEnumerable<Border> borders = iscHotspot.GetDescendantsOfType<Border>();
            foreach (Border border in borders)
            {
                if (border == brdHotspot)
                {
                    // 播放点击的热点的效果和显示描述文字。
                    PlayHotspotEffect(border);
                    ShowHotspotText(border);
                }
                else
                {
                    // 隐藏其他热点。
                    border.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnHotspot_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton btn = sender as ToggleButton;
            if (btn != null && btn.IsChecked == true)
            {
                // 播放所有热点的效果。
                iscHotspot.GetDescendantsOfType<Border>().ForEach(temp => PlayHotspotEffect(temp));
            }
        }

        private void BtnHotspot_Unchecked(object sender, RoutedEventArgs e)
        {
            ResetHotspot();
        }

        private async void BtnNavigateLockScreenSetting_Click(object sender, RoutedEventArgs e)
        {
            // 转到设置-锁屏界面。
            await Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private void GrdHotspot_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ResetHotspot();
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                e.Handled = true;
                Frame.GoBack();
            }
        }

        /// <summary>
        /// 播放热点效果。
        /// </summary>
        /// <param name="hotspot"></param>
        /// <param name="maxScale"></param>
        /// <param name="durationSeconds"></param>
        private void PlayHotspotEffect(UIElement hotspot, double maxScale = 1.4d, double durationSeconds = 0.4d)
        {
            #region 获取 Transform

            ScaleTransform scaleTransform = hotspot.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform
                {
                    ScaleX = 1.0d,
                    ScaleY = 1.0d
                };
                hotspot.RenderTransform = scaleTransform;
            }

            #endregion 获取 Transform

            #region X 轴动画

            DoubleAnimationUsingKeyFrames animationX = new DoubleAnimationUsingKeyFrames();
            animationX.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0d)),
                Value = 1.0d
            });
            animationX.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(durationSeconds / 2.0d)),
                Value = maxScale
            });
            animationX.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(durationSeconds)),
                Value = 1.0d
            });
            Storyboard.SetTarget(animationX, scaleTransform);
            Storyboard.SetTargetProperty(animationX, "ScaleX");

            #endregion X 轴动画

            #region Y 轴动画

            DoubleAnimationUsingKeyFrames animationY = new DoubleAnimationUsingKeyFrames();
            animationY.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0d)),
                Value = 1.0d
            });
            animationY.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(durationSeconds / 2.0d)),
                Value = maxScale
            });
            animationY.KeyFrames.Add(new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(durationSeconds)),
                Value = 1.0d
            });
            Storyboard.SetTarget(animationY, scaleTransform);
            Storyboard.SetTargetProperty(animationY, "ScaleY");

            #endregion Y 轴动画

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);
            storyboard.Begin();
        }

        /// <summary>
        /// 恢复所有热点到初始状态。
        /// </summary>
        private void ResetHotspot()
        {
            grdHotspotText.Visibility = Visibility.Collapsed;
            iscHotspot.GetDescendantsOfType<Border>().ForEach(temp =>
            {
                temp.Visibility = Visibility.Visible;
                PlayHotspotEffect(temp);
            });
        }

        private void ShowHotspotText(Border border)
        {
            Hotspot hotspot = (Hotspot)border.DataContext;
            Point hotspotPosition = border.GetPosition(relativeTo: iscHotspot);

            // 设置热点描述。
            txtHotspotText.Text = hotspot.Description + hotspot.Query;

            // 显示热点遮罩。
            grdHotspotText.Visibility = Visibility.Visible;

            // 设定热点文本位置。
            txtHotspotText.Margin = new Thickness(0, hotspotPosition.Y + border.ActualHeight + 10, 0, 0);

            PlayHotspotEffect(txtHotspotText, maxScale: 1.1d, durationSeconds: 0.4d);
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (e.ErrorMessage == "E_NETWORK_ERROR")
            {
                ViewModel.RaiseWallpaperUrlChanged();
            }
        }

        private async void BtnSinaWeiboShare_Click(object sender, RoutedEventArgs e)
        {
            UmengLink link = new UmengLink();
            link.Url = ViewModel.WallpaperUrl;
            link.Type = LinkType.Picture;
            link.Text = ViewModel.ImageArchive.GetCopyright();

            UmengClient umengClient = new SinaWeiboClient(App.appkey);
            var res = await umengClient.ShareLinkAsync(link);

            if (res.Status == ShareStatus.Success)
            {
                SideToastHelper.Success(ResourcesHelper.ShareSuccess);
            }
            else
            {
                SideToastHelper.Error(ResourcesHelper.ShareFailed);
            }
        }
    }
}