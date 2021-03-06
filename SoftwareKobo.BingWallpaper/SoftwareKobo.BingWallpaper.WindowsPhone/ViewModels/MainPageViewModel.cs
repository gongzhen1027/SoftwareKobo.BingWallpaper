﻿using GalaSoft.MvvmLight;
using SoftwareKobo.BingWallpaper.Datas;
using SoftwareKobo.BingWallpaper.Helpers;
using SoftwareKobo.BingWallpaper.Model;
using SoftwareKobo.BingWallpaper.Services;
using SoftwareKobo.BingWallpaper.Services.Interfaces;
using System.Linq;
using System.Net.Http;

namespace SoftwareKobo.BingWallpaper.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly IBingWallpaperService _bingWallpaperService;

        private string _backgroundUrl;

        private bool _hadNotifyNetworkError = false;

        public MainPageViewModel(IBingWallpaperService bingWallpaperService)
        {
            _bingWallpaperService = bingWallpaperService;

            if (IsInDesignMode == false)
            {
                Init();
            }
        }

        public string BackgroundUrl
        {
            get
            {
                return _backgroundUrl;
            }
            private set
            {
                _backgroundUrl = value;
                RaisePropertyChanged(() => BackgroundUrl);
            }
        }
        
        public async void Init()
        {
            // 确保至少能正确执行一次，能正常设置背景。
            while (true)
            {
                try
                {
                    ImageArchiveCollection imageArchiveCollection = await _bingWallpaperService.GetWallpaperInformationsAsync(0, 1, Settings.Area);
                    if (imageArchiveCollection != null)
                    {
                        ImageArchive imageArchive = imageArchiveCollection.Images.FirstOrDefault();
                        if (imageArchive != null)
                        {
                            // 设置背景。
                            BackgroundUrl = imageArchive.GetUrlWithSize(WallpaperSize._1920x1080);
                            // 立即更新主磁贴。
                            TileHelper.UpdateTile(imageArchive);
                        }
                    }

                    break;
                }
                catch (HttpRequestException ex)
                {
                    if (_hadNotifyNetworkError == false)
                    {
                        _hadNotifyNetworkError = true;
                        SideToastHelper.Error(ResourcesHelper.NetworkError);
                    }
                }
            }
        }
    }
}