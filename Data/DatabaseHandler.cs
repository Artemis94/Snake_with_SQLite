using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Snake_with_SQLite
{

    /// <summary>
    /// Contains the methods required for the database operations.
    /// </summary>
    class DatabaseHandler
    {
        private SQLiteConnection conn;
        private static DatabaseHandler instance;

        private DatabaseHandler()
        {
            if (System.IO.File.Exists("..\\..\\Database\\snake_data.db"))
            {
                conn = new SQLiteConnection("Data Source = ..\\..\\Database\\snake_data.db; Version = 3; New = False; Compress = True;");
                conn.Open();
            }
            else
            {
                conn = new SQLiteConnection("Data Source = ..\\..\\Database\\snake_data.db; Version = 3; New = True; Compress = True;");
                conn.Open();

                CreateDatabase();
            }
        }


        private void CreateDatabase()
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE `users` (
                                      `id` INTEGER PRIMARY KEY AUTOINCREMENT,
                                      `username` varchar(40) NOT NULL,
                                      `email` varchar(60) NOT NULL,
                                      `full_name` varchar(60) NOT NULL,
                                      `birthdate` date NOT NULL,
                                      `password` varchar(220) NOT NULL,
                                      `rank` INTEGER(11) NOT NULL,
                                      `active` tinyint(1) NOT NULL
                                    );

                                   CREATE TABLE `games` (
                                          `id` INTEGER PRIMARY KEY AUTOINCREMENT,
                                          `user_id` INTEGER(11) NOT NULL,
                                          `time` datetime NOT NULL,
                                          `difficulty` INTEGER(11) NOT NULL,
                                          `points` INTEGER(11) NOT NULL,
                                           FOREIGN KEY(user_id) REFERENCES users(id)
                                        );    

                                    INSERT INTO `users`(`username`, `email`, `full_name`, `birthdate`, `password`, `rank`, `active`) 
                                                VALUES ('admin', 'henry15@gmail.com', 'Henry the Snake', '2018-11-11', '0svvnkMTJ8UorIDCf9QgUk2r7zym9B+Z:VoLHoiZr5AhaNdROuMDEaVZePWw=', 3, True);

                                    ";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Provides access to the DatabaseHandler object.
        /// </summary>
        public static DatabaseHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DatabaseHandler();
                }
                return instance;
            }
        }


        /// <summary>
        /// Inserts a new user to the database, if the given username or e-mail doesn't already exist in the database.
        /// </summary>
        /// <param name="username">String, user input.</param>
        /// <param name="email">String, user input.</param>
        /// <param name="password">String, user input.</param>
        /// <param name="full_name">String, user input.</param>
        /// <param name="birthdate">DateTime, user input.</param>
        /// <returns>True if the Insert is successful, returns False if the username or e-mail are already taken.</returns>
        public bool InsertUser(string username, string email, string password, string full_name, DateTime birthdate)
        {
                bool taken = SelectUsername(username, email);

                if (taken)
                    return false;
                
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO users(username, email, full_name, password, birthdate, rank, active) 
                                    VALUES(@username, @email, @full_name, @password, @birthdate, 1, true)";
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@full_name", full_name);
                cmd.Parameters.AddWithValue("@password", password);
                cmd.Parameters.AddWithValue("@birthdate", birthdate);

                return cmd.ExecuteNonQuery()==1;
        }
        

        private bool SelectUsername(string username, string email)
        {
            bool exists = false;

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT username, email FROM users WHERE username=@username OR email=@email";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.ExecuteNonQuery();

            var reader1 = cmd.ExecuteReader();

            if (reader1.HasRows)
            {
                exists = true;        // The username and/or e-mail is already taken.
            }
            reader1.Close();

            return exists;            
        }


        /// <summary>
        /// Returns the user profile belonging to the username, if the given username and password are correct.
        /// </summary>
        /// <param name="username">String, user input.</param>
        /// <param name="password">String, user input.</param>
        /// <returns>A user object, if the username and password are correct, otherwise null.</returns>
        public User Login(string username, string password) {
            User u = null;

            var command = conn.CreateCommand();
            command.CommandText = "SELECT id, password FROM users WHERE username=@username and active=true";
            command.Parameters.AddWithValue("@username", username);
            command.ExecuteNonQuery();
            var reader1 = command.ExecuteReader();

            if (reader1.HasRows)
            {
                reader1.Read();

                if (PasswordHash.Validate(reader1.GetString(1), password))
                {
                    reader1.Close();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id, username, password, email, full_name, birthdate, rank, active FROM users WHERE username=@username";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                    var reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        u = new User(reader.GetInt32(0), reader.GetString(1),
                            reader.GetString(3), reader.GetString(4), reader.GetDateTime(5), reader.GetInt32(6), reader.GetBoolean(7));
                        reader.Close();
                    }
                }
                else
                    reader1.Close();
            }
            else
                reader1.Close();

            return u;
        }


        /// <summary>
        /// Inserts a game into the database.
        /// </summary>
        /// <param name="o">GameObject, represents the game played, that the player wishes to save.</param>
        /// <returns>Returns true if the save was successful. </returns>
        public bool SaveGame(GameObject o) {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO games(user_id, time, difficulty, points) VALUES(@user_id, @time, @difficulty, @points)";
            cmd.Parameters.AddWithValue("@user_id", LoggedInUser.loggedinuser.Id);
            cmd.Parameters.AddWithValue("@time", o.Time);
            cmd.Parameters.AddWithValue("@difficulty", o.Difficulty);
            cmd.Parameters.AddWithValue("@points", o.Points);
            int i = cmd.ExecuteNonQuery();
            return i == 1;
        }

        /// <summary>
        /// Returns an ordered List of all games from the database that belog to the current user.
        /// </summary>
        /// <returns>A List of GameObject objects or null if there are no games found.</returns>
        public List<GameObject> LoadGames()
        {
            List<GameObject> games = new List<GameObject>();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, time, difficulty, points FROM games WHERE user_id=@user_id ORDER BY difficulty ASC, points DESC;";
                cmd.Parameters.AddWithValue("@user_id", LoggedInUser.loggedinuser.Id);
                cmd.ExecuteNonQuery();
                var reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    games.Add(new GameObject(reader.GetInt32(0), reader.GetDateTime(1), reader.GetInt32(2), reader.GetInt32(3)));
                }

                reader.Close();
        
            if (games.Count != 0)
                    return games;
                return null;
        }

        /// <summary>
        /// Determines wheter or not the password entered belongs to the current user.
        /// </summary>
        /// <param name="pwd">String, user input.</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        public bool CorrectPassword(string pwd)
        {
            bool correct = false;

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT password FROM users WHERE username=@username";
            cmd.Parameters.AddWithValue("@username", LoggedInUser.loggedinuser.Username);
            cmd.ExecuteNonQuery();
            var reader = cmd.ExecuteReader();

            if (reader.HasRows) {
                reader.Read();
                if (PasswordHash.Validate(reader.GetString(0), pwd))
                    correct = true;
            }
            reader.Close();

            return correct;
        }

        /// <summary>
        /// Updates the current user with new information.
        /// </summary>
        /// <param name="username">String, user input</param>
        /// <param name="email">String, user input</param>
        /// <param name="fullname">String, user input</param>
        /// <param name="birthdate">DateTime, user input</param>
        /// <param name="rank">Integer, user input</param>
        /// <returns>
        /// 0 if the input username is already taken.
        /// 2 if the input e-mail is already in use by another account.
        /// 1 if the update was successful.
        /// </returns>
        public int UpdateUser(string username, string email, string fullname, DateTime birthdate, int rank)
        {
                if (!SelectUsername(username, LoggedInUser.loggedinuser.Id)) {
                    if (!SelectEmail(email, LoggedInUser.loggedinuser.Id)) {

                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"UPDATE users SET username=@username, email=@email, full_name=@full_name, birthdate=@birthdate , rank=@rank
                                  WHERE id=@id";
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@full_name", fullname);
                        cmd.Parameters.AddWithValue("@birthdate", birthdate);
                        cmd.Parameters.AddWithValue("@rank", rank);
                        cmd.Parameters.AddWithValue("@id", LoggedInUser.loggedinuser.Id);

                        return cmd.ExecuteNonQuery();
                    } else {
                        return 2;
                    }
                } else {
                    return 0;
                }
        }

        /// <summary>
        /// Updates the password that belogs to the current user.
        /// </summary>
        /// <param name="pwd">String, user input.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        public bool UpdatePassword(string pwd)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE users SET password=@password WHERE id=@id";
            cmd.Parameters.AddWithValue("@password", pwd);
            cmd.Parameters.AddWithValue("@id", LoggedInUser.loggedinuser.Id);

            if (cmd.ExecuteNonQuery() == 1)
                return true;

            return false;            
        }


        /// <summary>
        /// Updates the password that belogs to the e-mail address.
        /// </summary>
        /// <param name="pwd">String, user input.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        public bool UpdatePassword(string pwd, string email)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE users SET password=@password WHERE email=@email";
            cmd.Parameters.AddWithValue("@password", pwd);
            cmd.Parameters.AddWithValue("@email", email);

            if (cmd.ExecuteNonQuery() == 1)
                return true;

            return false;
        }

        /// <summary>
        /// Returns the highest point game of the selected game mode, for all users.
        /// </summary>
        /// <param name="difficulty"></param>
        /// <returns>A string List or null, if there are no games in the selectd category. </returns>
        public List<string> LoadGamesRanked(int difficulty)
        {
                List<string> games = new List<string>();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT u.id, u.username, MAX(g.points), g.time FROM games g LEFT JOIN users u ON g.user_id = u.id WHERE g.difficulty = @difficulty GROUP BY u.id ORDER BY points DESC";
                cmd.Parameters.AddWithValue("@difficulty", difficulty);
                cmd.ExecuteNonQuery();
                var reader = cmd.ExecuteReader();

            while (reader.Read()) 
            {
                string s = (reader.GetInt32(0) + "#" + reader.GetString(1) + "#" + reader.GetString(3) + "#" + reader.GetInt32(2));
                games.Add(s);
            }

                reader.Close();

            if (games.Count != 0)
                return games;
            return null;

        }

        /// <summary>
        /// Selects and returns all users from the database.
        /// </summary>
        /// <returns>A User List.</returns>
        public List<User> SelecAllUsers()
        {
            List<User> users = new List<User>();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, username, email, full_name, birthdate, rank, active FROM users";
                cmd.ExecuteNonQuery();

                var reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    users.Add(new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDateTime(4), reader.GetInt32(5), reader.GetBoolean(6)));
                }

                reader.Close();
            return users;
        }

        /// <summary>
        /// Updates a specific user with new information.
        /// </summary>
        /// <param name="id">Integer.</param>
        /// <param name="username">String, user input</param>
        /// <param name="email">String, user input</param>
        /// <param name="rank">Integer, user input</param>
        /// <param name="active">Bool</param>
        /// <returns>
        /// 0 if the input username is already taken.
        /// 2 if the input e-mail is already in use by another account.
        /// 1 if the update was successful.
        /// </returns>
        public int AdminUpdateUser(int id, string username, string email, int rank, bool active)
        {
                if (!SelectUsername(username, id)) {
                    if (!SelectEmail(email, id)) {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"UPDATE users SET username=@username, email=@email, rank=@rank, active=@active
                                  WHERE id=@id";
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@rank", rank);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@active", active);

                        return cmd.ExecuteNonQuery();
                    }
                    else {
                        return 2;
                    }
                } else
                    return 0;
        }

        /// <summary>
        /// Changes a specific user's password to the given parameter.
        /// </summary>
        /// <param name="id">Integer</param>
        /// <param name="pwd">String</param>
        /// <returns>1 if the change was successful.</returns>
        public int AdminResetUserPassword(int id, string pwd) {

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE users SET password=@pwd WHERE id=@id";
            cmd.Parameters.AddWithValue("@pwd", PasswordHash.Hash(pwd));
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery();           

        }

        /// <summary>
        /// Determines if the username parameter already exists in the database aside from being with the id parameter.
        /// </summary>
        /// <param name="username">String, user input</param>
        /// <param name="id">String, user input</param>
        /// <returns>True if the username is already in use, otherwise false. </returns>
        private bool SelectUsername(string username, int id) {
                bool exists = false;        

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT username FROM users WHERE username=@username AND id!=@id;";
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                var reader1 = cmd.ExecuteReader();

                if (reader1.HasRows) {
                    exists = true;
                }
                reader1.Close();

                return exists;

            
        }

        /// <summary>
        /// Determines if the email parameter already exists in the database aside from being with the id parameter.
        /// </summary>
        /// <param name="email">String, user input</param>
        /// <param name="id">String, user input</param>
        /// <returns>True if the e-mail address is already in use, otherwise false. </returns>
        private bool SelectEmail(string email, int id) {

            bool exists = false;

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT email FROM users WHERE email=@email AND id!=@id;";
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            var reader1 = cmd.ExecuteReader();

            if (reader1.HasRows) {
                exists = true;
            }
            reader1.Close();

            return exists;
        }


        /// <summary>
        /// Determines if the email parameter exists in the database or not.
        /// </summary>
        /// <param name="email">String, user input</param>
        /// <param name="id">String, user input</param>
        /// <returns>True if the e-mail address is present. </returns>
        public bool SelectEmail(string email)
        {
            bool exists = false;

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT email FROM users WHERE email=@email;";
            cmd.Parameters.AddWithValue("@email", email);
            cmd.ExecuteNonQuery();

            var reader1 = cmd.ExecuteReader();

            if (reader1.HasRows)
            {
                exists = true;
            }
            reader1.Close();

            return exists;
        }

        /// <summary>
        /// Determins how many users have Administrator rank.
        /// </summary>
        /// <returns>An integer number.</returns>
        public int SelectCountAdmins() {

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(id) FROM users WHERE rank = 3";
            cmd.ExecuteNonQuery();

            var reader = cmd.ExecuteReader();
            reader.Read();
            int n = reader.GetInt32(0);
            reader.Close();

            return n;
        }

    }
}
