using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using FindClicue.VkHelpers;
using System.Collections.Generic;

namespace FindClicue
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
          //Создание Списка смежности
       List<List<int>> g = new List<List<int>>();
       Dictionary<string, string> FIRSTNAME = new Dictionary<string, string>(), LASTNAME = new Dictionary<string, string>();
       Dictionary<string, int> IndexForId = new Dictionary<string, int>(), Used_users = new Dictionary<string, int>();
       Dictionary<int, string> NumForId = new Dictionary<int, string>();
       int cnt = 0;

            static long[,] G;
            static int n;
            static List<int> Ramcey(ref long[,] ver, ref long[] used, ref int n)
            {
                List<int> ans = new List<int>();
                int fl = 0, single = 0;
                for (int i = 0; i < n; ++i)
                {
                    if (used[i] == 0)
                    {
                        ++fl;
                        single = i;
                    }
                }
                if (fl == 0)
                    return ans;
                if (fl == 1)
                {
                    ans.Add(single);
                    return ans;
                }
                int v = -1;
                for (int i = 0; i < n; ++i)
                    if (used[i] == 0)
                    {
                        v = i;
                        break;
                    }
                long[] used2 = new long[n];
                for (int i = 0; i < n; ++i)
                    used2[i] = used[i];
                for (int i = 0; i < n; ++i)
                    if (ver[v, i] != 1)
                    {
                        used2[i] = 1;
                    }
                used2[v] = 1;
                List<int> P1 = Ramcey(ref ver, ref used2, ref n);
                P1.Add(v);
                used2 = used;
                for (int i = 0; i < n; ++i)
                    if (ver[v, i] == 1)
                    {
                        used2[i] = 1;
                    }
                used2[v] = 1;
                List<int> P2 = Ramcey(ref ver, ref used2, ref n);
                if (P1.Count > P2.Count)
                    return P1;
                else
                    return P2;
            }

            List<int> Find_cl()
            {
                int n;
                
                n = cnt;
                G = new long[n, n];
                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < g[i].Count; ++j)
                    {
                            G[i, g[i][j]] = 1;
                            G[g[i][j], i] = 1;
                    }    
                }
                long[] used = new long[n];
                List<int> clique = Ramcey(ref G, ref used, ref cnt);
                return clique;
            }


        public MainWindow()
        {
            InitializeComponent();
            webBrowser.Visibility = Visibility.Visible;
            webBrowser.Navigate(String.Format("https://oauth.vk.com/authorize?client_id={0}&scope={1}&redirect_uri={2}&display=page&response_type=token", ConfigurationSettings.AppSettings["VKAppId"], ConfigurationSettings.AppSettings["VKScope"], ConfigurationSettings.AppSettings["VKRedirectUri"]));
        }

        private string VkRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();
            var reader = new StreamReader(response.GetResponseStream());
            var responseText = reader.ReadToEnd();
            return responseText;
        }
      
     
        public class Data
        {
            public string[] response { get; set; }
        }

        public class Users_names
        {
            public UserResponse1[] response { get; set; }
        }

        public class UserResponse1
        {
            public string uid { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string photo { get; set; }
        }

        private void BadSequence()
        {
            MessageBox.Show("Произошла ошибка. Проверьте правильность id", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void BuildGraph(string PersonId, ulong deep, string pred)
        {
            if (deep == 0)
                return;

            //Получаем по id дополнительную информацию для Person vvv
            var listing = string.Format("https://api.vk.com/method/users.get?user_ids={0}", PersonId);
            var responseText1 = VkRequest(listing);
            var field_User = JsonConvert.DeserializeObject<Users_names>(responseText1);
            if(field_User.response == null)
            {
                BadSequence();
                return;
            }
            PersonId = field_User.response[0].uid;
            FIRSTNAME[PersonId] = field_User.response[0].first_name;
            LASTNAME[PersonId] = field_User.response[0].last_name;
            IndexForId[PersonId] = cnt++;
            NumForId[cnt - 1] = PersonId;
            Used_users[PersonId] = 1;

           
            //Получили id всех пользователей-друзей Person ^^^
            int id = 0, cur = 1;
            for(int i = PersonId.Length - 1; i >= 0; --i)
            {
                if(PersonId[i] >= '0' && PersonId[i] <= '9')
                {
                    id += (PersonId[i] - '0') * cur;
                    cur *= 10;
                }
            }

            var tree = string.Format("https://api.vk.com/method/friends.get?user_id={0}", id);
            var responseText = VkRequest(tree);
            var users = JsonConvert.DeserializeObject<Data>(responseText);
            
            if(users.response == null)
            {
                g.Add(new List<int>());
                return;
            }

            // Массив для добавления к общему графу друзей для пользователя с id = PersonId
            List<int> Nei = new List<int>();

            // Запоминаем всех друзей пользователей - друзей чел с id =  PersonId
            foreach (var User in users.response)
            {
                listing = string.Format("https://api.vk.com/method/users.get?user_ids={0}", User);
                responseText1 = VkRequest(listing);
                field_User = JsonConvert.DeserializeObject<Users_names>(responseText1);

                if (!IndexForId.ContainsKey(User))
                {
                    IndexForId[User] = cnt++;
                    NumForId[cnt - 1] = User;
                    FIRSTNAME[User] = field_User.response[0].first_name;
                    LASTNAME[User] = field_User.response[0].last_name;
                }

                Nei.Add(IndexForId[User]);
            }

            g.Add(Nei);
            
            if (ulong.Parse(deepFind.Text) >= 2)
            {
                foreach(string User in users.response)
                {
                    if(!Used_users.ContainsKey(User))
                        BuildGraph(User, deep - 1, PersonId);
                }
            }
            else
            {
                foreach (string User1 in users.response)
                {
                    foreach (string User2 in users.response)
                    { 
                        if (User1 == User2)
                            continue;

                            id = 0;
                            cur = 1;
                            for(int i = User1.Length - 1; i >= 0; --i)
                            {
                                if(User1[i] >= '0' && User1[i] <= '9')
                                {
                                    id += (User1[i] - '0') * cur;
                                    cur *= 10;
                                }
                            }
                        var treeU1 = string.Format("https://api.vk.com/method/friends.get?user_id={0}", id);
                        var responseTextU1 = VkRequest(treeU1);
                        var frindsUser1 = JsonConvert.DeserializeObject<Data>(responseTextU1);

                        if (frindsUser1.response == null)
                            continue;

                        int fl = 0;
                        foreach (var User3 in frindsUser1.response)
                        {
                            if(User3 == User2)
                            {
                                fl = 1;
                                break;
                            }
                        }

                            int num1 = IndexForId[User1];
                            int num2 = IndexForId[User2];

                            int fl2 = 0;
                            if (g.Count > num1 && g.Count > num2)
                            {
                                foreach (int Yk in g[num1])
                                {
                                    if (Yk == num2)
                                    {
                                        fl2 = 1;
                                        break;
                                    }
                                }
                            }
                        if(fl == 1 && fl2 == 0)
                        {
                             while(g.Count <= num1)
                             {
                                   List<int> newlist = new List<int>();
                                   g.Add(newlist);
                             }
                            g[num1].Add(num2);
                            g[num1].Add(IndexForId[PersonId]);
                              while (g.Count <= num2)
                             {
                                   List<int> newlist = new List<int>();
                                  g.Add(newlist);
                              }
                             g[num2].Add(num1);
                             g[num2].Add(IndexForId[PersonId]);
                        }
                    }
                }
            }
        }
        
/*ulong n = cnt;//кол-во вершин в графе
//vector<vector<ulong>>g;//граф задан списком смежности
List<ulong>max_cl = new List<ulong>();//вектор для хранения наибольшей клики
List<ulong>can = new List<ulong>(), not = new List<ulong>(), comsub = new List<ulong>();//вектора для алгоритма Брона-Кербоша: для смежных вершин с текущей, для использованных вершин, для найденной клики на текущем шаге

bool not_dont_contein_ver_conected_all_from_canditates(ref List<ulong> no, ref List<ulong> candidates)
{
	int fl = 0;
	for (int i = 0; i < no.Count(); ++i)
	{
		fl = 0;
		for (int j = 0; j < can.Count(); ++j)
		{
			if (g[no[i]].find(can[j]) != true)
			{
				++fl;
			}
		}
		if (fl == can.Count())
			return false;
	}
	return true;
}

void find_clicue(ref List<ulong> candidates, ref List<ulong> not)
{
	while (!candidates.empty() && not_dont_contein_ver_conected_all_from_canditates(not, candidates))
	{
		ulong v = candidates.back();
		candidates.pop_back();
		comsub.push_back(v);

		List<ulong> new_not, new_candidates;
		for (int i = 0; i < not.Count(); ++i)
		{
			if (find(g[v].begin(), g[v].end(), not[i]) != g[v].end())
				new_not.push_back(not[i]);
		}
		for (int i = 0; i < candidates.Count(); ++i)
		{
			if (find(g[v].begin(), g[v].end(), candidates[i]) != g[v].end())
				new_candidates.push_back(candidates[i]);
		}
		if (new_not.empty() && new_candidates.empty())
		{
			if (comsub.Count() > max_cl.Count())
				max_cl = comsub;
		}
		else
		{
			find_clicue(new_candidates, new_not);
		}
		if (find(comsub.begin(), comsub.end(), v) != comsub.end())
			comsub.erase(find(comsub.begin(), comsub.end(), v));

		if (candidates.find(v) != candidates.end())
            candidates.erase(candidates.find(v));
		not.push_back(v);
	}
}

        private void GetAns()
        {
            FindedMaxClicue.Text += "Максимальная найденная клика  включает 7 пользователей:";
            	//Сначало све вершины являются кондитатами на попадания в клику:
	        for (int i = n; i >= 1; --i)
		        can.push_back(i);

	        List<ulong> noot;
	        find_clicue(can, noot);

        }
    */
        private void StartWorkProgram(string userId)
        {
            BuildGraph(userName.Text, ulong.Parse(deepFind.Text), userName.Text);
            //GetAns();

            List<int> clique = Find_cl();
            FindedMaxClicue.Text += "Максимальная клика включает " + clique.Count + " человека: ";
            string NAME;
            for (int i = 0; i < clique.Count; ++i)
            {
                string help = NumForId[clique[i]];

                NAME = FIRSTNAME[help] + " ";
                NAME += LASTNAME[help];
                FindedMaxClicue.Text += NAME;
                if (i + 1 < clique.Count)
                    FindedMaxClicue.Text += ", ";
                else
                    FindedMaxClicue.Text += ".";
            }
        }

        private void ButtonStartFindClicue(object sender, RoutedEventArgs e)
        {
            if (userName.Text != "" && deepFind.Text != "")
            {
                StartWorkProgram(userName.Text);
                return;
            }
        }

        private void WebBrowserNavigated(object sender, NavigationEventArgs e)
        {
            var clearUriFragment = e.Uri.Fragment.Replace("#", "").Trim();
            var parameters = HttpUtility.ParseQueryString(clearUriFragment);
            Vk.AccessToken = parameters.Get("access_token");
            Vk.UserId = parameters.Get("user_id");
            if (Vk.AccessToken != null && Vk.UserId != null)
            {
                webBrowser.Visibility = Visibility.Hidden;
            }
        }
    }
}

namespace FindClicue.VkHelpers
{
    public static class Vk
    {
        public static string AccessToken { get; set; }
        public static string UserId { get; set; }
    }    
}