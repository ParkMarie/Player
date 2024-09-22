using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames.Application;
using static System.Net.WebRequestMethods;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace Player
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> _playHistory = new ObservableCollection<string>();
        private string _selectedFolder;
        private string[] _audioFiles;
        private MediaPlayer _player = new MediaPlayer();
        private CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> PlayHistory
        {
            get { return _playHistory; }
            set { _playHistory = value; OnPropertyChanged(nameof(PlayHistory)); }
        }

        public string TimeInfo { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _selectedFolder = dialog.FileName;
                LoadAudioFiles();
                PlayAudio(0);
            }
        }

        private void LoadAudioFiles()
        {
            _audioFiles = Directory.GetFiles(_selectedFolder)
                                   .Where(file => file.EndsWith(".mp3") || file.EndsWith(".m4a"))
                                   .ToArray();
        }

        private void PlayAudio(int index)
        {
            try
            {
                if (index < 0 || index >= _audioFiles.Length)
                {
                    MessageBox.Show("Не удалось воспроизвести аудиофайл. Индекс вне диапазона.", "Ошибка(", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                
                var uri = new Uri(_audioFiles[index]);
                _player.Open(uri);
                _player.Play();

                AddToHistory(_audioFiles[index]);
                StartPlaybackWatcher();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка воспроизведения аудиофайла.", "Ошибка(", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToHistory(string audioFile)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PlayHistory.Add(audioFile);
            });
        }

        private void StartPlaybackWatcher()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                try
                {
                    while (_player.Position < _player.NaturalDuration.TimeSpan)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            return;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TimeInfo = $"{_player.Position:mm\\:ss} / {_player.NaturalDuration.TimeSpan:mm\\:ss}";
                            OnPropertyChanged(nameof(TimeInfo));
                        });

                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {

                }
            }, _cancellationTokenSource.Token);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = Array.IndexOf(_audioFiles, _player.Source.LocalPath);
            int previousIndex = currentIndex > 0 ? currentIndex - 1 : _audioFiles.Length - 1;
            _player.Stop();
            PlayAudio(previousIndex);
        }

        private bool isPlaying = false;

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_audioFiles == null || _audioFiles.Length == 0)
            {
                MessageBox.Show("Нет доступных аудиофайлов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_player.Position == TimeSpan.Zero)
            {
                _player.Play();
                isPlaying = true;
            }
            else if (_player.Position == _player.NaturalDuration.TimeSpan)
            {
                _player.Stop();
                isPlaying = false;
            }
            else
            {
                if (isPlaying)
                {
                    _player.Pause();
                    isPlaying = false;
                }
                else
                {
                    _player.Play();
                    isPlaying = true;
                }
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_audioFiles == null || _audioFiles.Length == 0)
            {
                MessageBox.Show("Нет доступных аудиофайлов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int currentIndex = Array.IndexOf(_audioFiles, _player.Source?.LocalPath);
            if (currentIndex == -1) currentIndex = 0;

            int nextIndex = (currentIndex + 1) % _audioFiles.Length;
            _player.Stop();
            PlayAudio(nextIndex);
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            _player.MediaEnded -= RepeatMediaEnded;
            _player.MediaEnded += RepeatMediaEnded;
        }

        private void RepeatMediaEnded(object sender, EventArgs e)
        {
            _player.Position = TimeSpan.Zero; 
            _player.Play(); 
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_audioFiles == null || _audioFiles.Length == 0)
            {
                MessageBox.Show("Нет доступных аудиофайлов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; 
            }

            Random rng = new Random();
            _audioFiles = _audioFiles.OrderBy(x => rng.Next()).ToArray(); 
            PlayAudio(0); 
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_player.NaturalDuration.HasTimeSpan) 
            {
                
                _player.Position = TimeSpan.FromSeconds(e.NewValue * _player.NaturalDuration.TimeSpan.TotalSeconds / 100);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Volume = Math.Clamp(e.NewValue / 100, 0, 1); 
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            History.Visibility = Visibility.Visible; 

        }

        private void CloseHistory_Click(object sender, RoutedEventArgs e)
        {
            History.Visibility = Visibility.Collapsed; 
        }

    }
}