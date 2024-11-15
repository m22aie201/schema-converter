using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using Schema_Converters.Models;
using System.Data;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Schema_Converters.Services
{
    public class UtilitiesService
    {
        /// <summary>
        /// This function gets all columns and its data types for a given table
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tableName"></param>
        /// <param name="constraints"></param>
        /// <exception cref="Exception"></exception>
        public void GetColumns(SqlConnection conn, string? tableName, ref TableConstraints constraints)
        {
            if (tableName == null)
            {
                throw new Exception("Please provide table name.");
            }

            if (constraints == null)
            {
                constraints = new TableConstraints()
                {
                    TableName = tableName,
                    PrimaryKeys = new(),
                    ForeignKeys = new(),
                    Constraints = new()
                };
            }

            // Get columns for a specific table
            string[] columnRestrictions = new string[4] { null, null, tableName, null };
            DataTable columns = conn.GetSchema("Columns", columnRestrictions);

            Console.WriteLine($"Columns given in table {tableName} are:");
            foreach (DataRow row in columns.Rows)
            {
                if (constraints.Columns != null)
                {
                    constraints.Columns.Add(row["COLUMN_NAME"].ToString(), row["DATA_TYPE"].ToString());
                    Console.WriteLine($"Name: {row["COLUMN_NAME"]} - DataType: {row["DATA_TYPE"]}");
                }
            }
        }

        /// <summary>
        /// This function gets all constraints on a given table
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TableConstraints GetTableConstraints(SqlConnection conn, string? tableName)
        {
            if (tableName == null)
            {
                throw new Exception("Please provide table name.");
            }

            TableConstraints constraints = new TableConstraints()
            {
                TableName = tableName,
                PrimaryKeys = new(),
                ForeignKeys = new(),
                Constraints = new()
            };

            // Get primary keys
            string primaryKeyQuery = @"
                    SELECT KU.COLUMN_NAME, TC.CONSTRAINT_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                        ON TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
                    WHERE TC.TABLE_NAME = '" + tableName + @"' 
                        AND TC.CONSTRAINT_TYPE = 'PRIMARY KEY'";

            using (SqlCommand command = new SqlCommand(primaryKeyQuery, conn))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                Console.WriteLine("Primary Keys:");
                while (reader.Read())
                {
                    if (reader["COLUMN_NAME"] != null)
                    {
                        constraints.PrimaryKeys.Add(reader["COLUMN_NAME"].ToString(), reader["CONSTRAINT_NAME"].ToString());
                        Console.WriteLine($"Column: {reader["COLUMN_NAME"]}, Key: {reader["CONSTRAINT_NAME"]}");
                    }
                }
            }

            // Get foreign keys
            string foreignKeyQuery = @"
                    SELECT FK.CONSTRAINT_NAME, CU.COLUMN_NAME, PK.TABLE_NAME AS REFERENCED_TABLE
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS FK
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS CU ON FK.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS PK ON PK.CONSTRAINT_NAME = FK.UNIQUE_CONSTRAINT_NAME
                    WHERE CU.TABLE_NAME = '" + tableName + "'";

            using (SqlCommand command = new SqlCommand(foreignKeyQuery, conn))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                Console.WriteLine("\nForeign Keys:");
                while (reader.Read())
                {
                    constraints.ForeignKeys.Add(reader["COLUMN_NAME"].ToString() + "|" + reader["REFERENCED_TABLE"].ToString(), reader["CONSTRAINT_NAME"].ToString());
                    Console.WriteLine($"Foreign Key: {reader["CONSTRAINT_NAME"]} - Column: {reader["COLUMN_NAME"]} - References Table: {reader["REFERENCED_TABLE"]}");
                }
            }

            // Get other constraints
            string constraintsQuery = @"
                    SELECT TC.CONSTRAINT_NAME, TC.CONSTRAINT_TYPE, KU.COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU ON TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
                    WHERE CONSTRAINT_TYPE NOT IN ('PRIMARY KEY', 'FOREIGN KEY') 
                    AND TC.TABLE_NAME = '" + tableName + "'";

            using (SqlCommand command = new SqlCommand(constraintsQuery, conn))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                Console.WriteLine("\nConstraints:");
                while (reader.Read())
                {
                    constraints.Constraints.Add(reader["COLUMN_NAME"].ToString() + "|" + reader["CONSTRAINT_TYPE"].ToString(), reader["CONSTRAINT_NAME"].ToString());
                    Console.WriteLine($"Constraint: {reader["CONSTRAINT_NAME"]} - Type: {reader["CONSTRAINT_TYPE"]}");
                }
            }

            return constraints;
        }

        /// <summary>
        /// This function converts a data row to Bson (Binary Javascript Object Notation) document for MongoDB
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnCollection"></param>
        /// <returns></returns>
        private BsonDocument CreateBsonDocument(DataRow row, DataColumnCollection columnCollection)
        {
            var doc = new BsonDocument();

            foreach (var column in columnCollection)
            {
                var name = column.ToString();

                if (name != null)
                {
                    doc.Add(name, row[name].ToString());
                }
            }

            return doc;
        }

        /// <summary>
        /// This function saves data rows as documents in a MongoDB collection
        /// </summary>
        /// <param name="table"></param>
        /// <param name="saveIntegratedData"></param>
        /// <param name="tb"></param>
        public void InsertIntoMongoDB(DataTable table, bool saveIntegratedData, TableConstraints tb, string databaseName)
        {
            // MongoDB connection string
            string connectionString = "mongodb://localhost:27017";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            // Create a MongoDB collection
            CreateMongoDBCollection(database, table.TableName);

            var collection = database.GetCollection<BsonDocument>(table.TableName);
            List<BsonDocument> documentList = new();

            // SQL Server connection string
            string connString = "server=Home\\SQLExpress01;database=SchemaConverter;integrated Security=SSPI;TrustServerCertificate=True;";

            foreach (DataRow row in table.Rows)
            {
                BsonArray nestedDocumentList = new();
                var document = CreateBsonDocument(row, table.Columns);

                if (saveIntegratedData)
                {
                    if (tb.ForeignKeys != null)
                    {
                        foreach (var dt in tb.ForeignKeys)
                        {
                            var key = dt.Key.Split("|");
                            var columnValue = key[0];

                            using (SqlConnection connection = new(connString))
                            {
                                string query = $"SELECT * FROM {key[1]} WHERE {key[0]} = {row[key[0]]}";
                                
                                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                                DataTable dataTable = new DataTable();
                                adapter.Fill(dataTable);

                                foreach (DataRow r in dataTable.Rows)
                                {
                                    var doc = CreateBsonDocument(r, dataTable.Columns);

                                    nestedDocumentList.Add(doc);
                                }

                                document.Add(key[1], nestedDocumentList);
                                document.Remove(key[0]);
                            }
                        }
                    }
                }

                documentList.Add(document);
            }

            // Insert data
            collection.InsertMany(documentList);

            Console.WriteLine("Multiple documents inserted successfully.");
        }

        /// <summary>
        /// This function creates a MongoDB collection. The collection is dropped and created again if it exists in MongoDB.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="collectionName"></param>
        public void CreateMongoDBCollection(IMongoDatabase db, string collectionName)
        {
            var collectionList = db.ListCollectionNames().ToList();

            if (collectionList.Contains(collectionName))
            {
                db.DropCollection(collectionName);
            }
            
            db.CreateCollection(collectionName);
            Console.WriteLine($"Collection '{collectionName}' created successfully.");
        }

        /// <summary>
        /// This function delete a MongoDB collection.
        /// </summary>
        /// <param name="db"></param>
        public void DeleteAllCollections(IMongoDatabase db)
        {
            var collectionList = db.ListCollectionNames().ToList();

            foreach (var collection in collectionList)
            {
                db.DropCollection(collection);
            }
        }
    }
}
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.