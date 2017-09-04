using System.Data.Entity;

namespace SuperSocketAoma.Db
{
    /// <summary>
    /// OracleDbContext
    /// </summary>
    public class OracleDbContext : DbContext
    {
        /// <summary>
        /// OracleDbContext
        /// </summary>
        public OracleDbContext()
            : base("OracleDbContext")
        {
            //初始化
            Database.SetInitializer(new CreateDatabaseIfNotExists<OracleDbContext>());
        }

        /// <summary>
        /// Category
        /// </summary>
        public DbSet<AnalysisAlertData> AnalysisAlert { get; set; }

        /// <summary>
        /// OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("C##BUS_DATASYS");
        }
    }
}
