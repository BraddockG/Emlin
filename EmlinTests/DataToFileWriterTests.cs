﻿using Emlin;
using Emlin.Encryption;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace EmlinTests
{
    class DataToFileWriterTests
    {
        private List<KeysData> keysData;
        private MockFileSystem fileSystem;
        private DataToFileWriter dataToFileWriter;
        private string textContents;
        private IEncryptor encryptorFake;
        

        [SetUp]
        public void SetUp()
        {
            keysData = new List<KeysData>();
            fileSystem = new MockFileSystem();
            dataToFileWriter = new DataToFileWriter();
            dataToFileWriter.AddFileSystem(fileSystem);
            encryptorFake = new EncryptorFake();
        }

        private void PrepareFileForWriting()
        {
            dataToFileWriter.CreateDirectoryAndFile(@"D:\Directory\File.txt");
            textContents = GetContentsOfTextFile();
        }

        private string GetContentsOfTextFile()
        {
            return fileSystem.GetFile(@"D:\Directory\File.txt").TextContents;
        }

        private void WriteDataToFile()
        {
            dataToFileWriter.WriteRecordedDataToFile(keysData, @"D:\Directory\File.txt", encryptorFake);
            textContents = GetContentsOfTextFile();
        }

        [Test]
        public void Prepare_file_for_writing_should_create_the_directory_its_writing_to_if_it_doesnt_exist()
        {
            PrepareFileForWriting();
            Assert.That(fileSystem.Directory.Exists(@"D:\Directory"), Is.True);
        }

        [Test]
        public void Prepare_file_for_writing_should_not_create_the_directory_with_the_same_name_as_the_file()
        {
            PrepareFileForWriting();
            Assert.That(fileSystem.Directory.Exists(@"D:\Directory\File.txt"), Is.False);
        }

        [Test]
        public void Prepare_file_for_writing_should_create_the_file_its_writing_to_if_it_doesnt_exist()
        {
            PrepareFileForWriting();
            Assert.That(fileSystem.File.Exists(@"D:\Directory\File.txt"), Is.True);
        }

        [Test]
        public void Write_data_recorder_should_add_a_single_holdtime_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 100));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,100"));
        }

        [Test]
        public void Write_data_recorder_should_add_a_multiple_holdtime_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 100));
            keysData.Add(NewKeysData(1, 200));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,100"));
            Assert.That(textContents, Contains.Substring("1,200"));
        }

        [Test]
        public void Write_data_recorder_should_add_a_single_flight_time_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100"));
        }

        [Test]
        public void Write_data_recorder_should_add_multiple_flight_times_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100));
            keysData.Add(NewKeysData(1, 0, 260));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100"));
            Assert.That(textContents, Contains.Substring("1,0,260"));
        }

        [Test]
        public void Write_data_recorder_should_add_a_single_digraph1_time_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200"));
        }

        [Test]
        public void Write_data_recorder_should_add_multiple_digraph1_times_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200));
            keysData.Add(NewKeysData(1, 666, 666, 42));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200"));
            Assert.That(textContents, Contains.Substring("1,666,666,42"));
        }

        [Test]
        public void Write_data_recorder_should_add_a_single_digraph2_time_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200, 300));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200,300"));
        }


        [Test]
        public void Write_data_recorder_should_add_multiple_digraph2_times_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200, 300));
            keysData.Add(NewKeysData(1, 666, 666, 42, 96));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200,300"));
            Assert.That(textContents, Contains.Substring("1,666,666,42,96"));
        }

        [Test]
        public void Write_data_recorder_should_add_a_single_digraph3_time_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200, 300, 400));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200,300,400"));
        }


        [Test]
        public void Write_data_recorder_should_add_multiple_digraph3_times_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, 100, 200, 300, 400));
            keysData.Add(NewKeysData(1, 666, 666, 42, 96, 192));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,100,200,300,400"));
            Assert.That(textContents, Contains.Substring("1,666,666,42,96,192"));
        }

        [Test]
        public void Write_data_recorder_should_write_a_time_long_than_a_second_to_the_text_file()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 2000));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,2000"));
        }

        [Test]
        public void Write_data_recorder_should_handle_negative_values()
        {
            PrepareFileForWriting();
            keysData.Add(NewKeysData(0, 0, -2000));
            WriteDataToFile();

            Assert.That(textContents, Contains.Substring("0,0,-2000"));
        }


        private KeysData NewKeysData(int combID, int Ht, int Ft, int D1, int D2, int D3)
        {
            return new KeysData
            {
                CombinationID = combID,
                HoldTime = TimeSpan.FromMilliseconds(Ht),
                FlightTime = TimeSpan.FromMilliseconds(Ft),
                Digraph1 = TimeSpan.FromMilliseconds(D1),
                Digraph2 = TimeSpan.FromMilliseconds(D2),
                Digraph3 = TimeSpan.FromMilliseconds(D3)
            };
        }

        private KeysData NewKeysData(int combID, int Ht, int Ft, int D1, int D2)
        {
            return NewKeysData(combID, Ht, Ft, D1, D2, 0);
        }

        private KeysData NewKeysData(int combID, int Ht, int Ft, int D1)
        {
            return NewKeysData(combID, Ht, Ft, D1, 0);
        }

        private KeysData NewKeysData(int combID, int Ht, int Ft)
        {
            
            return NewKeysData(combID, Ht, Ft, 0);
        }

        private KeysData NewKeysData(int combID, int Ht)
        {
            return NewKeysData(combID,Ht,0);
        }
    }
}
