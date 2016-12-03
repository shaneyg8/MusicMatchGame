﻿using MusicMatchGame.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// Project adopted and based on Album Cover Match Game from channel 9 https://channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/UWP-064-Album-Cover-Match-Game-Setup-and-Working-with-Files-and-System-Folders

namespace MusicMatchGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Song> Songs;
        private ObservableCollection<StorageFile> AllSongs;

        bool _playingMusic = false;
        int _round = 0;

        public MainPage()
        {
            this.InitializeComponent();

            Songs = new ObservableCollection<Song>();
        }

       
        private async Task RetrieveFilesInFolders(
            ObservableCollection<StorageFile> list,
            StorageFolder parent)
        {
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".mp3")
                    list.Add(item);
            }

            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveFilesInFolders(list, item);
            }
        }

        private async Task<List<StorageFile>> PickRandomSongs(ObservableCollection<StorageFile> allSongs)
        {
            Random random = new Random();
            var songCount = allSongs.Count;

            var randomSongs = new List<StorageFile>();

            while (randomSongs.Count < 10)
            {
                var randomNumber = random.Next(songCount);
                var randomSong = allSongs[randomNumber];


                MusicProperties randomSongMusicProperties =
                    await randomSong.Properties.GetMusicPropertiesAsync();

                bool isDuplicate = false;
                foreach (var song in randomSongs)
                {
                    MusicProperties songMusicProperties = await song.Properties.GetMusicPropertiesAsync();
                    if (String.IsNullOrEmpty(randomSongMusicProperties.Album)
                        || randomSongMusicProperties.Album == songMusicProperties.Album)
                        isDuplicate = true;

                }

                if (!isDuplicate)
                    randomSongs.Add(randomSong);
            }

            return randomSongs;
        }

        private async Task PopulateSongList(List<StorageFile> files)
        {
            int id = 0;

            foreach (var file in files)
            {
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();

                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(
                    ThumbnailMode.MusicView,
                    200,
                    ThumbnailOptions.UseCurrentScale);

                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);

                var song = new Song();
                song.Id = id;
                song.Title = songProperties.Title;
                song.Artist = songProperties.Artist;
                song.Album = songProperties.Album;
                song.AlbumCover = albumCover;
                song.SongFile = file;

                Songs.Add(song);
                id++;
            }

        }

        private void SongGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
           //Users Selection

            _round++;
            StartCooldown();
        }

        private async Task<ObservableCollection<StorageFile>> SetupMusicList()
        {
            // This will get access to your Music Library
            StorageFolder folder = KnownFolders.MusicLibrary;
            var allSongs = new ObservableCollection<StorageFile>();
            await RetrieveFilesInFolders(allSongs, folder);
            return allSongs;
        }

        private async Task PrepareNewGame()
        {
            Songs.Clear();

            // Random Songs to be picked from your Library
            var randomSongs = await PickRandomSongs(AllSongs);

            // Getting data from selected songs
            await PopulateSongList(randomSongs);


        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            StartupProgressRing.IsActive = true;

            AllSongs = await SetupMusicList();
            await PrepareNewGame();

            StartupProgressRing.IsActive = false;

            StartCooldown();
        }

        private void StartCooldown()
        {
            _playingMusic = false;
            SolidColorBrush brush = new SolidColorBrush(Colors.Blue);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = string.Format("Get ready for round {0} ...", _round + 1);
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        private void StartCountdown()
        {
            _playingMusic = true;
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = "GO!";
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        private async void CountDown_Completed(object sender, object e)
        {
            if (!_playingMusic)
            {
                // Music Starts Playing
                var song = PickSong();

                MyMediaElement.SetSource(
                    await song.SongFile.OpenAsync(FileAccessMode.Read),
                    song.SongFile.ContentType);

                // Start countdown
                StartCountdown();
            }
        }

        private Song PickSong()
        {
            Random random = new Random();
            var unusedSongs = Songs.Where(p => p.Used == false);
            var randomNumber = random.Next(unusedSongs.Count());
            var randomSong = unusedSongs.ElementAt(randomNumber);
            randomSong.Selected = true;
            return randomSong;
        }

    }
}
