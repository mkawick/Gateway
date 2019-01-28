using System;
using System.Collections.Generic;
using System.Data;
using PacketTypes;
using Npgsql;

// NuGet:  Install-Package Npgsql -Version 3.1.10
namespace LoginServer
{
    public class DBAccountQuerier
    {
        public List<UserAccountRequest> pendingUserAccountRequests;
        public List<UserAccountRequest> resolvedUserAccountRequests;

        private Dictionary<int, string> products;

        // we need to add configuration params. Perhaps command line.
        public string serverName = "localhost";
        public long portId = 5432;
        public string loginUser = "postgres";
        public string password = "postgres";
        public string database = "postgres";

        bool isDbConnectionValid = false;
        bool IsDbConnectionValid { get { return isDbConnectionValid; } }
        private NpgsqlConnection dbConnection;
        private NpgsqlCommand userLookupCommand;
        private NpgsqlCommand userGetCharacterProfile;
        private NpgsqlCommand userInsertCharacterProfile;
        private NpgsqlCommand userUpdateCharacterProfile;

        public DBAccountQuerier()
        {
            products = new Dictionary<int, string>();
            GrabAllProducts(); 
        }

        void Initialize()
        {
            try
            {
                string connstring = String.Format("Server={0};Port={1};" +
                        "User Id={2};Password={3};Database={4};",
                        serverName, portId, loginUser,
                        password, database);
                dbConnection = new NpgsqlConnection(connstring);
                dbConnection.Open();

                PrepareStatements();
            }
            catch (Exception msg)
            {
                Console.Write("Exception {0}\n", msg);
                isDbConnectionValid = false;
                throw;
            }
            isDbConnectionValid = true;
        }

        void PrepareStatements()
        {
            userLookupCommand = new NpgsqlCommand(
                "SELECT a2p.user_id, c.character_id, c.name, c.state FROM "
                + "account_2_product a2p JOIN account a ON a.user_id=a2p.user_id "
                + "LEFT OUTER JOIN character_profile c ON a2p.id = c.account_2_product_id "
                + "WHERE a.username=@username AND a.password=@password AND a2p.product_id=@product_id",
                dbConnection);
            userLookupCommand.Parameters.Add("username", NpgsqlTypes.NpgsqlDbType.Varchar);
            userLookupCommand.Parameters.Add("password", NpgsqlTypes.NpgsqlDbType.Varchar);
            userLookupCommand.Parameters.Add("product_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userLookupCommand.Prepare();

            userGetCharacterProfile = new NpgsqlCommand(
                "SELECT character_id, \"name\", \"state\" FROM character_profile "
                + "WHERE \"account_2_product_id\" = (SELECT id FROM account_2_product WHERE user_id=@user_id AND product_id=@product_id)",
                dbConnection);
            userGetCharacterProfile.Parameters.Add("user_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userGetCharacterProfile.Parameters.Add("product_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userGetCharacterProfile.Prepare();

            userInsertCharacterProfile = new NpgsqlCommand(
                "INSERT INTO character_profile (\"name\", \"state\", \"account_2_product_id\") "
                + "VALUES (@name, @state, (SELECT id FROM account_2_product WHERE user_id=@user_id AND product_id=@product_id))",
                dbConnection);
            userInsertCharacterProfile.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Varchar);
            userInsertCharacterProfile.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Json);
            userInsertCharacterProfile.Parameters.Add("user_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userInsertCharacterProfile.Parameters.Add("product_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userInsertCharacterProfile.Prepare();

            userUpdateCharacterProfile = new NpgsqlCommand(
                "UPDATE character_profile SET \"state\" = @state WHERE character_id=@character_id",
                dbConnection);
            userUpdateCharacterProfile.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Json);
            userUpdateCharacterProfile.Parameters.Add("character_id", NpgsqlTypes.NpgsqlDbType.Integer);
            userUpdateCharacterProfile.Prepare();

        }

        void Shutdown()
        {
            if (dbConnection != null && isDbConnectionValid == true)
            {
                dbConnection.Close();
            }
        }

        void GrabAllProducts()
        {
            try
            {
                if (isDbConnectionValid == false)
                    Initialize();

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(
                    "SELECT product_id, productname FROM product", 
                    dbConnection);
                DataSet ds = new DataSet();
                ds.Reset();
                da.Fill(ds);

                DataTable dt = ds.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    int productId = row.Field<int>("product_id");
                    string productName = row.Field<string>("productname");
                    products.Add(productId, productName);
                }
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                Console.Write("Exception {0}\n", msg);
                throw;
            }
        }

        string SaltAndHash(string raw)
        {
            // todo, make this work
            return raw;
        }

        int GetProductIdFromName(string productname)
        {
            foreach (var v in products)
            {
                if (v.Value == productname)
                {
                    return v.Key;
                }
            }
            return -1;
        }

        public PlayerSaveState GetPlayerSaveState(string _username, string _password, string _productname)
        {
            string username = StringUtils.Sanitize(_username);
            string password = SaltAndHash(StringUtils.Sanitize(_password));
            int productId = GetProductIdFromName(StringUtils.Sanitize(_productname, false));

            // Quick exit if values are invalid
            if (username.Length == 0 || password.Length == 0 || productId == -1)
            {
                return null;
            }
            
            try
            {
                if (isDbConnectionValid == false)
                    Initialize();

                userLookupCommand.Parameters["username"].Value = username;
                userLookupCommand.Parameters["password"].Value = password;
                userLookupCommand.Parameters["product_id"].Value = productId;

                NpgsqlDataAdapter da = new NpgsqlDataAdapter(userLookupCommand);
                DataSet ds = new DataSet();
                ds.Reset();
                da.Fill(ds);

                DataTable dt = ds.Tables[0];
                // We'll only grab the first character for now
                if (dt.Rows.Count >= 1)
                {
                    var row = dt.Rows[0];

                    PlayerSaveState result = new PlayerSaveState();
                    // user_id in the db is the accountId
                    result.accountId = row.Field<int>("user_id");
                    if (row.IsNull("character_id"))
                    {
                        result.characterId = PlayerSaveState.NO_CHARACTER_ID;
                    }
                    else
                    {
                        result.characterId = row.Field<int>("character_id");
                    }
                    result.name = row.Field<string>("name");
                    result.state.state = row.Field<string>("state");

                    return result;
                }
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                Console.Write("Exception {0}\n", msg);
                return null;
            }
            return null;
        }

        public int CreateCharacterProfile(int accountId, string productName, string characterName, PlayerSaveStateData state)
        {
            int productId = GetProductIdFromName(StringUtils.Sanitize(productName, false));

            // Quick exit if values are invalid
            if (accountId == -1 || characterName.Length == 0 || state == null || productId == -1)
            {
                return PlayerSaveState.NO_CHARACTER_ID;
            }

            try
            {
                if (isDbConnectionValid == false)
                    Initialize();

                // user_id in the db is the accountId
                userInsertCharacterProfile.Parameters["user_id"].Value = accountId;
                userInsertCharacterProfile.Parameters["product_id"].Value = productId;
                userInsertCharacterProfile.Parameters["name"].Value = characterName;
                userInsertCharacterProfile.Parameters["state"].Value = state.state;

                if (userInsertCharacterProfile.ExecuteNonQuery() != 1)
                {
                    // Something went wrong
                    return PlayerSaveState.NO_CHARACTER_ID;
                }

                // Grab the character_id
                // TODO: Wrap this up in a stored proc - this current approach only works as we only have one
                // login server, and we block on each query made
                userGetCharacterProfile.Parameters["user_id"].Value = accountId;  // user_id in the db is the accoountId
                userGetCharacterProfile.Parameters["product_id"].Value = productId;
                return (int)userGetCharacterProfile.ExecuteScalar();
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                Console.Write("Exception {0}\n", msg);
                return PlayerSaveState.NO_CHARACTER_ID;
            }
        }

        public bool UpdateCharacterProfile(int characterId, PlayerSaveStateData state)
        {
            // Quick exit if values are invalid
            if (characterId == PlayerSaveState.NO_CHARACTER_ID || state == null)
            {
                return false;
            }

            try
            {
                if (isDbConnectionValid == false)
                    Initialize();

                userUpdateCharacterProfile.Parameters["character_id"].Value = characterId;
                userUpdateCharacterProfile.Parameters["state"].Value = state.state;

                return userUpdateCharacterProfile.ExecuteNonQuery() == 1;
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                Console.Write("Exception {0}\n", msg);
                return false;
            }
        }
    }
}
