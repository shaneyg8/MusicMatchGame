using MusicMatchGame.Models;
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
// Project adopted and based on Album Cover Game from channel 9 https://channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/UWP-064-Album-Cover-Match-Game-Setup-and-Working-with-Files-and-System-Folders

namespace MusicMatchGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Song> Songs;
        private ObservableCollection<StorageFile> AllSongs;

        bool playmusic = false;
        int round = 0;
        int totalScore = 0;

        public MainPage()
        {
            this.InitializeComponent();

            Songs = new ObservableCollection<Song>();
        }

       
        private async Task GetAllFiles(
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
                await GetAllFiles(list, item);
            }
        }

        private async Task<List<StorageFile>> PickRandomSongs(ObservableCollection<StorageFile> allSongs)
        {
            Random ran = new Random();
            var songCount = allSongs.Count;

            var randomSongs = new List<StorageFile>();

            while (randomSongs.Count < 10)
            {
                var randomNumber = ran.Next(songCount);
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

        private async void SongGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Ignore clicks when loading new song
            if (!playmusic) return;

            CountDown.Pause();
            MyMediaElement.Stop();

            var clickedSong = (Song)e.ClickedItem;
            var correctSong = Songs.FirstOrDefault(p => p.Selected == true);

            // Correct and Incorrect answers

            Uri uri;
            int score;
            if (clickedSong.Selected)
            {
                uri = new Uri("ms-appx:///Assets/correct.png");
                score = (int)MyProgressBar.Value;
            }
            else
            {
                uri = new Uri("ms-appx:///Assets/incorrect.png");
                score = ((int)MyProgressBar.Value) * -1;
            }
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var fileStream = await file.OpenAsync(FileAccessMode.Read);
            await clickedSong.AlbumCover.SetSourceAsync(fileStream);


            totalScore += score;
            round++;

            ResultTextBlock.Text = string.Format("Score: {0} Total Score after {1} Rounds: {2}", score, round, totalScore);
            TitleTextBlock.Text = String.Format("Correct Song: {0}", correctSong.Title);
            ArtistTextBlock.Text = string.Format("Performed by: {0}", correctSong.Artist);
            AlbumTextBlock.Text = string.Format("On Album: {0}", correctSong.Album);

            clickedSong.Used = true;

            correctSong.Selected = false;
            correctSong.Used = true;

            if (round >= 5)
            {
                InstructionTextBlock.Text = string.Format("Game over ... You scored: {0}", totalScore);
                PlayAgainButton.Visibility = Visibility.Visible;
            }
            else
            {
                StartCooldown();
            }
        }

        private async Task<ObservableCollection<StorageFile>> SetupMusicList()
        {
            // This will get access to your Music Library
            StorageFolder folder = KnownFolders.MusicLibrary;
            var allSongs = new ObservableCollection<StorageFile>();
            await GetAllFiles(allSongs, folder);
            return allSongs;
        }

        private async Task PrepareNewGame()
        {
            Songs.Clear();

            // // Random Songs to be chosen
            var randomSongs = await PickRandomSongs(AllSongs);

            // Getting data from selected songs
            await PopulateSongList(randomSongs);

            StartCooldown();

            // State management
            InstructionTextBlock.Text = "Get ready ...";
            ResultTextBlock.Text = "";
            TitleTextBlock.Text = "";
            ArtistTextBlock.Text = "";
            AlbumTextBlock.Text = "";

            totalScore = 0;
            round = 0;

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
            playmusic = false;
            SolidColorBrush brush = new SolidColorBrush(Colors.Blue);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = string.Format("Please prepare for round {0} ...", round + 1);
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        private void CountdownStr()
        {
            playmusic = true;
            SolidColorBrush brush = new SolidColorBrush(Colors.Green);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = "GO GO GO!";
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        private async void CountdownFinished(object sender, object e)
        {
            if (!playmusic)
            {
                // Music Starts Playing
                var song = ChooseSong();

                MyMediaElement.SetSource(
                    await song.SongFile.OpenAsync(FileAccessMode.Read),
                    song.SongFile.ContentType);

                // Start countdown
                CountdownStr();
            }
        }

        private async void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await PrepareNewGame();

            PlayAgainButton.Visibility = Visibility.Collapsed;
        }

        private void buttonclick(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private Song ChooseSong()
        {
            Random ran = new Random();
            var SongsNotUsed = Songs.Where(p => p.Used == false);
            var ranNumber = ran.Next(SongsNotUsed.Count());
            var ranSong = SongsNotUsed.ElementAt(ranNumber);
            ranSong.Selected = true;
            return ranSong;
        }

    }
}
