﻿using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Gigobyte.Daterpillar.TextTransformation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace Tests.Daterpillar.IntegrationTest
{
    [TestClass]
    [DeploymentItem(Test.Data.Samples)]
    [DeploymentItem((Test.File.x86SQLiteInterop))]
    [DeploymentItem((Test.File.x64SQLiteInterop))]
    [UseApprovalSubdirectory(nameof(ApprovalTests))]
    [UseReporter(typeof(DiffReporter), typeof(ClipboardReporter))]
    public class TemplateGenerationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        [TestCategory(Test.Trait.Integration)]
        public void Transform_should_generate_a_sqlite_schema_when_invoked()
        {
            // Arrange
            var settings = new SQLiteTemplateSettings()
            {
                CommentsEnabled = true,
                DropTableIfExist = true
            };

            var schema = Schema.Load(SampleData.GetFile(Test.File.MockSchemaXML).OpenRead());
            var sut = new SQLiteTemplate(settings, new SQLiteTypeNameResolver());

            // Act
            var script = sut.Transform(schema);
            var connection = CreateSQLiteConnection(script);

            // Assert
            Assert.IsNotNull(connection);
            Assert.IsFalse(string.IsNullOrWhiteSpace(script));
            Assert.IsTrue(File.Exists(connection.FileName));
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        [TestCategory(Test.Trait.Integration)]
        public void Transform_should_generate_csharp_classes_when_invoked()
        {
            // Arrange
            var schema = Schema.Load(SampleData.GetFile(Test.File.MockSchemaXML).OpenRead());
            var sut = new CSharpTemplate(CSharpTemplateSettings.Default, new CSharpTypeNameResolver());

            // Act
            var csharp = sut.Transform(schema);

            var syntax = CSharpSyntaxTree.ParseText(csharp);
            var errorList = syntax.GetDiagnostics();
            int errorCount = 0;

            foreach (var error in syntax.GetDiagnostics())
            {
                errorCount++;
                Debug.WriteLine(error);
            }

            // Assert
            Assert.AreEqual(0, errorCount);
            Assert.IsFalse(string.IsNullOrWhiteSpace(csharp));
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        [TestCategory(Test.Trait.Integration)]
        public void Transform_should_generate_csharp_classes_that_implement_INotifyPropertyChanged_when_invoked()
        {
            // Arrange
            var schema = Schema.Load(SampleData.GetFile(Test.File.MockSchemaXML).OpenRead());
            var settings = new NotifyPropertyChangedTemplateSettings()
            {
                DataContractsEnabled = true,
                SchemaAnnotationsEnabled = true,
                VirtualPropertiesEnabled = true,
                PartialRaisePropertyChangedMethodEnabled = true,
            };
            var sut = new NotifyPropertyChangedTemplate(settings, new CSharpTypeNameResolver());

            // Act
            var csharp = sut.Transform(schema);

            var syntax = CSharpSyntaxTree.ParseText(csharp);
            var errorList = syntax.GetDiagnostics();
            int errorCount = 0;

            foreach (var error in syntax.GetDiagnostics())
            {
                errorCount++;
                Debug.WriteLine(error);
            }

            // Assert
            Assert.AreEqual(0, errorCount);
            Assert.IsFalse(string.IsNullOrWhiteSpace(csharp));
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        [TestCategory(Test.Trait.Integration)]
        public void Transform_should_generate_a_mysql_schema_when_invoked()
        {
            // Arrange
            using (var fileStream = SampleData.GetFile(Test.File.MockSchemaXML).OpenRead())
            {
                var settings = new MySQLTemplateSettings()
                {
                    CommentsEnabled = true,
                    DropDatabaseIfExist = true
                };

                var schema = Schema.Load(fileStream);
                var sut = new MySQLTemplate(settings, new MySQLTypeNameResolver());
                int nChanges;

                // Act
                var script = sut.Transform(schema);
                TestContext.WriteLine(script);

                using (var connection = new MySql.Data.MySqlClient.MySqlConnection(ConfigurationManager.ConnectionStrings["mysql"].ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = script;
                        nChanges = command.ExecuteNonQuery();
                    }
                }

                // Assert
                Assert.IsTrue(nChanges > 0, $"{nChanges} changes were made.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(script));
            }
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        [TestCategory(Test.Trait.Integration)]
        public void Transform_should_generate_a_tsql_schema_when_invoked()
        {
            // Arrange
            var settings = new MSSQLTemplateSettings()
            {
                AddScript = true,
                CreateSchema = false,
                UseDatabase = true,
                CommentsEnabled = true,
                DropDatabaseIfExist = false,
            };

            var schema = SampleData.CreateMockSchema();
            var script = new MSSQLTemplate(settings).Transform(schema);

            // Act
            using (var connection = new SqlConnection(Test.ConnectionString.MSSQL))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = script;
                    command.ExecuteNonQuery();
                }

                // Cleanup
                SampleData.TryTruncateDatabase(connection, schema);
            }

            // Assert
            Approvals.Verify(script);
        }

        #region Private Members

        private SQLiteConnection CreateSQLiteConnection(string schema)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"temp{DateTime.Now.ToString("HHmmssfff")}.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            string connectionString = new SQLiteConnectionStringBuilder() { DataSource = dbPath }.ConnectionString;
            var connection = new SQLiteConnection(connectionString);

            if (schema != null)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = schema;
                    command.ExecuteNonQuery();
                }
            }

            return connection;
        }

        #endregion Private Members
    }
}