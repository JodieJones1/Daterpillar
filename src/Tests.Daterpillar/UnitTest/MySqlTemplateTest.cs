﻿using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Gigobyte.Daterpillar.TextTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;

namespace Tests.Daterpillar.UnitTest
{
    [TestClass]
    [UseApprovalSubdirectory(nameof(ApprovalTests))]
    [UseReporter(typeof(DiffReporter), typeof(ClipboardReporter))]
    public class MySQLTemplateTest
    {
        [ClassCleanup]
        public static void Cleanup()
        {
            ApprovalTests.Maintenance.ApprovalMaintenance.CleanUpAbandonedFiles();
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        public void Transform_should_generate_a_mysql_schema_when_all_template_settings_are_enabled()
        {
            // Arrange
            var settings = new MySQLTemplateSettings()
            {
                CommentsEnabled = true,
                DropDatabaseIfExist = true
            };

            var schema = SampleData.CreateSchema();
            var managerTable = SampleData.CreateTableSchema("Manager");
            managerTable.Comment = "this is a table";
            schema.Tables.Add(managerTable);

            var mockResolver = Mock.Create<ITypeNameResolver>();
            mockResolver.Arrange(x => x.GetName(Arg.IsAny<DataType>()))
                .Returns("int")
                .OccursAtLeast(1);

            var sut = new MySQLTemplate(settings, mockResolver);

            // Act
            var result = sut.Transform(schema);

            // Assert
            mockResolver.Assert();
            Approvals.Verify(result);
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        public void Transform_should_generate_a_mysql_schema_when_all_template_settings_are_disabled()
        {
            // Arrange
            var settings = new MySQLTemplateSettings()
            {
                CommentsEnabled = false,
                DropDatabaseIfExist = false
            };

            var schema = SampleData.CreateSchema();
            var managerTable = SampleData.CreateTableSchema("Manager");
            schema.Tables.Add(managerTable);

            var mockResolver = Mock.Create<ITypeNameResolver>();
            mockResolver.Arrange(x => x.GetName(Arg.IsAny<DataType>()))
                .Returns("int")
                .OccursAtLeast(1);

            var sut = new MySQLTemplate(settings, mockResolver);

            // Act
            var result = sut.Transform(schema);

            // Assert
            mockResolver.Assert();
            Approvals.Verify(result);
        }

        [TestMethod]
        [Owner(Test.Dev.Ackara)]
        public void Transform_should_assign_the_mysql_schema_index_tableName_property_when_null()
        {
            // Arrange
            var sample = SampleData.CreateSchema();
            sample.Tables[0].Indexes[1].Table = string.Empty;

            var mockTypeResolver = Mock.Create<ITypeNameResolver>();
            mockTypeResolver.Arrange(x => x.GetName(Arg.IsAny<DataType>()))
                .Returns("INT")
                .OccursAtLeast(1);

            var sut = new MySQLTemplate(new MySQLTemplateSettings(), mockTypeResolver);

            // Act
            var script = sut.Transform(sample);

            // Assert
            mockTypeResolver.AssertAll();
            Approvals.Verify(script);
        }
    }
}