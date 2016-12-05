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
    
    public sealed partial class MainPage : Page
    {
        //Generic Dynamic Data Collection
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

       // Getting all .mp3 files from your Music Library
       //Music Library Capability must be added
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

        //Picking Random songs instead of picking songs the same order each time

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
                //Getting Songs and their data .. Xaml Get and Set
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
            // Produces the png pictures once right or wrong answers are clicked 
            // A URI is used to indentify a resource in this case it is getting the images from the assets folder
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
            //Output of scores , Correct song, Album and so on...
            ResultTextBlock.Text = string.Format("Score: {0} Total Score after {1} Rounds: {2}", score, round, totalScore);
            TitleTextBlock.Text = String.Format("Correct Song: {0}", correctSong.Title);
            ArtistTextBlock.Text = string.Format("Performed by: {0}", correctSong.Artist);
            AlbumTextBlock.Text = string.Format("On Album: {0}", correctSong.Album);

            clickedSong.Used = true;

            correctSong.Selected = false;
            correctSong.Used = true;

            if (round >= 5)
            {
                //Once game is over it will produce this message
                //Play Again button will also appear
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

            // This will appear after play again is clicked
            InstructionTextBlock.Text = "Please Get ready ...";
            ResultTextBlock.Text = "";
            TitleTextBlock.Text = "";
            ArtistTextBlock.Text = "";
            AlbumTextBlock.Text = "";

            totalScore = 0;
            round = 0;
            // Everything above will then be set back to 0
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            StartupProgressRing.IsActive = true;

            AllSongs = await SetupMusicList();
            await PrepareNewGame();

            StartupProgressRing.IsActive = false;

            StartCooldown();
        }

        //Bar on top of the page
        private void StartCooldown()
        {
            playmusic = false;
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = string.Format("Please prepare for round {0} ...", round + 1);
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        //Once preperation is done you're then timed to guess song
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

        //After game is over you're given the play again option
        private async void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await PrepareNewGame();

            PlayAgainButton.Visibility = Visibility.Collapsed;
        }

        //Exiting the application if you want to quit the game
        private void buttonclick(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //Choosing songs at random
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
