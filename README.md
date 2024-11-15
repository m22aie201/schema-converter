**Readme**

The solution has been created with ASP.NET web application template, and is based on .NET 6 (https://dotnet.microsoft.com/en-us/download/dotnet/6.0) with C# as coding language.

The purpose for this application is to transfer schema and data from a SQL based RDBMS to a document based NoSQL database. For our demonstration we have used SQL Server 2022 Express Edition as source and MongoDB as target database.

*SQL Server 2022 - https://www.microsoft.com/en-in/sql-server/sql-server-downloads* <br />
*MongoDB - https://www.mongodb.com/try/download/community*

To run this application, the application should have connections to SQL Server and MongoDB.

To run this web application, set Schema-Converters as the startup project. The application should run on url: https://localhost:7185/.

We have also placed a sample SQL schema and data creation script in this repository (SQL\SQLScript.sql).
