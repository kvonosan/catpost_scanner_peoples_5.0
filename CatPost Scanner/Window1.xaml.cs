using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Threading;
using System.Threading;

namespace CatPost_Scanner
{
    class Ids
    {
        public int Id;
        public string Name;
        public string type;
        public int Count;
    }

    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private MainWindow window;
        private List<Ids> ids = new List<Ids>();
        private int tasks_count = 100; // по сколько постов на таск
        private int scanned = 0;
        private int post_count = 0;
        private MySqlConnection conn;

        private void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        public Window1(MainWindow win)
        {
            window = win;
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private int GetGroupId(string url)
        {
            url = url.TrimEnd('/');
            string[] splitted = url.Split('/');
            int len = splitted.Length;
            var GroupName = splitted[len-1];

            WebClient client = new WebClient();
            Stream data = client.OpenRead("https://api.vk.com/method/groups.getById?" + window.Token + "&group_id=" + GroupName + "&v=" + window.version);
            StreamReader reader = new StreamReader(data);
            JObject groups = JObject.Parse(reader.ReadToEnd());
            reader.Close();
            data.Close();

            if (groups.SelectToken("response") != null)
            {
                return int.Parse(groups["response"][0]["id"].ToString());
            }

            return -1;
        }

        private int GetUserId(string url)
        {
            url = url.TrimEnd('/');
            string[] splitted = url.Split('/');
            int len = splitted.Length;
            var UserName = splitted[len - 1];

            WebClient client = new WebClient();
            Stream data = client.OpenRead("https://api.vk.com/method/users.get?" + window.Token + "&user_ids=" + UserName + "&v=" + window.version);
            StreamReader reader = new StreamReader(data);
            JObject Users = JObject.Parse(reader.ReadToEnd());
            reader.Close();
            data.Close();

            if (Users.SelectToken("response") != null)
            {
                return int.Parse(Users["response"][0]["id"].ToString());
            }

            return -1;
        }

        private int GetCount(int id, string type)
        {
            WebClient client = new WebClient();
            int scan_id = id;
            if (type == "group")
            {
                scan_id = -1 * scan_id;
            }
            Stream data = client.OpenRead("https://api.vk.com/method/wall.get?" + window.Token + "&owner_id=" + scan_id + "&count=1&v=" + window.version);
            StreamReader reader = new StreamReader(data);
            JObject contents = JObject.Parse(reader.ReadToEnd());
            reader.Close();
            data.Close();

            if (contents.SelectToken("response") != null)
            {
                return int.Parse(contents["response"]["count"].ToString());
            }

            return -1;
        }

        private async Task<int> GetCountTask(int id, string type)
        {
            return await Task.Run<int>(() =>
            {
                return GetCount(id, type);
            });
        }

        private async Task<int> GetGroupIdTask(string url)
        {
            return await Task.Run<int>(() =>
            {
                return GetGroupId(url);
            });
        }

        private async Task<int> GetUserIdTask(string url)
        {
            return await Task.Run<int>(() =>
            {
                return GetUserId(url);
            });
        }

        private async Task GetIdsTask(bool update = false)
        {
            ids.Clear();
            TextBox1.AppendText("---------------------------------------------\n");
            TextBox1.AppendText("Текущий пользователь " + window.User + "!\n");

            if (!update)
            {
                int lineCount = TextBox.LineCount;
                for (int line = 0; line < lineCount; line++)
                {
                    string str = TextBox.GetLineText(line).Trim().Replace(Environment.NewLine, "");
                    Thread.Sleep(1000);
                    if (str != "")
                    {
                        Ids id = new Ids();
                        int gId = await GetGroupIdTask(str);
                        if (gId != -1)
                        {
                            id.Id = gId;
                            id.type = "group";
                            id.Name = str;
                            if (!idExist(gId))
                            {
                                int post_counts = await GetCountTask(gId, "group");
                                if (post_counts != -1)
                                {
                                    id.Count = post_counts;
                                    ids.Add(id);
                                    TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") имеет " + id.Count + " постов. Группа добавлена в очередь.\n");
                                }
                                else
                                {
                                    TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") - ошибка взятия количества постов. Группа не добавлена.\n");
                                }
                            }
                            else
                            {
                                TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") - уже в очереди сканирования.\n");
                            }
                        }
                        else
                        {
                            int UserId = await GetUserIdTask(str);
                            if (UserId != -1)
                            {
                                id.Id = UserId;
                                id.type = "user";
                                id.Name = str;
                                if (!idExist(UserId))
                                {
                                    int post_counts = await GetCountTask(UserId, "user");
                                    if (post_counts != -1)
                                    {
                                        id.Count = post_counts;
                                        ids.Add(id);
                                        TextBox1.AppendText("Пользователь  " + id.Name + " (id = " + id.Id + ") имеет " + id.Count + " постов. Пользователь добавлен в очередь.\n");
                                    }
                                    else
                                    {
                                        TextBox1.AppendText("Пользователь " + str + " (id = " + UserId + ") - ошибка взятия количества постов.\n");
                                    }
                                }
                                else
                                {
                                    TextBox1.AppendText("Пользователь " + id.Name + " (id = " + id.Id + ") - уже в очереди сканирования.\n");
                                }
                            }
                            else
                            {
                                TextBox1.AppendText("Пользователь или группа " + str + " не существует.\n");
                            }
                        }
                    }
                }
            }
            else
            {
                string sql_groups = "SELECT name FROM vk_groups_and_users";
                MySqlCommand sql_groups_cmd = new MySqlCommand(sql_groups, conn);
                sql_groups_cmd.Prepare();
                var reader = sql_groups_cmd.ExecuteReader();
                string str = "";
                while (reader.Read())
                {
                    str = reader["name"].ToString();
                    Thread.Sleep(1000);
                    if (str != "")
                    {
                        Ids id = new Ids();
                        int gId = await GetGroupIdTask(str);
                        if (gId != -1)
                        {
                            id.Id = gId;
                            id.type = "group";
                            id.Name = str;
                            if (!idExist(gId))
                            {
                                int post_counts = await GetCountTask(gId, "group");
                                if (post_counts != -1)
                                {
                                    id.Count = post_counts;
                                    ids.Add(id);
                                    TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") имеет " + id.Count + " постов. Группа добавлена в очередь.\n");
                                }
                                else
                                {
                                    TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") - ошибка взятия количества постов. Группа не добавлена.\n");
                                }
                            }
                            else
                            {
                                TextBox1.AppendText("Группа " + id.Name + " (id = " + id.Id + ") - уже в очереди сканирования.\n");
                            }
                        }
                        else
                        {
                            int UserId = await GetUserIdTask(str);
                            if (UserId != -1)
                            {
                                id.Id = UserId;
                                id.type = "user";
                                id.Name = str;
                                if (!idExist(UserId))
                                {
                                    int post_counts = await GetCountTask(UserId, "user");
                                    if (post_counts != -1)
                                    {
                                        id.Count = post_counts;
                                        ids.Add(id);
                                        TextBox1.AppendText("Пользователь  " + id.Name + " (id = " + id.Id + ") имеет " + id.Count + " постов. Пользователь добавлен в очередь.\n");
                                    }
                                    else
                                    {
                                        TextBox1.AppendText("Пользователь " + str + " (id = " + UserId + ") - ошибка взятия количества постов.\n");
                                    }
                                }
                                else
                                {
                                    TextBox1.AppendText("Пользователь " + id.Name + " (id = " + id.Id + ") - уже в очереди сканирования.\n");
                                }
                            }
                            else
                            {
                                TextBox1.AppendText("Пользователь или группа " + str + " не существует.\n");
                            }
                        }
                    }
                }
                reader.Close();
            }
            TextBox1.AppendText("Очередь групп и пользователей создана! Всего в сканировании " + ids.Count + ".\n");
        }

        private bool idExist(int Id)
        {
            foreach (Ids id in ids)
            {
                if (id.Id == Id)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task WorkTask(bool Update = false)
        {
            foreach (Ids id in ids)
            {
                scanned = 0;
                post_count = 0;
                if (id.type == "group")
                {
                    TextBox1.AppendText("Сканируем группу http://vk.com/club" + id.Id + ".\n");
                }
                else
                {
                    TextBox1.AppendText("Сканируем пользователя http://vk.com/id" + id.Id + ".\n");
                }
                TextBox1.AppendText("Всего записей " + id.Count + ".\n");

                string sql = "SELECT vk_id FROM vk_groups_and_users WHERE type=@type AND vk_id=@id";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@type", id.type);
                cmd.Parameters.AddWithValue("@id", id.Id);
                MySqlDataReader reader = cmd.ExecuteReader();
                bool post_exist_in_database = false;
                while (reader.Read())
                {
                    post_exist_in_database = true;
                }
                reader.Close();
                if (!post_exist_in_database)
                {
                    string insert = "INSERT INTO vk_groups_and_users (vk_id, type, name) VALUES(@id, @type, @name);";
                    MySqlCommand cmd1 = new MySqlCommand(insert, conn);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id", id.Id);
                    cmd1.Parameters.AddWithValue("@type", id.type);
                    cmd1.Parameters.AddWithValue("@name", id.Name);
                    cmd1.ExecuteNonQuery();
                }

                int taskCount = id.Count / tasks_count + 1;
                if (Update)
                {
                    taskCount = 1;
                }

                string sql_save_count1 = "SELECT scanned FROM vk_groups_and_users WHERE vk_id = @vk_id";
                MySqlCommand sql_save_count_cmd1 = new MySqlCommand(sql_save_count1, conn);
                sql_save_count_cmd1.Prepare();
                sql_save_count_cmd1.Parameters.AddWithValue("@vk_id", id.Id);
                var reader1 = sql_save_count_cmd1.ExecuteReader();
                int scanned_count = 0;
                while (reader1.Read())
                {
                    string str = reader1["scanned"].ToString();
                    if (str == "")
                    {
                        scanned_count = 0;
                    }
                    else
                    {
                        scanned_count = int.Parse(str);
                    }
                }
                reader1.Close();

                if (scanned_count == id.Count)
                {
                    TextBox1.AppendText("Группа уже просканирована...\n");
                    continue;
                }

                if (scanned_count > 0)
                {
                    TextBox1.AppendText("Продолжаем сканирование...\n");
                }
                else
                {
                    TextBox1.AppendText("Сканируем...\n");
                }

                int a = 0;
                for (int i = 0; i < taskCount; i++)
                {
                    int count = 0;
                    if (i == 0 && !Update)
                    {
                        i = scanned_count / tasks_count;
                        scanned = scanned_count;
                    }
                    int global_offset1 = (a + i) * tasks_count;
                    int scanid = id.Id;
                    string type = id.type;
                    int added_posts = 0;
                    if (Update)
                    {
                        if (id.Count > 100)
                        {
                            count = 100;
                        }
                        else
                        {
                            count = id.Count;
                        }
                    }
                    else
                    {
                        if (i + 1 == taskCount)
                        {
                            count = id.Count % tasks_count;
                        }
                        else
                        {
                            count = tasks_count;
                        }
                    }
                    if (id.type == "user")
                    {
                        added_posts = await Task.Run(() => 
                        {
                            return Scan(id.Id, id.type, global_offset1, count);
                        });
                    }
                    else
                    {
                        added_posts = await Task.Run(() =>
                        {
                            return Scan(-1 * id.Id, id.type, global_offset1, count);
                        });
                    }
                    scanned += count;
                    post_count += added_posts;
                    TextBox1.AppendText("Просканировано " + scanned + " добавлено в базу " + post_count + " всего постов " + id.Count + ".\n");
                    if (!Update)
                    {
                        string sql_save_count = "UPDATE vk_groups_and_users SET scanned=@scanned WHERE vk_id = @vk_id";
                        MySqlCommand sql_save_count_cmd = new MySqlCommand(sql_save_count, conn);
                        sql_save_count_cmd.Prepare();
                        sql_save_count_cmd.Parameters.AddWithValue("@scanned", scanned);
                        sql_save_count_cmd.Parameters.AddWithValue("@vk_id", id.Id);
                        sql_save_count_cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        if (scanned_count >= id.Count - 100)
                        {
                            string sql_save_count = "UPDATE vk_groups_and_users SET scanned=@scanned WHERE vk_id = @vk_id";
                            MySqlCommand sql_save_count_cmd = new MySqlCommand(sql_save_count, conn);
                            sql_save_count_cmd.Prepare();
                            sql_save_count_cmd.Parameters.AddWithValue("@scanned", id.Count);
                            sql_save_count_cmd.Parameters.AddWithValue("@vk_id", id.Id);
                            sql_save_count_cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            if (!Update)
            {
                TextBox1.AppendText("<--- Сканирование завершено " + DateTime.Now.ToString() + ".\n");
            }
            else
            {
                TextBox1.AppendText("<--- Обновление завершено " + DateTime.Now.ToString() + ".\n");
            }
            
        }

        private async Task button_work()
        {
            button.IsEnabled = false;
            button1.IsEnabled = false;
            button2.IsEnabled = false;
            tabControl.SelectedIndex = 1;
            conn = new MySqlConnection("server=185.159.129.209;user=root;database=peoples;password=test1234;Character Set=utf8;");
            conn.Open();
            MySqlCommand cmd_ = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn); // Setting tiimeout on mysqlServer
            cmd_.ExecuteNonQuery();
            await GetIdsTask();
            TextBox1.AppendText("---> Сканирование запущено " + DateTime.Now.ToString() + ".\n");
            await WorkTask();
            
            button.IsEnabled = true;
            button1.IsEnabled = true;
            button2.IsEnabled = true;
        }

        private async Task button_update()
        {
            button.IsEnabled = false;
            button1.IsEnabled = false;
            button2.IsEnabled = false;
            tabControl.SelectedIndex = 1;            
            conn = new MySqlConnection("server=185.159.129.209;user=root;database=peoples;password=test1234;Character Set=utf8;");
            conn.Open();
            MySqlCommand cmd_ = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn); // Setting tiimeout on mysqlServer
            cmd_.ExecuteNonQuery();
            await GetIdsTask(true);
            TextBox1.AppendText("---> Обновление запущено " + DateTime.Now.ToString() + ".\n");
            await WorkTask(true);

            button.IsEnabled = true;
            button1.IsEnabled = true;
            button2.IsEnabled = true;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await button_work();
            }
            catch (Exception ex)
            {
                TextBox1.AppendText("Ошибка:" + ex.Message + ".\n");
                TextBox1.AppendText("<--- Сканирование остановлено " + DateTime.Now.ToString() + ".\n");
                button.IsEnabled = true;
                button1.IsEnabled = true;
                button2.IsEnabled = true;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            TextBox1.Clear();
            button1.IsEnabled = false;
        }

        private void TextBox1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox1.ScrollToEnd();
            AllowUIToUpdate();
        }

        private int Scan(int id, string ug_type, int global_offset, int count)
        {
            int ciclov = count / 100;
            int post_added_count = 0;

            for (var i = 0; i <= ciclov; i++)
            {
                int offset = global_offset + i * 100;
                int current_count = 100;
                if (i == ciclov)
                {
                    current_count = count - i * 100;
                }

                WebClient client = new WebClient();
                Stream data = client.OpenRead("https://api.vk.com/method/wall.get?" + window.Token + "&owner_id=" + id + "&offset=" + offset + "&count=" + current_count + "&v=" + window.version);
                StreamReader reader = new StreamReader(data);
                JObject contents = JObject.Parse(reader.ReadToEnd());

                MySqlCommand cmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn); // Setting tiimeout on mysqlServer
                cmd.ExecuteNonQuery();

                string sql_post_id = "SELECT id FROM vk_groups_and_users WHERE vk_id = @vk_id";
                MySqlCommand sql_post_id_cmd = new MySqlCommand(sql_post_id, conn);
                sql_post_id_cmd.Prepare();
                string sql1 = "SELECT id FROM content WHERE vk_id=@vk_id";
                MySqlCommand post_exist = new MySqlCommand(sql1, conn);
                post_exist.Prepare();
                string sql2 = "INSERT INTO content (group_id, date, text, likes, reposts, views, owner_id, vk_id, link)" +
                    " VALUES(@group_id, @date, @text, @likes, @reposts, @views, @owner_id, @vk_id, @link); SELECT LAST_INSERT_ID();";
                MySqlCommand post_insert = new MySqlCommand(sql2, conn);
                post_insert.Prepare();

                sql_post_id_cmd.Parameters.Clear();
                if (ug_type == "group")
                {
                    sql_post_id_cmd.Parameters.AddWithValue("@vk_id", -1*id);
                }
                else
                {
                    sql_post_id_cmd.Parameters.AddWithValue("@vk_id", id);
                }
                
                var reader_sql_post_id = sql_post_id_cmd.ExecuteReader();
                int group_id = 0;
                while (reader_sql_post_id.Read())
                {
                    group_id = int.Parse(reader_sql_post_id["id"].ToString());
                }
                reader_sql_post_id.Close();

                string text = "";
                int marked_as_ads = 0;
                long attach_date = 0;
                string attach_link = "";
                if (contents.SelectToken("response") != null)
                {
                    for (var j = 0; j < current_count; j++)
                    {
                        int vk_id = int.Parse(contents["response"]["items"][j]["id"].ToString());
                        text = contents["response"]["items"][j]["text"].ToString();
                        int likes = int.Parse(contents["response"]["items"][j]["likes"]["count"].ToString());
                        int reposts = int.Parse(contents["response"]["items"][j]["reposts"]["count"].ToString());
                        marked_as_ads = 0;

                        if (contents["response"]["items"][j].SelectToken("marked_as_ads") != null)
                        {
                            marked_as_ads = int.Parse(contents["response"]["items"][j]["marked_as_ads"].ToString());
                        }
                        int owner_id = int.Parse(contents["response"]["items"][j]["owner_id"].ToString());
                        int views = 0;
                        if (contents["response"]["items"][j].SelectToken("views") != null)
                        {
                            views = int.Parse(contents["response"]["items"][j]["views"]["count"].ToString());
                        }
                        if (contents["response"]["items"][j].SelectToken("attachments") != null)
                        {
                            int attachments_count = 0;
                            attachments_count = contents["response"]["items"][j]["attachments"].Count();
                            for (int a = 0; a < attachments_count; a++)
                            {
                                string type = contents["response"]["items"][j]["attachments"][a]["type"].ToString();
                                if (type == "photo")
                                {
                                    //attach_vk_id = int.Parse(contents["response"]["items"][j]["attachments"][a]["photo"]["id"].ToString());
                                    //attach_owner_id = int.Parse(contents["response"]["items"][j]["attachments"][a]["photo"]["owner_id"].ToString());
                                    if (contents["response"]["items"][j]["attachments"][a]["photo"].SelectToken("photo_1280") != null)
                                    {
                                        attach_link = contents["response"]["items"][j]["attachments"][a]["photo"]["photo_1280"].ToString();
                                    }
                                    else if (contents["response"]["items"][j]["attachments"][a]["photo"].SelectToken("photo_807") != null)
                                    {
                                        attach_link = contents["response"]["items"][j]["attachments"][a]["photo"]["photo_807"].ToString();
                                    }
                                    else if (contents["response"]["items"][j]["attachments"][a]["photo"].SelectToken("photo_604") != null)
                                    {
                                        attach_link = contents["response"]["items"][j]["attachments"][a]["photo"]["photo_604"].ToString();
                                    }
                                    else if (contents["response"]["items"][j]["attachments"][a]["photo"].SelectToken("photo_130") != null)
                                    {
                                        attach_link = contents["response"]["items"][j]["attachments"][a]["photo"]["photo_130"].ToString();
                                    }
                                    else if (contents["response"]["items"][j]["attachments"][a]["photo"].SelectToken("photo_75") != null)
                                    {
                                        attach_link = contents["response"]["items"][j]["attachments"][a]["photo"]["photo_75"].ToString();
                                    }
                                    attach_date = long.Parse(contents["response"]["items"][j]["attachments"][a]["photo"]["date"].ToString());
                                }
                                else if (type == "doc")
                                {
                                    attach_link = contents["response"]["items"][j]["attachments"][a]["doc"]["url"].ToString();
                                    attach_date = long.Parse(contents["response"]["items"][j]["attachments"][a]["doc"]["date"].ToString());
                                }
                                else
                                {
                                    marked_as_ads = 1;
                                }
                            }
                            if (marked_as_ads != 1)
                            {
                                try
                                {
                                    if (conn.State.ToString() != "Open")
                                    {
                                        conn.Open();
                                        MySqlCommand cmd5 = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn); // Setting tiimeout on mysqlServer
                                        cmd5.ExecuteNonQuery();
                                    }

                                    post_insert.Parameters.Clear();
                                    post_insert.Parameters.AddWithValue("@group_id", group_id);
                                    post_insert.Parameters.AddWithValue("@date", attach_date);
                                    post_insert.Parameters.AddWithValue("@text", text);
                                    post_insert.Parameters.AddWithValue("@likes", likes);
                                    post_insert.Parameters.AddWithValue("@reposts", reposts);
                                    post_insert.Parameters.AddWithValue("@views", views);
                                    post_insert.Parameters.AddWithValue("@owner_id", owner_id);
                                    post_insert.Parameters.AddWithValue("@vk_id", vk_id);
                                    post_insert.Parameters.AddWithValue("@link", attach_link);
                                    var reader_post_insert = post_insert.ExecuteReader();

                                    int post_id = 0;
                                    while (reader_post_insert.Read())
                                    {
                                        post_id = int.Parse(reader_post_insert[0].ToString());
                                    }
                                    reader_post_insert.Close();
                                    post_added_count++;
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                }
            }
            return post_added_count;
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await button_update();
            }
            catch (Exception ex)
            {
                TextBox1.AppendText("Ошибка:" + ex.Message + ".\n");
                TextBox1.AppendText("<--- Сканирование остановлено " + DateTime.Now.ToString() + ".\n");
                button.IsEnabled = true;
                button1.IsEnabled = true;
                button2.IsEnabled = true;
            }
        }
    }
}
