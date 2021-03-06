﻿using SoftwareKobo.BingWallpaper.Model;
using SoftwareKobo.BingWallpaper.Services;
using SoftwareKobo.BingWallpaper.Services.Interfaces;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace SoftwareKobo.BingWallpaper.BackgroundTask
{
    public sealed class UpdateTileTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            UpdateTile();
        }

        private async void UpdateTile()
        {
            try
            {
                IBingWallpaperService service = new BingWallpaperJsonService();
                ImageArchiveCollection imageArchiveCollection = await service.GetWallpaperInformationsAsync(0, 1, Settings.Area);
                ImageArchive image = imageArchiveCollection.Images.FirstOrDefault();

                string tileText = image.GetTitle();
                // ReSharper disable InconsistentNaming
                string _150x150url = image.GetUrlWithSize(WallpaperSize._150x150);
                string _310x150url = image.GetUrlWithSize(WallpaperSize._310x150);
                // ReSharper restore InconsistentNaming

                TileNotification tile = new TileNotification(TileTemplateHelper.CreateTileTemplate(tileText, _150x150url, _310x150url));
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
            }
            finally
            {
                _deferral.Complete();
            }
        }
    }
}