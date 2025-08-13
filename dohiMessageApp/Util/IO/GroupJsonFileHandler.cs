using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using WalkieDohi.Entity;
using WalkieDohi.Util.Provider;
using System.IO;
using Newtonsoft.Json;
using System.Windows;

namespace WalkieDohi.Util.IO
{
    class GroupJsonFileHandler : GroupFileProvider
    {
        private string filePath => Path.Combine(RoamingDir, fileName);

        private readonly string fileName = "groups.json";

        private string RoamingDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WalkieDohi");
       
        public ObservableCollection<GroupEntity> LoadGroups()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return JsonConvert.DeserializeObject<ObservableCollection<GroupEntity>>(File.ReadAllText(filePath));
                }
            }catch (Exception e)
            {
                MessageBox.Show("그룹파일을 불러오지 못하였습니다.\n" + e.Message);
            }

            ObservableCollection<GroupEntity> groups = new ObservableCollection<GroupEntity>();
            return groups;
        }


        public void SaveGroups(ObservableCollection<GroupEntity> Groups)
        {
            try
            {
                var json = JsonConvert.SerializeObject(Groups, Formatting.Indented);
                File.WriteAllText(filePath, json);
                MainData.Groups = Groups;
            }
            catch (Exception e)
            {
                MessageBox.Show("그룹파일을 저장하지 못하였습니다.\n" + e.Message);
            }

        }
    }
}
