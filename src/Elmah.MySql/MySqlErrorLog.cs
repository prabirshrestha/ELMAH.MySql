
namespace Elmah.MySql
{
    using System;
    using System.Collections;
    using System.Configuration;
    using System.Diagnostics;
    using Elmah;

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
                throw new Elmah.ApplicationException("Connection string is missing for the MySql error log.");

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves a single application error from log given its 
        ///             identifier, or null if it does not exist.
        /// </summary>
        public override ErrorLogEntry GetError(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves a page of application errors from the log in 
        ///             descending order of logged time.
        /// </summary>
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            throw new NotImplementedException();
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
                                              ? config["connectionstringName"].ToString()
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