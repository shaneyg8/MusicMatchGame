# MusicMatchGame
Music Match Game C#

#Goals
* use capabilities
* use x:bind getting data from observable collections
* create a navigation scheme
* get to grips with async and await

#How it works
* This game takes Music from your music library (If you don't have .mp3 files you can get them in the album folder above) and takes the album cover and all its data. It then plays a song at random and you must choose which song matches whatever album cover that song belongs too. A points system is in place and you will be able to see your score after each round. You have 5 rounds to guess the songs and which one matches to. 

* To start the game first remove everything from your music library and use the album folder given and place that in "Music". The app will read all the data in your library and process it.

#What did i use?
My main objecctive was to base the project off what we did in classes and what we used.
##X:Bind
Through xaml I use x:bind where it reads data from the observable collection in MainPage.xaml.cs as shown below

```
<StackPanel Grid.Row="1" Orientation="Vertical" Margin="20">
            <GridView Name="SongGridView" 
                      ItemsSource="{x:Bind Songs}" 
                      IsItemClickEnabled="True" 
                      ItemClick="SongGridView_ItemClick">
                <GridView.ItemTemplate>
                    <DataTemplate x:DataType="data:Song">
                        <Grid>
                            <Image Name="AlbumArtImage" 
                                   Height="75" 
                                   Width="75" 
                                   Source="{x:Bind AlbumCover}" />
```

This information is gotten from a data source this being an observable collection as shown below in the c# code for the observable collection Songs
```
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
```
##Page Navigation
Buttons are used for Page navigation and can be seen on the home page which is the main menu and also I use buttons to Play Again and also exit the whole application itself

## Capabilities
I wanted to make something that also used capabilities and accessing the Music Library was one of them.

## References
[Bob Taber - channel 9 and msdn UWP Series](https://channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/UWP-001-Series-Introduction)
[Navigation](https://msdn.microsoft.com/en-us/library/windows/apps/ff626521(v=vs.105).aspx)
[Exiting application](http://www.tech-recipes.com/rx/23742/create-an-exit-button-in-c-visual-studio/)
[Page navigation 2](http://www.c-sharpcorner.com/uploadfile/2e414e/basic-navigation-between-pages-using-xaml-c-sharp-in-windows-st/)
[Using x:Bind](https://msdn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension)
[Placing Images](https://msdn.microsoft.com/en-us/library/gg680265(v=pandp.11).aspx)
