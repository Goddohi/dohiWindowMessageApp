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
        private readonly string filePath = "groups.json";
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
                MessageBox.Show("친구파일을 불러오지 못하였습니다.\n" + e.Message);
            }

            ObservableCollection<GroupEntity> groups = new ObservableCollection<GroupEntity>();
            File.WriteAllText(filePath, JsonConvert.SerializeObject(groups, Formatting.Indented));
            return groups;
        }
    }
}
