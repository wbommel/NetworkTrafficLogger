using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Threading;
using static System.String;

/*	stub for all new functions. this will ensure that CommandBehaviour will work and that a connection is valid
            //prepare return value

            //Check if open
            if (_connectionCheckOpen())
            {
            }
            _connectionAutomaticClose();

            //return return value ;)
 */

namespace Vobsoft.Csharp.Database
{
    /// <summary>
    /// 
    /// </summary>
    /// <history>19.08.2014 VB: did a lot of redesign and optimization regarding usings and made use of ConnectionBehaviour through the class</history>
    public class SqLiteHandler : IDisposable
    {
        #region declarations

        private string _strConnectionString;

        #region event handling
        public event DatabaseFileNotExistingEventHandler DatabaseFileNotExisting;
        protected void ThrowDatabaseFileNotExistingEvent(
            DatabaseFileNotExistingEventArgs ea)
        {
            DatabaseFileNotExisting?.Invoke(this, ea);
        }
        #endregion
        #endregion



        #region constructor / dispose
        public SqLiteHandler(
            string databaseFileName)
        {
            _initConnection(databaseFileName);
        }
        public SqLiteHandler(
            string databaseFileName,
            ConnectionBehaviour connectionMode)
        {
            ConnectionMode = connectionMode;
            _initConnection(databaseFileName);
        }
        public SqLiteHandler(
            string databaseFileName,
            ConnectionBehaviour connectionMode,
            string createSqlString)
        {
            CreateDatabase(databaseFileName, createSqlString);
            ConnectionMode = connectionMode;
            _initConnection(databaseFileName);
        }

        #region dispose pattern (http://www.codeproject.com/Articles/15360/Implementing-IDisposable-and-the-Dispose-Pattern-P)
        // some fields that require cleanup
        private bool _disposed; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // clean up managed handles
                if (Connection != null)
                {
                    switch (Connection.State)
                    {
                        case ConnectionState.Open:
                            //Connection.Close();
                            Dispatcher.CurrentDispatcher.Invoke(delegate () { Connection.Close(); });
                            break;

                        case ConnectionState.Broken:
                        case ConnectionState.Connecting:
                            break;

                        case ConnectionState.Closed:
                        case ConnectionState.Executing:
                            break;
                        case ConnectionState.Fetching:
                            break;
                    }

                    Connection = null;
                }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
        #endregion



        #region private functions
        #region         connection
        private void _initConnection(
            string databaseFilename)
        {
            if (IsNullOrEmpty(databaseFilename)) throw new ArgumentNullException(nameof(databaseFilename));

            try
            {
                if (File.Exists(databaseFilename))
                {
                    _strConnectionString = Concat("Data Source=", databaseFilename, ";");
                    Connection = new SQLiteConnection(_strConnectionString);
                }
                else
                {
                    //THis doesn't make sense at this point, since there are now event subscriptions at construction time.
                    //ThrowDatabaseFileNotExistingEvent(new DatabaseFileNotExistingEventArgs() { DatabaseFilename = databaseFilename, });

                    //so just throw the FileNotFoundException
                    throw new FileNotFoundException(Concat("SQLiteHandler._initConnection: File not found while trying to access database (", databaseFilename, ")"));
                }
            }
            catch (BadImageFormatException ex)
            {
                //Most likely one of the projects is build in a different platform (32bit/64bit), or the "32-bit bevorzugen" checkbox is set and every other project is in 64bit.
                throw ex;
            }
            catch (DllNotFoundException ex)
            {
                if (ex.Message.Contains("SQLite.Interop.dll"))
                {
                    //Interop dll is missing in target folder i.e. bin\debug
                }
                throw ex;
            }

            if (ConnectionMode == ConnectionBehaviour.AllwaysOpen)
            {
                Connection.Open();
            }

            //set default
            ValidateValueTypes = true;
        }

        private void _openConnection()
        {
            if (ConnectionMode == ConnectionBehaviour.Manually) { Connection.Open(); }
        }

        private void _closeConnection()
        {
            if (ConnectionMode == ConnectionBehaviour.Manually) { Connection.Close(); }
        }

        private bool _connectionCheckOpen()
        {
            //Automatic open?
            if (Connection.State == ConnectionState.Closed &&
                ConnectionMode == ConnectionBehaviour.AutomaticOpenAndClose)
            {
                Connection.Open();
            }

            //Check if open
            return Connection.State == ConnectionState.Open;
        }

        private void _connectionAutomaticClose()
        {
            //Automatic close?
            if (Connection.State != ConnectionState.Closed &&
                ConnectionMode == ConnectionBehaviour.AutomaticOpenAndClose)
            {
                Connection.Close();
            }
        }
        #endregion

        #region         sql execution
        private long _executeSql(
            string sqlString)
        {
            if (IsNullOrEmpty(sqlString)) throw new ArgumentNullException(nameof(sqlString));

            //declare return value
            var result = -1;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var sc = new SQLiteCommand(sqlString, Connection))
                {
                    try
                    {
                        result = sc.ExecuteNonQuery();
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                }
            }
            _connectionAutomaticClose();

            return result;
        }
        #endregion

        #region         read data
        private SQLiteDataReader _getDataReader(
            string sqlString)
        {
            if (IsNullOrEmpty(sqlString)) throw new ArgumentNullException(nameof(sqlString));

            //prepare return value
            SQLiteDataReader dr = null;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var cmd = new SQLiteCommand(sqlString, Connection))
                {
                    dr = cmd.ExecuteReader(CommandBehavior.Default);
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return dr;

        }

        private DataTable _getDataTable(
            string sqlString)
        {
            if (IsNullOrEmpty(sqlString)) throw new ArgumentNullException(nameof(sqlString));

            //prepare return value
            DataTable dt = null;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var da = new SQLiteDataAdapter(sqlString, Connection))
                {
                    dt = new DataTable();
                    da.Fill(dt);
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return dt;
        }

        private DataTable _getDataTable(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            if (IsNullOrEmpty(preparedStatement)) throw new ArgumentNullException(nameof(preparedStatement));
            if (sqlParameters == null) throw new ArgumentNullException(nameof(sqlParameters));

            //prepare return value
            DataTable dt = null;

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = preparedStatement;
                cmd.Connection = Connection;

                foreach (SqlParameter sqlParam in sqlParameters)
                {
                    cmd.Parameters.Add(sqlParam.Name, sqlParam.Type);
                    cmd.Parameters[sqlParam.Name].Value = sqlParam.Value;
                }

                //Check if open 
                if (_connectionCheckOpen())
                {
                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }

            _connectionAutomaticClose();

            //return return value ;)
            return dt;
        }

        private bool _hasRows(
            string sqlString)
        {
            if (IsNullOrEmpty(sqlString)) throw new ArgumentNullException(nameof(sqlString));

            //prepare return value
            var fHasRows = false;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var cmd = new SQLiteCommand(sqlString, Connection))
                {
                    using (var dr = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        fHasRows = dr.HasRows;
                    }
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return fHasRows;
        }

        private bool _hasRows(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            if (IsNullOrEmpty(preparedStatement)) throw new ArgumentNullException(nameof(preparedStatement));
            if (sqlParameters == null) throw new ArgumentNullException(nameof(sqlParameters));

            //prepare return value
            var fHasRows = false;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = preparedStatement;
                    cmd.Connection = Connection;

                    foreach (SqlParameter sqlParam in sqlParameters)
                    {
                        cmd.Parameters.Add(sqlParam.Name, sqlParam.Type);
                        cmd.Parameters[sqlParam.Name].Value = sqlParam.Value;
                    }

                    using (var dr = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        fHasRows = dr.HasRows;
                    }
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return fHasRows;
        }
        #endregion

        #region write data
        private int _writeDataToTable(
            string tableName,
            Dictionary<string, object> values)
        {
            if (IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (values == null) throw new ArgumentNullException(nameof(values));

            //prepare return value
            var intRowsAffected = 0;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var builder = new SQLiteCommandBuilder(new SQLiteDataAdapter("SELECT * FROM " + tableName, Connection)))
                using (var scInsert = builder.GetInsertCommand())
                {
                    foreach (var kvp in values)
                    {
                        foreach (SQLiteParameter prm in scInsert.Parameters)
                        {
                            if (prm.SourceColumn != kvp.Key) continue;

                            if (ValidateValueTypes)
                            {
                                if (_dataIsValid(prm, kvp.Value))
                                {
                                    prm.Value = kvp.Value;
                                }
                            }
                            else
                            {
                                prm.Value = kvp.Value;
                            }

                            break;
                        }
                    }

                    try
                    {
                        intRowsAffected = scInsert.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        if (e is SQLiteException)
                        {

                        }
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return intRowsAffected;
        }

        private int _executePreparedStatement(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            if (IsNullOrEmpty(preparedStatement)) throw new ArgumentNullException(nameof(preparedStatement));
            if (sqlParameters == null) throw new ArgumentNullException(nameof(sqlParameters));

            //prepare return value
            var intRowsAffected = 0;

            //Check if open
            if (_connectionCheckOpen())
            {
                using (var cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = preparedStatement;
                    cmd.Connection = Connection;

                    foreach (SqlParameter sqlParam in sqlParameters)
                    {
                        cmd.Parameters.Add(sqlParam.Name, sqlParam.Type);
                        cmd.Parameters[sqlParam.Name].Value = sqlParam.Value;
                    }

                    try
                    {
                        intRowsAffected = cmd.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        //Database is locked sometimes when connection always open ?????
                        throw ex;
                    }
                }
            }
            _connectionAutomaticClose();

            //return return value ;)
            return intRowsAffected;
        }

        private void _test()
        {
            //_sqlConn.LastInsertRowId
        }
        #endregion

        #region         data handling
        private bool _dataIsValid(
            IDataParameter prm,
            object value)
        {
            if (prm == null) throw new ArgumentNullException(nameof(prm));
            if (value == null) throw new ArgumentNullException(nameof(value));

            //validate integer types
            if (prm.DbType == DbType.Int16 && value is short) return true;
            if (prm.DbType == DbType.Int32 && value is int) return true;
            if (prm.DbType == DbType.Int64 && value is long) return true;
            if (prm.DbType == DbType.UInt16 && value is ushort) return true;
            if (prm.DbType == DbType.UInt32 && value is uint) return true;
            if (prm.DbType == DbType.UInt64 && value is ulong) return true;
            if (prm.DbType == DbType.AnsiString && value is string) return true;
            if (prm.DbType == DbType.AnsiStringFixedLength && value is string) return true;
            if (prm.DbType == DbType.Binary) return true;
            if (prm.DbType == DbType.Boolean && value is bool) return true;
            if (prm.DbType == DbType.Byte && value is byte) return true;
            if (prm.DbType == DbType.Date && value is DateTime) return true;
            if (prm.DbType == DbType.DateTime && value is DateTime) return true;
            if (prm.DbType == DbType.DateTime2 && value is DateTime) return true;
            if (prm.DbType == DbType.DateTimeOffset && value is DateTimeOffset) return true;
            if (prm.DbType == DbType.Double && value is double) return true;
            if (prm.DbType == DbType.Object && value != null) return true;
            if (prm.DbType == DbType.SByte && value is sbyte) return true;
            if (prm.DbType == DbType.Single && value is float) return true;
            if (prm.DbType == DbType.String && value is string) return true;
            if (prm.DbType == DbType.StringFixedLength && value is string) return true;
            if (prm.DbType == DbType.Time && value is DateTime) return true;

            /* ??? unsure if these types really fit together ??? */
            if (prm.DbType == DbType.Currency && value is decimal) return true;
            if (prm.DbType == DbType.Decimal && value is decimal) return true;
            if (prm.DbType == DbType.Guid && value is Guid) return true; // ??? Does that work ???
            if (prm.DbType == DbType.VarNumeric && (
                value is short ||
                value is int ||
                value is long ||
                value is ushort ||
                value is uint ||
                value is ulong)) return true;
            if (prm.DbType == DbType.Xml && value is string) return true;
            /* ??? unsure if these types really fit together ??? */

            return false;
        }

        private DataTable _convertReaderToDataTable(
            SQLiteDataReader dr)
        {
            var dt = new DataTable();

            //TODO: Need this still?
            //dr.Read(

            return dt;
        }
        #endregion
        #endregion



        #region properties
        #region         settings
        public bool ValidateValueTypes { get; set; }
        #endregion

        #region         connection
        /// <summary>represents the connection to the database</summary>
        /// <value>SQLiteConnection object</value>
        public SQLiteConnection Connection { get; private set; }

        public ConnectionBehaviour ConnectionMode { get; } = ConnectionBehaviour.AllwaysOpen;

        public ConnectionState ConnectionState => Connection.State;

        public long LastInsertedRowId => Connection.LastInsertRowId;

        #endregion



        #endregion



        #region methods
        #region         connection
        public void Connection_Open()
        {
            _openConnection();
        }

        public void Connection_Close()
        {
            _closeConnection();
        }
        #endregion

        #region         sql execution
        public bool ExecuteSql(
            string sqlString)
        {
            return _executeSql(sqlString) > -1;
        }

        public long ExecuteSqlWithRowsAffected(string sqlString)
        {
            return _executeSql(sqlString);
        }
        #endregion

        #region         read data
        public SQLiteDataReader GetDataReader(
            string sqlString)
        {
            return _getDataReader(sqlString);
        }

        public DataTable GetDataTable(
            string sqlString)
        {
            return _getDataTable(sqlString);
        }

        public DataTable GetDataTable(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            return _getDataTable(preparedStatement, sqlParameters);
        }

        public bool HasRows(
            string sqlString)
        {
            return _hasRows(sqlString);
        }
        public bool HasRows(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            return _hasRows(preparedStatement, sqlParameters);
        }
        #endregion

        #region write data
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <returns>count of affected rows</returns>
        /// <remarks>
        /// Example: 
        ///     Dictionary<string, object> dic = new Dictionary<string, object>();
        ///     dic.Add("BasePathId", (object)0);
        ///     dic.Add("FullFileName", (object) PicObj.FullFileName);
        ///     dic.Add("FileName", (object) PicObj.FileName);
        ///     dic.Add("Path", (object) PicObj.Path);
        ///     dic.Add("Hash", (object) PicObj.Hash);
        ///     dic.Add("Comment", (object) PicObj.Comment);
        ///     SqLiteHandler.WriteDataToTable("tblPictureObjects", dic);
        ///</remarks>
        public int WriteDataToTable(
            string tableName,
            Dictionary<string, object> values)
        {
            return _writeDataToTable(tableName, values);
        }

        /// <summary>
        /// executes a prepared SQL statement and returns the count of affected rows
        /// </summary>
        /// <param name="preparedStatement">SQL prepared statement to execute</param>
        /// <param name="sqlParameters">ArrayList filled with params of type SqlParameter</param>
        /// <returns>count of affected rows</returns>
        public int ExecutePreparedStatement(
            string preparedStatement,
            IEnumerable sqlParameters)
        {
            return _executePreparedStatement(preparedStatement, sqlParameters);
        }
        #endregion
        #endregion



        #region static methods
        /// <summary>
        /// creates a database with the given structure
        /// </summary>
        /// <param name="databaseFileName">name of the new database file (full path)</param>
        /// <param name="sqlStatement">string containing CREATE statements to build the structure of the new database</param>
        public static int CreateDatabase(
            string databaseFileName,
            string sqlStatement)
        {
            if (IsNullOrEmpty(databaseFileName)) throw new ArgumentNullException(nameof(databaseFileName));
            if (IsNullOrEmpty(sqlStatement)) throw new ArgumentNullException(nameof(sqlStatement));

            int intResult = -1;

            //Create SQL Connection with chosen filename            
            var sqlConn = new SQLiteConnection(Concat("Data Source=", databaseFileName, ";"));
            sqlConn.Open();

            //execute given CREATE statements
            if (!IsNullOrEmpty(sqlStatement))
            {
                using (var cmdCommand = new SQLiteCommand(sqlStatement, sqlConn))
                {
                    try
                    {
                        intResult = cmdCommand.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

            //close connection -> done (TODO: maybe return the intResult of the SQLiteCommand.ExecuteNonQuery)
            sqlConn.Close();

            return intResult;
        }
        #endregion
    }



    #region simple classes
    public class SqlParameter
    {
        public string Name { get; set; }
        public DbType Type { get; set; }
        public object Value { get; set; }
    }
    #endregion



    #region event handlers
    public delegate void DatabaseFileNotExistingEventHandler(object sender, DatabaseFileNotExistingEventArgs e);
    public class DatabaseFileNotExistingEventArgs : EventArgs
    {
        public string DatabaseFilename { get; set; }
    }
    #endregion
}
