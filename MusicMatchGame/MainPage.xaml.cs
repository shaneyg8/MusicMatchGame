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
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Here we will get access to the music library
            //MusicLibrary will get music from your current music library
            StorageFolder folder = KnownFolders.MusicLibrary;
            var allSongs = new ObservableCollection<StorageFile>();
            await RetrieveFiles(allSongs, folder);

            // This will choose random songs from your music library so it won't play one 
            // after another after another because then the game would be a lot more simple
            var randomSongs = await PickRandomSongs(allSongs);

        }

        private async Task RetrieveFiles(
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
                await RetrieveFiles(list, item);
            }
        }

        private async Task<List<StorageFile>> PickRandomSongs(ObservableCollection<StorageFile> allSongs)
        {
            Random random = new Random();
            var songCount = allSongs.Count;

            var ranSongs = new List<StorageFile>();

            while (ranSongs.Count < 10)
            {
                var ranNum = random.Next(songCount);
                var ranSong = allSongs[ranNum];

               // Same songs should be never picked twice

                MusicProperties ranSongMusicProperties =
                    await ranSong.Properties.GetMusicPropertiesAsync();

                bool isDuplicate = false;
                foreach (var song in ranSongs)
                {
                    MusicProperties songMusicProperties = await song.Properties.GetMusicPropertiesAsync();
                    if (String.IsNullOrEmpty(ranSongMusicProperties.Album)
                        || ranSongMusicProperties.Album == songMusicProperties.Album)
                        isDuplicate = true;

                }

                if (!isDuplicate)
                    ranSongs.Add(ranSong);
            }

            return ranSongs;
        }

        

    }
}
