using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Runtime.Serialization.Json;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using ServiceStack.Text;


namespace Test
{
    [Serializable]
    public class UserData
    {
        public int Day { get; set; }
        public int Rank { get; set; }
        public string Status { get; set; }
        public int Steps { get; set; }

    }


    class User
    {
        public string Name { get; set; }
        public List<UserData> Data { get; set; }
        public ChartValues<int> Steps { get; set; }
        public int AverageSteps { get; set; }
        public int BestResult { get; set; }
        public int WorstResult { get; set; }
        public string Color { get; set; }
    }


    class PeriodViewModel : UserControl, INotifyPropertyChanged
    {
        private User selectedUser;
        public ObservableCollection<User> Users { get; set; }
        public User SelectedUser
        {
            get { return selectedUser; }
            set
            {
                selectedUser = value;
                OnPropertyChanged("SelectedUser");
            }
        }


        public ObservableCollection<ObservableCollection<Day>> Period { get; set; }
        public ObservableCollection<Day> Day { get; set; }


        public PeriodViewModel()
        {
            LoadDataFromJson();


            Users = new ObservableCollection<User>();


            MakeUserList();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }


        private void LoadDataFromJson()
        {
            Period = new ObservableCollection<ObservableCollection<Day>>();
            for (int i = 1; i <= 30; i++)
            {
                string json = File.ReadAllText(Environment.CurrentDirectory + $"\\Data\\day{i}.json");
                Day = new ObservableCollection<Day>(JsonConvert.DeserializeObject<ObservableCollection<Day>>(json));
                Period.Add(Day);
            }
        }


        private void MakeUserList()
        {
            int daynum = 1;
            foreach (var day in Period)
            {
                foreach (var user in day)
                {
                    var temp = Users.SingleOrDefault(x => x.Name == user.User);
                    if (temp == null)
                    {
                        Users.Add(new User { Name = user.User, AverageSteps = user.Steps, BestResult = user.Steps, WorstResult = user.Steps, Data = new List<UserData>(), Steps = new ChartValues<int>() });
                        if (daynum != 1)
                        {
                            int ind = 1;
                            do
                            {
                                Users.Last().Steps.Add(0);
                                ind++;
                            }
                            while (ind < daynum);
                        }
                        Users.Last().Data.Add(new UserData { Day = daynum, Rank = user.Rank, Status = user.Status, Steps = user.Steps });
                        Users.Last().Steps.Add(user.Steps);
                    }
                    else
                    {
                        if ((daynum > 1) && (temp.Data.Last().Day != daynum - 1))
                            Users[Users.IndexOf(temp)].Steps.Add(0);
                        Users.Remove(temp);
                        temp.AverageSteps += user.Steps;
                        temp.BestResult = user.Steps > temp.BestResult ? user.Steps : temp.BestResult;
                        temp.WorstResult = user.Steps < temp.WorstResult ? user.Steps : temp.WorstResult;
                        temp.Data.Add(new UserData { Day = daynum, Rank = user.Rank, Status = user.Status, Steps = user.Steps });
                        temp.Steps.Add(user.Steps);
                        Users.Add(temp);
                    }
                }
                daynum++;
            }
            Users.AsParallel().ForAll(x => x.AverageSteps /= Period.Count);
            Users.AsParallel().ForAll(x =>
            {
                if ((x.BestResult - x.AverageSteps > x.AverageSteps * 20 / 100) || (x.AverageSteps - x.WorstResult > x.AverageSteps * 20 / 100))
                    x.Color = "Red";
                else
                    x.Color = "Black";
            });
        }


        private RelayCommand exportToJsonCommand;
        public RelayCommand ExportToJsonCommand
        {
            get
            {
                return exportToJsonCommand ?? (exportToJsonCommand = new RelayCommand(obj =>
                {
                    if (SelectedUser != null)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog
                        {
                            Filter = "JSON files |*.json",
                            FileName = SelectedUser.Name
                        };
                        if (saveFileDialog.ShowDialog() == true)
                            File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(SelectedUser.Data, Formatting.Indented));
                    }
                }));
            }
        }


        private RelayCommand exportToXmlCommand;
        public RelayCommand ExportToXmlCommand
        {
            get
            {
                return exportToXmlCommand ?? (exportToXmlCommand = new RelayCommand(obj =>
                {
                    if (SelectedUser != null)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog
                        {
                            Filter = "XML files |*.xml",
                            FileName = SelectedUser.Name
                        };
                        if (saveFileDialog.ShowDialog() == true)
                        {
                            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<UserData>));
                            FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                            xmlSerializer.Serialize(fileStream, SelectedUser.Data);
                        }
                    }
                }));
            }
        }


        private RelayCommand exportToCsvCommand;
        public RelayCommand ExportToCsvCommand
        {
            get
            {
                return exportToCsvCommand ?? (exportToCsvCommand = new RelayCommand(obj =>
                {
                    if (SelectedUser != null)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog
                        {
                            Filter = "CSV files |*.csv",
                            FileName = SelectedUser.Name
                        };
                        if (saveFileDialog.ShowDialog() == true)
                            File.WriteAllText(saveFileDialog.FileName, CsvSerializer.SerializeToCsv(SelectedUser.Data));
                    }
                }));
            }
        }
    }
}