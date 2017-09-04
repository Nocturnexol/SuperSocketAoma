using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

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
            //解决EF动态建库数据库表名变为复数问题  
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();  
        }
    }
}
