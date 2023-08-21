namespace Ogle.Repository.Sql.Abstractions
{
	public class OgleSqlRepositoryOptions
	{
		public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string DetailedTableName { get; set; }
        public bool AutoCreateTable { get; set; }
	}
}

