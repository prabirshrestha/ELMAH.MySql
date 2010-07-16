using System.Data;
using MySql.Data.MySqlClient;

namespace Elmah
{
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Diagnostics;

    /// <summary>
    /// MySql provider for ELMAH
    /// </summary>
    public class MySqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        #region ctors

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        /// <param name="config">Configuration Settings.</param>
        public MySqlErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = GetConnectionString(config);

            if (string.IsNullOrEmpty(connectionString))
                throw new ApplicationException("Connection string is missing for the MySql error log.");

            _connectionString = connectionString;

            ApplicationName = config.Contains("applicationName") ? config["applicationName"].ToString() : string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlErrorLog"/> class
        /// to use a specific connection string for conencting to the database.
        /// </summary>
        public MySqlErrorLog(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            _connectionString = connectionString;
        }

        #endregion

        /// <summary>
        /// Get the name of this error log implemntation.
        /// </summary>
        public override string Name
        {
            get { return "MySql Error Log"; }
        }

        /// <summary>
        /// Gets the connection string used by the log to connect to the database.
        /// </summary>
        public virtual string ConnectionString
        {
            get { return _connectionString; }
        }

        #region Overrides of ErrorLog

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        public override string Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            string errorXml = ErrorXml.EncodeString(error);
            Guid id = Guid.NewGuid();

            using (MySqlConnection cn = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("Elmah_LogError", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@pErrorId", id.ToString());
                    cmd.Parameters.AddWithValue("@pApplication", ApplicationName);
                    cmd.Parameters.AddWithValue("@pHost", error.HostName);
                    cmd.Parameters.AddWithValue("@pType", error.Type);
                    cmd.Parameters.AddWithValue("@pSource", error.Source);
                    cmd.Parameters.AddWithValue("@pMessage", error.Message);
                    cmd.Parameters.AddWithValue("@pUser", error.User);
                    cmd.Parameters.AddWithValue("@pStatusCode", error.StatusCode);
                    cmd.Parameters.AddWithValue("@pTimeUtc", error.Time.ToUniversalTime());
                    cmd.Parameters.AddWithValue("@pAllXml", errorXml);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return id.ToString();
                }
            }
        }

        /// <summary>
        /// Retrieves a single application error from log given its 
        ///             identifier, or null if it does not exist.
        /// </summary>
        public override ErrorLogEntry GetError(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            Guid errorGuid;
            try
            {
                errorGuid = new Guid(id);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml = null;

            using (MySqlConnection cn = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("Elmah_GetErrorXml", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@pApplication", ApplicationName);
                    cmd.Parameters.AddWithValue("@pErrorId", id);

                    cn.Open();
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            errorXml = reader["AllXml"].ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(errorXml))
                return null;

            Error error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        /// <summary>
        /// Retrieves a page of application errors from the log in 
        ///             descending order of logged time.
        /// </summary>
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pagesize", pageSize, null);

            using (MySqlConnection cn = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("Elmah_GetErrorsXml", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@pApplication", ApplicationName);
                    cmd.Parameters.AddWithValue("@pPageIndex", pageIndex);
                    cmd.Parameters.AddWithValue("@pPageSize", pageSize);
                    cmd.Parameters.Add("pTotalCount", MySqlDbType.Int32).Direction = ParameterDirection.Output; 

                    cn.Open();

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (errorEntryList != null)
                        {
                            while (reader.Read())
                            {
                                Guid guid = new Guid(reader["ErrorId"].ToString());

                                Error error = new Error();
                                error.ApplicationName = reader["Application"].ToString();
                                error.HostName = reader["Host"].ToString();
                                error.Type = reader["Type"].ToString();
                                error.Source = reader["Source"].ToString();
                                error.Message = reader["Message"].ToString();
                                error.User = reader["User"].ToString();
                                error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                                error.Time = Convert.ToDateTime(reader["TimeUtc"].ToString()).ToLocalTime();

                                errorEntryList.Add(new ErrorLogEntry(this, guid.ToString(), error));
                            }
                        }
                    }

                    return (int)cmd.Parameters["pTotalCount"].Value;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the connection string from the given configuration dictionary.
        /// </summary>
        private string GetConnectionString(IDictionary config)
        {
            Debug.Assert(config != null);

            // first look for a connection string name that can be 
            // subsequently indexed into the <connectionStrings> section of
            // the configuration to get the actual connection string.

            string connectionStringName = config.Contains("connectionStringName")
                                              ? config["connectionStringName"].ToString()
                                              : string.Empty;

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (settings == null)
                    return string.Empty;

                return settings.ConnectionString ?? string.Empty;

            }

            // connection string name not found so see if a connection string was given directly

            string connectionString = config.Contains("connectionString")
                                          ? config["connectionString"].ToString()
                                          : string.Empty;
            if (!string.IsNullOrEmpty(connectionString))
                return connectionString;

            // as a last resort, check for another setting called
            // connectionStringAppKey. This specifies the key in
            // <appSettings> that contains the actual connection string to
            // be used.
            string connectionStringAppKey = config.Contains("connectionStringAppKey")
                                                ? config["connectionStringAppKey"].ToString()
                                                : string.Empty;
            return string.IsNullOrEmpty(connectionStringAppKey)
                       ? string.Empty
                       : ConfigurationManager.AppSettings[connectionStringAppKey];
        }

        #endregion
    }
}