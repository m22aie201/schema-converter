using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using Schema_Converters.Models;
using System.Data;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Schema_Converters.Services
{
    public class InheritanceTransformation
    {
        private readonly UtilitiesService _utilitiesService;

        public InheritanceTransformation(UtilitiesService utilitiesService)
        {
            _utilitiesService = utilitiesService;
        }

        /// <summary>
        /// This function takes all data from SQL and saves into MongoDB following "Rule 4: Specialization in Inheritance Relationship" from paper.
        /// </summary>
        public void InheritenceMapping()
        {
            DataSet ds = new DataSet();
            List<TableConstraints> tbList = new List<TableConstraints>();

            // MongoDB connection string
            string connString = "mongodb://localhost:27017";

            var client = new MongoClient(connString);
            var database = client.GetDatabase("InheritanceSchemaDB");

            _utilitiesService.DeleteAllCollections(database);

            // SQL Server connection string
            string connectionString = "server=Home\\SQLExpress01;database=SchemaConverter;integrated Security=SSPI;TrustServerCertificate=True;";

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                TableConstraints? tb = new TableConstraints();

                // Get all tables from SQL Server
                DataTable tables = connection.GetSchema("Tables");

                Console.WriteLine("Tables (SQL):");

                foreach (DataRow row in tables.Rows)
                {
                    Console.WriteLine($"Table: {row["TABLE_NAME"]}");

                    if (row["TABLE_NAME"] != null)
                    {
                        tb = _utilitiesService.GetTableConstraints(connection, row["TABLE_NAME"].ToString());
                        _utilitiesService.GetColumns(connection, row["TABLE_NAME"].ToString(), ref tb);

                        tbList.Add(tb);
                    }

                    string query = $"SELECT * FROM {tb.TableName}";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    dataTable.TableName = tb.TableName;

                    if (dataTable.Rows.Count > 0)
                    {
                        ds.Tables.Add(dataTable);
                        _utilitiesService.InsertIntoMongoDB(dataTable, false, tb, "InheritanceSchemaDB");
                    }
                }

                Dictionary<string, Guid> keyValuePairs = new Dictionary<string, Guid>();

                // Replace id values of referenced entities
                foreach (var t in tbList)
                {
                    var dt = ds.Tables[t.TableName];

                    if (dt != null)
                    {
                        foreach (var foreignKey in t.ForeignKeys)
                        {
                            var cName = foreignKey.Key.Split('|');
                            var baseCollection = database.GetCollection<BsonDocument>(t.TableName); ;
                            var collection = database.GetCollection<BsonDocument>(cName[1]);

                            foreach (DataRow baseTableRow in dt.Rows)
                            {
                                var filterDefinitions = new List<FilterDefinition<BsonDocument>>();
                                foreach (var pk in t.PrimaryKeys)
                                {
                                    filterDefinitions.Add(Builders<BsonDocument>.Filter.Eq(pk.Key, baseTableRow[pk.Key].ToString()));
                                }

                                var baseFilter = Builders<BsonDocument>.Filter.And(filterDefinitions);
                                var filter = Builders<BsonDocument>.Filter.Eq(cName[0], baseTableRow[cName[0]].ToString());

                                var baseColVal = baseCollection.Find(baseFilter).First();
                                var colVal = collection.Find(filter).First();

                                var update = Builders<BsonDocument>.Update.Set(cName[0], colVal["_id"]);

                                baseCollection.UpdateOne(baseFilter, update);
                            }
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
