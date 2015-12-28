﻿using Gigobyte.Daterpillar.Data;
using Gigobyte.Daterpillar.Data.Linq;
using Gigobyte.Daterpillar.Transformation;
using Gigobyte.Daterpillar.Transformation.Template;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Tests.Daterpillar.Sample;

namespace Tests.Daterpillar.IntegrationTest
{
    [TestClass]
    [DeploymentItem(Artifact.x86SQLiteInterop)]
    [DeploymentItem(Artifact.x64SQLiteInterop)]
    [DeploymentItem(Artifact.SamplesFolder + Artifact.SampleSchema)]
    public class SQLiteCommandTests
    {
        public static string ConnectionString;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            BuildSQLiteDatabase();
        }

        [TestMethod]
        [Owner(Str.Ackara)]
        public void RunQueryOnSQLiteConnection()
        {
            // Arrange
            using (var connection = new AdoNetConnectionWrapper(new SQLiteConnection(ConnectionString), QueryStyle.SQLite))
            {
                var query = new Query()
                    .SelectAll()
                    .From(Song.Table)
                    .Where($"{Song.IdColumn}<='100'");

                // Act
                var album = connection.Execute<Song>(query);
                var track1 = album.First();

                var single = connection.Execute<Song>(track1.ToSelectCommand())
                    .First();

                // Assert
                Assert.AreEqual(3, album.Count());
                Assert.AreEqual(track1.Id, single.Id);
            }
        }

        [TestMethod]
        [Owner(Str.Ackara)]
        public void RunInsertCommandOnSQLiteConnection()
        {
            // Arrange
            var track1 = Samples.GetSong();
            var query = new Query()
                .SelectAll()
                .From(Song.Table)
                .Where($"{Song.NameColumn}={track1.Name.ToSQL()}");

            using (var connection = new AdoNetConnectionWrapper(new SQLiteConnection(ConnectionString), QueryStyle.SQLite))
            {
                // Act
                connection.Insert(track1);
                connection.Commit();

                var song = connection.Execute<Song>(query)
                    .First();

                // Assert
                Assert.AreEqual(nameof(RunInsertCommandOnSQLiteConnection), song.Name);
                Assert.AreNotEqual(track1.Id, song.Id);
            }
        }

        [TestMethod]
        [Owner(Str.Ackara)]
        public void RunDeleteCommandOnSQLiteConnection()
        {
            // Arrange
            var track1 = Samples.GetSong();
            var query = new Query()
                .SelectAll()
                .From(Song.Table)
                .Where($"{Song.NameColumn}={track1.Name.ToSQL()}");

            using (var connection = new AdoNetConnectionWrapper(new SQLiteConnection(ConnectionString), QueryStyle.SQLite))
            {
                // Act
                connection.Insert(track1);
                connection.Commit();

                var insertedTrack = connection.Execute<Song>(query)
                    .First();

                connection.Delete(insertedTrack);
                connection.Commit();

                bool trackDeleted = connection.Execute<Song>(query).FirstOrDefault() == null;

                // Assert
                Assert.IsNotNull(insertedTrack);
                Assert.IsTrue(trackDeleted);
            }
        }

        [TestMethod]
        [Owner(Str.Ackara)]
        public void HandleSQLiteException()
        {
            // Arrange
            using (var connection = new AdoNetConnectionWrapper(new SQLiteConnection(ConnectionString), QueryStyle.SQLite))
            {
                connection.ExceptionHandler = ExceptionHandler;

                // Act
                connection.ExecuteNonQuery(nameof(HandleSQLiteException));
                connection.Commit();
            }

            // Assert
            Assert.IsTrue(_exceptionHandlerCalled);
        }

        [TestMethod]
        [Owner(Str.Ackara)]
        public void RaiseErrorEvent()
        {
            // Arrange
            using (var connection = new AdoNetConnectionWrapper(new SQLiteConnection(ConnectionString), QueryStyle.SQLite))
            {
                // Act
                connection.Error += Connection_Error;
                connection.ExecuteNonQuery(nameof(RaiseErrorEvent));

                try
                {
                    connection.Commit();
                    Assert.Fail("Exception was not thrown.");
                }
                catch
                {
                    System.Threading.Thread.Sleep(500);

                    // Assert
                    Assert.IsTrue(_eventRaised);
                }
            }
        }

        internal void ExceptionHandler(Exception ex, string command, out bool handled)
        {
            _exceptionHandlerCalled = (command == nameof(HandleSQLiteException));
            handled = true;
        }

        internal void Connection_Error(object sender, DbExceptionEventArgs e)
        {
            _eventRaised = true;
        }

        #region Private Members

        private static bool _exceptionHandlerCalled, _eventRaised;

        private static void BuildSQLiteDatabase()
        {
            string path = Samples.GetFile(Artifact.SampleSchema).FullName;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var schema = Schema.Load(stream);
                string sqlite = new SQLiteTemplate().Transform(schema);

                using (var connection = DbFactory.CreateSQLiteConnection(sqlite))
                {
                    var builder = new SQLiteConnectionStringBuilder();
                    builder.DataSource = connection.FileName;

                    ConnectionString = builder.ConnectionString;
                }
            }
        }

        #endregion Private Members
    }
}