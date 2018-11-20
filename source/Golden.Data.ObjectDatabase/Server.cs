using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Golden.Data.ObjectDatabase
{
	public class Server : DbObjectBase
	{
        #region Constants
        public const string DefaultDatabaseName = "master";
		public const string BatchSeparator = "GO";
        #endregion
        #region Fields
		private Lazy<SqlConnection> _Connection;
		private string userName, password;
		private Lazy<Version> _Version;
		private Lazy<string> _MachineName, _Edition, _Collation;
		private Lazy<ReadOnlyCollection<Database>> _Databases;
        #endregion
        #region Properties
        public ReadOnlyCollection<Database> Databases
		{
			get { return _Databases.Value; }
		}
		public Version Version
		{
			get { return _Version.Value; }
		}
		public string MachineName
		{
			get { return _MachineName.Value; }
		}
		public string Edition
		{
			get { return _Edition.Value; }
		}
		public string Collation
		{
			get { return _Collation.Value; }
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public override DbObjectBase Parent
		{
			get
			{
				return base.Parent;
			}
			internal set
			{
				base.Parent = value;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public override string QuoteName
		{
			get
			{
				return base.QuoteName;
			}
		}
        #endregion
        #region Methods

        internal Server()
		{
		}
		public Server(string instanceName) : this(instanceName, null, null, true)
		{
		}
		public Server(string instanceName, string userName, string password) : this(instanceName, userName, password, false)
		{
		}
		private Server(string instanceName, string userName, string password, bool trustedConnection)
		{
			var csb = new SqlConnectionStringBuilder();
			csb.DataSource = instanceName;
			if (trustedConnection)
			{
				csb.IntegratedSecurity = true;
			}
			else
			{
				csb.UserID = userName;
				csb.Password = password;
				csb.IntegratedSecurity = false;
			}
			csb.ApplicationName = nameof(ObjectDatabase);
			Initialize(csb.ConnectionString);
		}
		internal void Initialize(string connectionString)
		{
			var csb = new SqlConnectionStringBuilder(connectionString);
			csb.InitialCatalog = "";
			var baseConnectionString = csb.ConnectionString;

			_Connection = new Lazy<SqlConnection>(() => new SqlConnection(baseConnectionString));
			_Databases = new Lazy<ReadOnlyCollection<Database>>(LoadDatabases);
			_Version = new Lazy<Version>(LoadVersion);
			_MachineName = new Lazy<string>(LoadMachineName);
			_Edition = new Lazy<string>(LoadEdition);
			_Collation = new Lazy<string>(LoadCollation);

			base.Name = csb.DataSource;
			this.userName = csb.UserID;
			this.password = csb.Password;
		}
		private Version LoadVersion()
		{
			return new Version((string)this.ExecuteScalar("SELECT SERVERPROPERTY('ProductVersion')"));
		}
		private string LoadMachineName()
		{
			var result = Utility.Utilities.Convert<string>(this.ExecuteScalar("SELECT SERVERPROPERTY('MachineName')"), true);
			if (result == null) throw new InvalidOperationException();
			return result;
		}
		private string LoadEdition()
		{
			return ((string)this.ExecuteScalar("SELECT SERVERPROPERTY('Edition')"));
		}
		private string LoadCollation()
		{
			var result = Utility.Utilities.Convert<string>(this.ExecuteScalar("SELECT SERVERPROPERTY('Collation')"), true);
			if (result == null) throw new InvalidOperationException();
			return result;
		}
		private ReadOnlyCollection<Database> LoadDatabases()
		{
			var result = new List<Database>();
			var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetDatabases");
			this.ExecuteReader(cmdStr, reader =>
			{
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						var db = new Database(Convert.ToString(reader["Name"]));
						db.Parent = this;
						result.Add(db);
					}
				}
			});

			return result.AsReadOnly();
		}
		public Database Database(string name)
		{
			return _Databases.Value.First(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
		public int ExecuteNonQuery(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.InternalExecuteNonQuery(null, sqlCommand, parameters);
		}
		public object ExecuteScalar(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.InternalExecuteScalar(null, sqlCommand, parameters);
		}
		public void ExecuteReader(string sqlCommand, Action<SqlDataReader> action, SqlParameter[] parameters = null, CommandBehavior behavior = CommandBehavior.Default)
		{
			this.InternalExecuteReader(null, sqlCommand, action, parameters, behavior);
		}
		public DataSet ExecuteWithResults(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.InternalExecuteWithResults(null, sqlCommand, parameters);
		}
		public DataTable ExecuteWithResult(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.InternalExecuteWithResult(null, sqlCommand, parameters);
		}

		//private string[] GetStatements(string sqlCommand)
		//{
		//	var statements = new List<string>();
		//	var buffer = new StringBuilder();
		//	int index;
		//	int iCh = 0;
		//	char ch;

		//	while (iCh < sqlCommand.Length)
		//	{
		//		ch = sqlCommand[iCh];

		//		#region String
		//		if (ch == '\'')
		//		{
		//			index = sqlCommand.IndexOf('\'', iCh + 1);
		//			if (index >= 0)
		//			{
		//				buffer.Append(sqlCommand.Substring(iCh, index - iCh + 1));
		//				iCh = index + 1;
		//			}
		//			else
		//			{
		//				throw new FormatException();
		//			}
		//			continue;
		//		}
		//		#endregion
		//		#region Multiline Comment
		//		if (sqlCommand.IndexOf("/*", iCh, StringComparison.OrdinalIgnoreCase) == iCh)
		//		{
		//			index = sqlCommand.IndexOf("*/", iCh + 2, StringComparison.OrdinalIgnoreCase);
		//			if (index >= 0)
		//			{
		//				iCh = index + 2;
		//			}
		//			else
		//			{
		//				break;
		//			}
		//			continue;
		//		}
		//		#endregion
		//		#region Singleline Comment
		//		if (sqlCommand.IndexOf("--", iCh, StringComparison.OrdinalIgnoreCase) == iCh)
		//		{
		//			var mindex = sqlCommand.IndexOfAny(new[] { "\r\n", "\n" }, iCh + 2, StringComparison.OrdinalIgnoreCase);
		//			if (mindex.Key >= 0)
		//			{
		//				iCh = mindex.Key; // + (mindex.Value == 0 ? 2 : 1);
		//				continue;
		//			}
		//			else
		//			{
		//				break;
		//			}
		//		}
		//		#endregion
		//		#region BatchSeparator
		//		if (!string.IsNullOrEmpty(Server.BatchSeparator))
		//		{
		//			if (sqlCommand.IndexOf(Server.BatchSeparator, iCh, StringComparison.OrdinalIgnoreCase) == iCh)
		//			{
		//				bool isSep = true;
		//				for (int i = iCh - 1; i >= 0; i--)
		//				{
		//					if (sqlCommand[i] == '\n')
		//					{
		//						isSep = true;
		//						break;
		//					}
		//					if (!char.IsWhiteSpace(sqlCommand, i))
		//					{
		//						isSep = false;
		//						break;
		//					}
		//				}
		//				if (isSep)
		//				{
		//					var mIndex = sqlCommand.IndexOfAny(new[] { "\r\n", "\n" }, iCh + Server.BatchSeparator.Length, StringComparison.OrdinalIgnoreCase);
		//					var temp = sqlCommand.Substring(iCh + Server.BatchSeparator.Length, mIndex.Key - iCh - Server.BatchSeparator.Length);
		//					if (temp.Length == 0 || Regex.IsMatch(temp, @"^ ((\s*) | (\s+\d+\s*)) ;* \s* $", RegexOptions.IgnorePatternWhitespace))
		//					{
		//						statements.Add(buffer.ToString());
		//						buffer.Clear();

		//						iCh = mIndex.Key + (mIndex.Value == 0 ? 2 : 1);
		//						continue;
		//					}
		//				}
		//			}
		//		}
		//		#endregion

		//		buffer.Append(ch);
		//		iCh++;
		//	}
		//	if (buffer.Length > 0) statements.Add(buffer.ToString());

		//	return statements.ToArray();
		//}
		internal int InternalExecuteNonQuery(string databaseName, string sqlCommand, SqlParameter[] parameters = null)
		{
			//if (string.IsNullOrWhiteSpace(databaseName)) databaseName = Server.DefaultDatabaseName;
			try
			{
				if (!databaseName.IsNullOrEmpty())
				{
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, databaseName, userName, password);
				}
				using (var cmd = new SqlCommand(sqlCommand, _Connection.Value))
				{
					if (parameters != null) cmd.Parameters.AddRange(parameters);
					_Connection.Value.Open();
					var result = cmd.ExecuteNonQuery();
					_Connection.Value.Close();
					return result;
				}
			}
			catch
			{
				_Connection.Value.Close();
				throw;
			}
			finally
			{
				if (!databaseName.IsNullOrEmpty())
				{
				    _Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, null, userName, password);
				}
			}
		}
		internal object InternalExecuteScalar(string databaseName, string sqlCommand, SqlParameter[] parameters = null)
		{
			//if (string.IsNullOrWhiteSpace(databaseName)) databaseName = Server.DefaultDatabaseName;
			//string prevDbName = null;
			try
			{
                if (!databaseName.IsNullOrEmpty())
                {
                    _Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, databaseName, userName, password);
                }
                using (var cmd = new SqlCommand(sqlCommand, _Connection.Value))
				{
					if (parameters != null) cmd.Parameters.AddRange(parameters);
					_Connection.Value.Open();
					var result = cmd.ExecuteScalar();
					_Connection.Value.Close();
					return result;
				}
			}
			catch
			{
				_Connection.Value.Close();
				throw;
			}
			finally
			{
				if (!databaseName.IsNullOrEmpty())
				{
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, null, userName, password);
				}
			}
		}
		internal void InternalExecuteReader(string databaseName, string sqlCommand, Action<SqlDataReader> action, SqlParameter[] parameters = null, CommandBehavior behavior = CommandBehavior.Default)
		{
			//if (string.IsNullOrWhiteSpace(databaseName)) databaseName = Server.DefaultDatabaseName;
			//string prevDbName = null;
			SqlDataReader reader = null;
			try
			{
				if (!databaseName.IsNullOrEmpty())
				{
					//prevDbName = _Connection.Value.Database;
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, databaseName, userName, password);
				}
				using (var cmd = new SqlCommand(sqlCommand, _Connection.Value))
				{
					if (parameters != null) cmd.Parameters.AddRange(parameters);
					_Connection.Value.Open();
					using (reader = cmd.ExecuteReader(behavior))
					{
						action.Invoke(reader);
					}
					_Connection.Value.Close();
				}
			}
			catch
			{
				if (reader != null) ((IDisposable)reader).Dispose();
				_Connection.Value.Close();
				throw;
			}
			finally
			{
				if (!databaseName.IsNullOrEmpty())
				{
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, null, userName, password);
				}
			}
		}
		internal DataSet InternalExecuteWithResults(string databaseName, string sqlCommand, SqlParameter[] parameters = null)
		{
			//if (string.IsNullOrWhiteSpace(databaseName)) databaseName = Server.DefaultDatabaseName;
			//string prevDbName = null;
			DataSet result = null;
			try
			{
				if (!databaseName.IsNullOrEmpty())
				{
					//prevDbName = _Connection.Value.Database;
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, databaseName, userName, password);
				}
				using (var adapter = new SqlDataAdapter(sqlCommand, _Connection.Value))
				{
					if (parameters != null) adapter.SelectCommand.Parameters.AddRange(parameters);
					_Connection.Value.Open();
					result = new DataSet();
					adapter.Fill(result);
					_Connection.Value.Close();
					return result;
				}
			}
			catch
			{
				if (result != null) result.Dispose();
				_Connection.Value.Close();
				throw;
			}
			finally
			{
				if (!databaseName.IsNullOrEmpty())
				{
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, null, userName, password);
				}
			}
		}
		internal DataTable InternalExecuteWithResult(string databaseName, string sqlCommand, SqlParameter[] parameters = null)
		{
			//if (string.IsNullOrWhiteSpace(databaseName)) databaseName = Server.DefaultDatabaseName;
			//string prevDbName = null;
			DataTable result = null;
			try
			{
				if (!databaseName.IsNullOrEmpty())
				{
					//prevDbName = _Connection.Value.Database;
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, databaseName, userName, password);
				}
				using (var adapter = new SqlDataAdapter(sqlCommand, _Connection.Value))
				{
					if (parameters != null) adapter.SelectCommand.Parameters.AddRange(parameters);
					_Connection.Value.Open();
					result = new DataTable();
					adapter.Fill(result);
					_Connection.Value.Close();
					return result;
				}
			}
			catch
			{
				if (result != null) result.Dispose();
				_Connection.Value.Close();
				throw;
			}
			finally
			{
				if (!databaseName.IsNullOrEmpty())
				{
					_Connection.Value.ConnectionString = ChangeDatabaseName(_Connection.Value.ConnectionString, null, userName, password);
				}
			}
		}
        private static string ChangeDatabaseName(string connectionString, string newDatabaseName, string userName, string password)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);
            csb.InitialCatalog = (newDatabaseName??"");
            if (!csb.IntegratedSecurity)
            {
                csb.UserID = userName;
                csb.Password = password;
            }
            return csb.ConnectionString;
        }
        public override string ToString()
		{
			return base.Name;
		}

        #endregion
	}
}
