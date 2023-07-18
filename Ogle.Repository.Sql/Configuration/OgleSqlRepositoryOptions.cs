namespace Ogle.Repository.Sql
{
	public class OgleSqlRepositoryOptions
	{
		public string ConnectionString { get; set; }
        public string TableName { get; set; }
		public bool AutoCreateTable { get; set; }
	}
}

