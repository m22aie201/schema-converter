using Microsoft.Data.SqlClient;
using Schema_Converters.Models;
using System.Data;

namespace Schema_Converters.Services
{
    public class RelationshipTransformation
    {
        private readonly UtilitiesService _utilitiesService;

        public RelationshipTransformation(UtilitiesService utilitiesService)
        {
            _utilitiesService = utilitiesService;
        }

        /// <summary>
        /// This function saves SQL data to MongoDB using following rules from paper:
        /// 1. Rule 1: One-to-One Association Relationship Transformation
        /// 2. Rule 2: One-to-Many Association Relationship Transformation
        /// 3. Rule 3: Many-to-Many Association Relationship
        /// </summary>
        /// <param name="saveIntegratedData"></param>
        public void RelationshipTransform(bool saveIntegratedData)
        {
            // SQL Server connection string
            string connectionString = "server=Home\\SQLExpress01;database=SchemaConverter;integrated Security=SSPI;TrustServerCertificate=True;";

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                TableConstraints? tb = new TableConstraints();

                // Get all tables from SQL
                DataTable tables = connection.GetSchema("Tables");
                Console.WriteLine("Tables:");
                foreach (DataRow row in tables.Rows)
                {
                    Console.WriteLine($"Table: {row["TABLE_NAME"]}");

                    if (row["TABLE_NAME"] != null)
                    {
                        tb = _utilitiesService.GetTableConstraints(connection, row["TABLE_NAME"].ToString());
                        _utilitiesService.GetColumns(connection, row["TABLE_NAME"].ToString(), ref tb);
                    }

                    string query = $"SELECT * FROM {tb.TableName}";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    dataTable.TableName = tb.TableName;

                    if (dataTable.Rows.Count > 0)
                    {
                        _utilitiesService.InsertIntoMongoDB(dataTable, saveIntegratedData, tb, "RelationshipSchemaDB");
                    }
                }
            }
        }
    }
}
